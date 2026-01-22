using FindActivity.Application.Dtos;

namespace FindActivity.Web.Models.Activities;

public class ActivityDetailsViewModel
{
    public ActivityDetailsDto Activity { get; set; } = new();
    public bool IsCreator { get; set; }
    public bool CanReviewHost { get; set; }
    public string? CurrentUserId { get; set; }
}
