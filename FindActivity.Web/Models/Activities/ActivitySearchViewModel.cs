using FindActivity.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FindActivity.Web.Models.Activities;

public class ActivitySearchViewModel
{
    public string? City { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? StartUtcFrom { get; set; }
    public DateTime? StartUtcTo { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? RadiusKm { get; set; }

    public IReadOnlyList<ActivityListItemDto> Results { get; set; } = Array.Empty<ActivityListItemDto>();
    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}
