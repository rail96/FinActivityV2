using FindActivity.Domain.Enums;

namespace FindActivity.Domain.Entities;

/// <summary>
/// A user-submitted report against either an Activity or another User. Polymorphic via
/// <see cref="TargetType"/> and the two nullable foreign keys; exactly one of them is populated.
/// </summary>
public class Report
{
    public Guid Id { get; set; }

    /// <summary>The user who filed this report.</summary>
    public string ReporterUserId { get; set; } = string.Empty;
    public ApplicationUser? ReporterUser { get; set; }

    public ReportTargetType TargetType { get; set; }

    /// <summary>Set when <see cref="TargetType"/> is <see cref="ReportTargetType.Activity"/>.</summary>
    public Guid? TargetActivityId { get; set; }
    public Activity? TargetActivity { get; set; }

    /// <summary>Set when <see cref="TargetType"/> is <see cref="ReportTargetType.User"/>.</summary>
    public string? TargetUserId { get; set; }
    public ApplicationUser? TargetUser { get; set; }

    public ReportReason Reason { get; set; }

    /// <summary>Free-text context from the reporter (max 2,000 chars).</summary>
    public string Details { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Resolution (filled in by an admin/moderator).
    public DateTime? ResolvedUtc { get; set; }
    public string? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
    public string? ResolutionNotes { get; set; }
}
