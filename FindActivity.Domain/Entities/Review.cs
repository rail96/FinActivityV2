namespace FindActivity.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public Activity? Activity { get; set; }

    public string ReviewerUserId { get; set; } = string.Empty;
    public ApplicationUser? ReviewerUser { get; set; }

    public string RevieweeUserId { get; set; } = string.Empty;
    public ApplicationUser? RevieweeUser { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
