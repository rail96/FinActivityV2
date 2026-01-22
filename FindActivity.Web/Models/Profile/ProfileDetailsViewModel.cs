using FindActivity.Domain.Entities;

namespace FindActivity.Web.Models.Profile;

public class ProfileDetailsViewModel
{
    public ApplicationUser User { get; set; } = new();
    public IReadOnlyList<ReviewSummary> Reviews { get; set; } = Array.Empty<ReviewSummary>();

    public class ReviewSummary
    {
        public Guid ActivityId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string ReviewerUserId { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
    }
}
