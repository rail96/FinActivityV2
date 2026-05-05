using FindActivity.Domain.Enums;

namespace FindActivity.Application.Dtos;

public class ParticipantSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Joined;
}
