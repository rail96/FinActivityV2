using FindActivity.Application.Dtos;
using FindActivity.Domain.Enums;

namespace FindActivity.Web.Models.Activities;

public class MyHostedViewModel
{
    /// <summary>Currently selected status filter (null = All).</summary>
    public ActivityStatus? FilterStatus { get; set; }

    public IReadOnlyList<ActivityWithParticipantsDto> Activities { get; set; } = Array.Empty<ActivityWithParticipantsDto>();

    public int TotalCount { get; set; }
    public int ScheduledCount { get; set; }
    public int CancelledCount { get; set; }
    public int CompletedCount { get; set; }
}
