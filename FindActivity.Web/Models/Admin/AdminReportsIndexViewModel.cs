using FindActivity.Domain.Enums;

namespace FindActivity.Web.Models.Admin;

public class AdminReportsIndexViewModel
{
    public IReadOnlyList<ReportListItem> Reports { get; set; } = Array.Empty<ReportListItem>();

    /// <summary>Currently selected status filter (Open by default).</summary>
    public ReportStatus FilterStatus { get; set; } = ReportStatus.Open;

    public int OpenCount { get; set; }
    public int ActionTakenCount { get; set; }
    public int DismissedCount { get; set; }

    public class ReportListItem
    {
        public Guid Id { get; set; }
        public ReportTargetType TargetType { get; set; }
        public string TargetLabel { get; set; } = string.Empty;
        public Guid? TargetActivityId { get; set; }
        public string? TargetUserId { get; set; }
        public ReportReason Reason { get; set; }
        public ReportStatus Status { get; set; }
        public string ReporterDisplay { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
    }
}
