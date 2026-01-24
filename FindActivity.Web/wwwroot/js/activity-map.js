(() => {
    const mapElement = document.getElementById("activity-map");
    if (!mapElement) {
        return;
    }

    const token = mapElement.dataset.mapboxToken;
    const dataElement = document.getElementById("activity-map-data");
    const events = dataElement ? JSON.parse(dataElement.textContent || "[]") : [];

    if (!token) {
        mapElement.textContent = "Add a Mapbox access token to enable the map.";
        mapElement.classList.add("text-muted", "d-flex", "align-items-center", "justify-content-center");
        return;
    }

    mapboxgl.accessToken = token;

    const map = new mapboxgl.Map({
        container: mapElement,
        style: "mapbox://styles/mapbox/streets-v12",
        center: [-120.5, 47.4],
        zoom: 5
    });

    map.addControl(new mapboxgl.NavigationControl(), "top-right");

    const bounds = new mapboxgl.LngLatBounds();
    let hasMarkers = false;
    const WA_BBOX = "-124.848974,45.543541,-116.91607,49.002494";

    const geocodeAddress = async (query) => {
        const url = new URL(`https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(query)}.json`);
        url.searchParams.set("access_token", token);
        url.searchParams.set("bbox", WA_BBOX);
        url.searchParams.set("country", "us");
        url.searchParams.set("limit", "1");

        const response = await fetch(url.toString());
        if (!response.ok) {
            return null;
        }
        const data = await response.json();
        return data.features?.[0]?.center ?? null;
    };

    const addMarker = (lngLat, eventInfo) => {
        const content = document.createElement("div");
        const title = document.createElement("strong");
        title.textContent = eventInfo.title;
        const address = document.createElement("div");
        address.textContent = `${eventInfo.address}, ${eventInfo.city}, ${eventInfo.state}`;
        const link = document.createElement("a");
        link.href = eventInfo.detailsUrl;
        link.textContent = "View details";
        content.append(title, address, link);

        const popup = new mapboxgl.Popup({ offset: 24 }).setDOMContent(content);
        new mapboxgl.Marker()
            .setLngLat(lngLat)
            .setPopup(popup)
            .addTo(map);
        bounds.extend(lngLat);
        hasMarkers = true;
    };

    Promise.all(
        events.map(async (eventInfo) => {
            if (!eventInfo.address || !eventInfo.city) {
                return;
            }
            const coords = await geocodeAddress(`${eventInfo.address}, ${eventInfo.city}, ${eventInfo.state}`);
            if (coords) {
                addMarker(coords, eventInfo);
            }
        })
    ).finally(() => {
        if (hasMarkers) {
            map.fitBounds(bounds, { padding: 40, maxZoom: 12 });
        }
    });
})();
