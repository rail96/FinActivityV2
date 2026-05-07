namespace FindActivity.Application.Dtos;

public class ActivitySearchParamsDto
{
    public string? City { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? StartUtcFrom { get; set; }
    public DateTime? StartUtcTo { get; set; }

    /// <summary>
    /// When set, only activities within this many kilometers of <see cref="CenterLat"/>/<see cref="CenterLng"/>
    /// (or Seattle's downtown center if those are null) are returned.
    /// </summary>
    public int? MaxDistanceKm { get; set; }

    public double? CenterLat { get; set; }
    public double? CenterLng { get; set; }
}
