using FindActivity.Domain.Entities;

namespace FindActivity.Web.Models.Admin;

public class AdminReportDetailsViewModel
{
    public Report Report { get; set; } = new();
    public ApplicationUser? Reporter { get; set; }

    /// <summary>Populated when the report targets an activity.</summary>
    public Activity? TargetActivity { get; set; }

    /// <summary>Populated when the report targets a user (or the host of a target activity).</summary>
    public ApplicationUser? TargetUser { get; set; }

    /// <summary>Other reports against the same target (most recent first), excluding this one.</summary>
    public IReadOnlyList<RelatedReport> RelatedReports { get; set; } = Array.Empty<RelatedReport>();

    public class RelatedReport
    {
        public Guid Id { get; set; }
        public Domain.Enums.ReportReason Reason { get; set; }
        public Domain.Enums.ReportStatus Status { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
