using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FindActivity.Web.Models.Activities;

public class ActivityFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Display(Name = "Start (UTC)")]
    public DateTime StartUtc { get; set; } = DateTime.UtcNow.AddDays(1);

    [Range(15, 1440)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string State { get; set; } = string.Empty;

    [StringLength(150)]
    public string AddressPlaceId { get; set; } = string.Empty;

    /// <summary>Captured from Mapbox autocomplete via a hidden field. Used for the map view.</summary>
    public double? Latitude { get; set; }

    /// <summary>Captured from Mapbox autocomplete via a hidden field. Used for the map view.</summary>
    public double? Longitude { get; set; }

    [Range(1, 5000)]
    public int Capacity { get; set; } = 10;

    [Range(0, 120)]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}
