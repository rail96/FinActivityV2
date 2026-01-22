using FindActivity.Domain.Enums;

namespace FindActivity.Domain.Entities;

public class ActivityParticipant
{
    public Guid ActivityId { get; set; }
    public Activity? Activity { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public ParticipantStatus Status { get; set; } = ParticipantStatus.Joined;
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}
