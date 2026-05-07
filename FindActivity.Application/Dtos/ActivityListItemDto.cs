using FindActivity.Domain.Enums;

namespace FindActivity.Application.Dtos;

public class ActivityListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int JoinedCount { get; set; }
    public ActivityStatus Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CoverImagePath { get; set; }
}
