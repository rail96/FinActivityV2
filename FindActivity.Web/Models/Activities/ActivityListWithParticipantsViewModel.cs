using FindActivity.Application.Dtos;

namespace FindActivity.Web.Models.Activities;

public class ActivityListWithParticipantsViewModel
{
    public string Heading { get; set; } = string.Empty;
    public bool ShowHostLink { get; set; }
    public IReadOnlyList<ActivityWithParticipantsDto> Activities { get; set; } = Array.Empty<ActivityWithParticipantsDto>();
}
