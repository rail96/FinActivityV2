namespace FindActivity.Application.Geo;

/// <summary>
/// Tiny great-circle distance helper. Lives in the Application layer so it can be used
/// by services without taking a dependency on the Web project.
/// </summary>
public static class GeoMath
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>Seattle's approximate downtown center (Pike Place / Pioneer Square area).</summary>
    public const double SeattleLat = 47.6062;
    public const double SeattleLng = -122.3321;

    /// <summary>Haversine distance between two lat/lng points, in kilometers.</summary>
    public static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180.0;
}
