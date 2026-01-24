namespace FindActivity.Application.Dtos;

public class ActivitySearchParamsDto
{
    public string? City { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? StartUtcFrom { get; set; }
    public DateTime? StartUtcTo { get; set; }
}
