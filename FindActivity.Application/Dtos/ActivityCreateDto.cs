namespace FindActivity.Application.Dtos;

public class ActivityCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime StartUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public int Capacity { get; set; }
    public int? MinAge { get; set; }
}
