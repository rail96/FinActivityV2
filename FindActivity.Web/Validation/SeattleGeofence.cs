namespace FindActivity.Web.Validation;

/// <summary>
/// Bounding box for the "Greater Seattle area" we serve. Used to gate activity creation:
/// activities outside this rectangle are rejected even if Mapbox returned a valid Washington address.
///
/// The box covers Seattle, Bellevue, Redmond, Kirkland, Bothell, Tacoma, Everett, and the
/// surrounding metro. It deliberately excludes the rest of Washington state so we don't accept
/// activities in Spokane, Yakima, etc.
/// </summary>
public static class SeattleGeofence
{
    public const double MinLatitude = 47.15;
    public const double MaxLatitude = 48.05;
    public const double MinLongitude = -122.80;
    public const double MaxLongitude = -121.60;

    /// <summary>Mapbox-style "minLng,minLat,maxLng,maxLat" string for autocomplete bbox params.</summary>
    public const string MapboxBbox = "-122.80,47.15,-121.60,48.05";

    public static bool IsInside(double? latitude, double? longitude)
    {
        if (latitude is null || longitude is null)
        {
            return false;
        }

        return latitude >= MinLatitude && latitude <= MaxLatitude
            && longitude >= MinLongitude && longitude <= MaxLongitude;
    }
}
