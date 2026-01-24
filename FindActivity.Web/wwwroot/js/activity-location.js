(() => {
    window.__activityLocationLoaded = true;
    const containers = document.querySelectorAll("[data-address-autocomplete]");
    if (!containers.length) {
        return;
    }

    const WA_BBOX = "-124.848974,45.543541,-116.91607,49.002494";
    const SEATTLE_PROXIMITY = "-122.3321,47.6062";

    const debounce = (fn, delay) => {
        let timerId;
        return (...args) => {
            window.clearTimeout(timerId);
            timerId = window.setTimeout(() => fn(...args), delay);
        };
    };

    const getCityFromFeature = (feature) => {
        const context = feature.context || [];
        const place = context.find((item) => item.id.startsWith("place."));
        const locality = context.find((item) => item.id.startsWith("locality."));
        if (place?.text) {
            return place.text;
        }
        if (locality?.text) {
            return locality.text;
        }
        const fallback = feature.place_name?.split(",").map((part) => part.trim());
        return fallback?.length ? fallback[1] ?? "" : "";
    };

    containers.forEach((container) => {
        const token = container.dataset.mapboxToken;
        const form = container.closest("form");
        const addressInput = container.querySelector("[data-address-input]");
        const cityInput = (form ?? document).querySelector("[data-city-input]");
        const stateInput = (form ?? document).querySelector("[data-state-input]");
        const placeIdInput = container.querySelector("[data-place-id-input]");
        const list = container.querySelector("[data-address-suggestions]");
        const help = container.querySelector("[data-address-help]");

        if (!addressInput || !placeIdInput || !list) {
            return;
        }

        const hasToken = Boolean(token && token.trim());
        if (help) {
            help.textContent = hasToken
                ? "Enter a city first, then select a suggested address."
                : "Mapbox token missing. Address suggestions are disabled.";
        }

        let activeRequest = 0;

        const clearSuggestions = () => {
            list.innerHTML = "";
            list.classList.add("d-none");
        };

        const showStatus = (message) => {
            list.innerHTML = "";
            const item = document.createElement("div");
            item.className = "list-group-item text-muted";
            item.textContent = message;
            list.appendChild(item);
            list.classList.remove("d-none");
            list.style.display = "block";
            list.style.position = "absolute";
            list.style.zIndex = "1050";
            list.style.backgroundColor = "#fff";
        };

        const showSuggestions = (features) => {
            list.innerHTML = "";
            if (!features.length) {
                showStatus("No Washington matches found.");
                return;
            }

            features.forEach((feature) => {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "list-group-item list-group-item-action";
                button.textContent = feature.place_name;
                button.dataset.placeId = feature.id;
                button.dataset.placeName = feature.place_name;
                button.dataset.city = getCityFromFeature(feature);
                const region = (feature.context || []).find((item) => item.id.startsWith("region."));
                button.dataset.state = region?.short_code?.toUpperCase() || region?.text || "";
                list.appendChild(button);
            });

            list.classList.remove("d-none");
            list.style.display = "block";
            list.style.position = "absolute";
            list.style.zIndex = "1050";
            list.style.backgroundColor = "#fff";
        };

        const fetchSuggestions = debounce(async () => {
            const query = addressInput.value.trim();
            if (query.length < 3) {
                clearSuggestions();
                return;
            }

            const requestId = ++activeRequest;
            const fullQuery = `${query}, WA`;
            const url = new URL(`https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(fullQuery)}.json`);
            url.searchParams.set("access_token", token);
            url.searchParams.set("autocomplete", "true");
            url.searchParams.set("types", "address,poi");
            url.searchParams.set("bbox", WA_BBOX);
            url.searchParams.set("country", "us");
            url.searchParams.set("proximity", SEATTLE_PROXIMITY);
            url.searchParams.set("limit", "6");

            try {
                const response = await fetch(url.toString());
                if (!response.ok) {
                    showStatus("Unable to load suggestions.");
                    return;
                }
                const data = await response.json();
                if (requestId !== activeRequest) {
                    return;
                }
                showSuggestions(data.features || []);
            } catch {
                showStatus("Unable to load suggestions.");
            }
        }, 250);

        addressInput.addEventListener("input", () => {
            placeIdInput.value = "";
            addressInput.classList.remove("is-invalid");
            if (cityInput) {
                cityInput.value = "";
            }
            if (stateInput) {
                stateInput.value = "";
            }
            if (!hasToken) {
                showStatus("Missing Mapbox token.");
                return;
            }
            fetchSuggestions();
        });

        if (cityInput) {
            cityInput.addEventListener("input", () => {
                placeIdInput.value = "";
                if (stateInput) {
                    stateInput.value = "";
                }
                clearSuggestions();
            });
        }

        list.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement) || !target.dataset.placeId) {
                return;
            }

            addressInput.value = target.dataset.placeName || addressInput.value;
            if (cityInput) {
                cityInput.value = target.dataset.city || "";
            }
            if (stateInput) {
                stateInput.value = target.dataset.state || "";
            }
            placeIdInput.value = target.dataset.placeId;
            addressInput.classList.remove("is-invalid");
            clearSuggestions();
        });

        document.addEventListener("click", (event) => {
            if (!container.contains(event.target)) {
                clearSuggestions();
            }
        });

        if (form) {
            form.addEventListener("submit", (event) => {
                if (!placeIdInput.value) {
                    event.preventDefault();
                    addressInput.classList.add("is-invalid");
                    if (help) {
                        help.textContent = "Select a suggested address before saving.";
                    }
                }
            });
        }
    });
})();
