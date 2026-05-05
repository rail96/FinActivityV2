using FindActivity.Domain.Enums;

namespace FindActivity.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime StartUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? AddressPlaceId { get; set; }
    public int Capacity { get; set; }
    public int? MinAge { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedByUser { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Scheduled;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp the 24h reminder email was sent for this activity. Null until the reminder runs.
    /// Used by ActivityReminderService for idempotency so the background job won't double-send.
    /// </summary>
    public DateTime? ReminderSentUtc { get; set; }

    public ICollection<ActivityParticipant> Participants { get; set; } = new List<ActivityParticipant>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
