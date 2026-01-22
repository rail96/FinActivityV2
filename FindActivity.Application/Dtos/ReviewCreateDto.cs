namespace FindActivity.Application.Dtos;

public class ReviewCreateDto
{
    public Guid ActivityId { get; set; }
    public string RevieweeUserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
