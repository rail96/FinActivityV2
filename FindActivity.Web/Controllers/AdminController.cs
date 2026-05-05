using System.Security.Claims;
using FindActivity.Application.Interfaces;
using FindActivity.Domain.Entities;
using FindActivity.Domain.Enums;
using FindActivity.Web.Models.Admin;
using FindActivity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Controllers;

/// <summary>
/// Moderator dashboard. All actions require the "Admin" role (seeded by <see cref="RoleSeeder"/>).
/// </summary>
[Authorize(Roles = RoleSeeder.AdminRole)]
public class AdminController : Controller
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAppDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Reports list, filterable by status. Defaults to Open.</summary>
    public async Task<IActionResult> Index(ReportStatus? status, CancellationToken cancellationToken)
    {
        var filter = status ?? ReportStatus.Open;

        var reportsQuery = _db.Reports
            .AsNoTracking()
            .Where(r => r.Status == filter)
            .OrderByDescending(r => r.CreatedUtc);

        // Project to the list-item shape, joining target labels in.
        var reports = await reportsQuery
            .Select(r => new
            {
                r.Id,
                r.TargetType,
                r.TargetActivityId,
                r.TargetUserId,
                r.Reason,
                r.Status,
                r.CreatedUtc,
                ReporterDisplay = r.ReporterUser != null
                    ? (r.ReporterUser.DisplayName ?? r.ReporterUser.UserName ?? r.ReporterUserId)
                    : r.ReporterUserId,
                ActivityTitle = r.TargetActivity != null ? r.TargetActivity.Title : null,
                TargetUserDisplay = r.TargetUser != null
                    ? (r.TargetUser.DisplayName ?? r.TargetUser.UserName ?? r.TargetUserId)
                    : null
            })
            .ToListAsync(cancellationToken);

        var counts = await _db.Reports
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vm = new AdminReportsIndexViewModel
        {
            FilterStatus = filter,
            OpenCount = counts.FirstOrDefault(c => c.Status == ReportStatus.Open)?.Count ?? 0,
            ActionTakenCount = counts.FirstOrDefault(c => c.Status == ReportStatus.ActionTaken)?.Count ?? 0,
            DismissedCount = counts.FirstOrDefault(c => c.Status == ReportStatus.Dismissed)?.Count ?? 0,
            Reports = reports.Select(r => new AdminReportsIndexViewModel.ReportListItem
            {
                Id = r.Id,
                TargetType = r.TargetType,
                TargetActivityId = r.TargetActivityId,
                TargetUserId = r.TargetUserId,
                TargetLabel = r.TargetType == ReportTargetType.Activity
                    ? (r.ActivityTitle ?? "(deleted activity)")
                    : (r.TargetUserDisplay ?? "(deleted user)"),
                Reason = r.Reason,
                Status = r.Status,
                ReporterDisplay = r.ReporterDisplay ?? string.Empty,
                CreatedUtc = r.CreatedUtc
            }).ToList()
        };

        return View(vm);
    }

    /// <summary>Full report detail with action buttons.</summary>
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var report = await _db.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.TargetActivity)
                .ThenInclude(a => a!.CreatedByUser)
            .Include(r => r.TargetUser)
            .Include(r => r.ResolvedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return NotFound();
        }

        // For a user report, "TargetUser" is direct; for an activity report, the activity's host
        // is also useful context (often the point of the report).
        var targetUser = report.TargetUser
            ?? report.TargetActivity?.CreatedByUser;

        var relatedQuery = _db.Reports.AsNoTracking().Where(r => r.Id != report.Id);
        if (report.TargetType == ReportTargetType.Activity && report.TargetActivityId is { } activityId)
        {
            relatedQuery = relatedQuery.Where(r => r.TargetActivityId == activityId);
        }
        else if (report.TargetType == ReportTargetType.User && report.TargetUserId is { } userId)
        {
            relatedQuery = relatedQuery.Where(r => r.TargetUserId == userId);
        }

        var related = await relatedQuery
            .OrderByDescending(r => r.CreatedUtc)
            .Take(20)
            .Select(r => new AdminReportDetailsViewModel.RelatedReport
            {
                Id = r.Id,
                Reason = r.Reason,
                Status = r.Status,
                CreatedUtc = r.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        var vm = new AdminReportDetailsViewModel
        {
            Report = report,
            Reporter = report.ReporterUser,
            TargetActivity = report.TargetActivity,
            TargetUser = targetUser,
            RelatedReports = related
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(Guid id, string? notes, CancellationToken cancellationToken)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        ResolveReport(report, ReportStatus.Dismissed, notes);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["AdminMessage"] = "Report dismissed.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Suspend the target user for the given number of days.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, int days, string? notes, CancellationToken cancellationToken)
    {
        if (days < 1 || days > 3650)
        {
            return BadRequest("Suspension must be between 1 and 3,650 days.");
        }

        var report = await _db.Reports
            .Include(r => r.TargetActivity)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var targetUserId = report.TargetUserId
            ?? report.TargetActivity?.CreatedByUserId;
        if (targetUserId is null)
        {
            return BadRequest("Report has no associated user to suspend.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (user is null)
        {
            return NotFound("Target user no longer exists.");
        }

        user.BannedUntilUtc = DateTime.UtcNow.AddDays(days);
        // Rotating the security stamp invalidates any active auth cookie, so a suspended
        // user gets booted on their next request (within the validation interval, ~30 min by default).
        user.SecurityStamp = Guid.NewGuid().ToString();
        ResolveReport(report, ReportStatus.ActionTaken,
            string.IsNullOrWhiteSpace(notes)
                ? $"Suspended user for {days} days."
                : $"Suspended user for {days} days. {notes}");

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {AdminId} suspended user {UserId} until {Until} via report {ReportId}.",
            User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id, user.BannedUntilUtc, report.Id);

        TempData["AdminMessage"] = $"User suspended until {user.BannedUntilUtc:yyyy-MM-dd HH:mm} UTC.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Long-term suspension (365 days). Effectively a ban until appealed.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Ban(Guid id, string? notes, CancellationToken cancellationToken)
        => Suspend(id, days: 365, notes ?? "Banned.", cancellationToken);

    /// <summary>Cancel the activity that's being reported.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveActivity(Guid id, string? notes, CancellationToken cancellationToken)
    {
        var report = await _db.Reports
            .Include(r => r.TargetActivity)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        if (report.TargetType != ReportTargetType.Activity || report.TargetActivity is null)
        {
            return BadRequest("This report doesn't target an activity.");
        }

        report.TargetActivity.Status = ActivityStatus.Cancelled;
        ResolveReport(report, ReportStatus.ActionTaken,
            string.IsNullOrWhiteSpace(notes)
                ? "Activity removed (cancelled)."
                : $"Activity removed (cancelled). {notes}");

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Admin {AdminId} cancelled activity {ActivityId} via report {ReportId}.",
            User.FindFirstValue(ClaimTypes.NameIdentifier), report.TargetActivityId, report.Id);

        TempData["AdminMessage"] = "Activity removed.";
        return RedirectToAction(nameof(Index));
    }

    private void ResolveReport(Report report, ReportStatus status, string? notes)
    {
        report.Status = status;
        report.ResolvedUtc = DateTime.UtcNow;
        report.ResolvedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        report.ResolutionNotes = notes;
    }
}
