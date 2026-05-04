using System.Security.Claims;
using FindActivity.Application.Interfaces;
using FindActivity.Domain.Entities;
using FindActivity.Domain.Enums;
using FindActivity.Web.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IAppDbContext _db;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IAppDbContext db, ILogger<ReportsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Receives a report submission (modal form) from Activity Details / Profile Details pages.
    /// Validates, persists an open report, and redirects back with a TempData status message.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReportCreateViewModel model, CancellationToken cancellationToken)
    {
        var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (reporterId is null)
        {
            return Forbid();
        }

        // Per-target validation: exactly one of the two target IDs must be set, matching TargetType.
        switch (model.TargetType)
        {
            case ReportTargetType.Activity when model.TargetActivityId is null:
                ModelState.AddModelError(nameof(model.TargetActivityId), "Activity to report is missing.");
                break;
            case ReportTargetType.User when string.IsNullOrWhiteSpace(model.TargetUserId):
                ModelState.AddModelError(nameof(model.TargetUserId), "User to report is missing.");
                break;
        }

        if (!ModelState.IsValid)
        {
            TempData["ReportError"] = "We couldn't submit your report. Please try again.";
            return RedirectSafely(model.ReturnUrl);
        }

        // Reject self-reports — user can't report themselves or their own activities.
        if (model.TargetType == ReportTargetType.User && model.TargetUserId == reporterId)
        {
            TempData["ReportError"] = "You can't report yourself.";
            return RedirectSafely(model.ReturnUrl);
        }

        if (model.TargetType == ReportTargetType.Activity && model.TargetActivityId is { } activityId)
        {
            var activity = await _db.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

            if (activity is null)
            {
                return NotFound();
            }

            if (activity.CreatedByUserId == reporterId)
            {
                TempData["ReportError"] = "You can't report your own activity.";
                return RedirectSafely(model.ReturnUrl);
            }
        }
        else if (model.TargetType == ReportTargetType.User && model.TargetUserId is { } targetUserId)
        {
            var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == targetUserId, cancellationToken);
            if (!userExists)
            {
                return NotFound();
            }
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterId,
            TargetType = model.TargetType,
            TargetActivityId = model.TargetType == ReportTargetType.Activity ? model.TargetActivityId : null,
            TargetUserId = model.TargetType == ReportTargetType.User ? model.TargetUserId : null,
            Reason = model.Reason,
            Details = model.Details ?? string.Empty,
            Status = ReportStatus.Open,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {ReporterId} filed report {ReportId} against {TargetType} (Activity={ActivityId}, User={UserId}) with reason {Reason}.",
            reporterId, report.Id, model.TargetType, report.TargetActivityId, report.TargetUserId, report.Reason);

        TempData["ReportSuccess"] = "Thanks — your report has been submitted. Our team will review it.";
        return RedirectSafely(model.ReturnUrl);
    }

    /// <summary>
    /// Send the user back to <paramref name="returnUrl"/> if it's a local URL; otherwise
    /// fall back to the activities index. Prevents open-redirect via the form's ReturnUrl field.
    /// </summary>
    private IActionResult RedirectSafely(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Activities");
    }
}
