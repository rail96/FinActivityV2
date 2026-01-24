using FindActivity.Domain.Enums;

namespace FindActivity.Application.Dtos;

public class ActivityDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? AddressPlaceId { get; set; }
    public int Capacity { get; set; }
    public int? MinAge { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public ActivityStatus Status { get; set; }
    public int JoinedCount { get; set; }
    public bool IsUserJoined { get; set; }
    public IReadOnlyList<ParticipantSummaryDto> Participants { get; set; } = Array.Empty<ParticipantSummaryDto>();
}
