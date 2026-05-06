using System.Security.Claims;
using FindActivity.Application.Dtos;
using FindActivity.Application.Interfaces;
using FindActivity.Application.Services;
using FindActivity.Domain.Entities;
using FindActivity.Domain.Enums;
using FindActivity.Web.Models.Activities;
using FindActivity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FindActivity.Web.Controllers;

public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        IActivityService activityService,
        IAppDbContext db,
        INotificationService notifications,
        ILogger<ActivitiesController> logger)
    {
        _activityService = activityService;
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromQuery] ActivitySearchViewModel model, CancellationToken cancellationToken)
    {
        var filters = new ActivitySearchParamsDto
        {
            City = model.City,
            CategoryId = model.CategoryId,
            StartUtcFrom = model.StartUtcFrom,
            StartUtcTo = model.StartUtcTo
        };

        model.Results = await _activityService.SearchAsync(filters, cancellationToken);
        model.Categories = await GetCategoriesAsync(cancellationToken);
        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> MyHosted(ActivityStatus? status, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        // Pull the full set first so we can compute per-status counts for the tabs.
        var allHosted = await _activityService.GetHostedActivitiesAsync(userId, statusFilter: null, cancellationToken);
        var filtered = status.HasValue
            ? allHosted.Where(a => a.Status == status.Value).ToList()
            : (IReadOnlyList<ActivityWithParticipantsDto>)allHosted;

        return View(new MyHostedViewModel
        {
            FilterStatus = status,
            Activities = filtered,
            TotalCount = allHosted.Count,
            ScheduledCount = allHosted.Count(a => a.Status == ActivityStatus.Scheduled),
            CancelledCount = allHosted.Count(a => a.Status == ActivityStatus.Cancelled),
            CompletedCount = allHosted.Count(a => a.Status == ActivityStatus.Completed)
        });
    }

    [Authorize]
    public async Task<IActionResult> MyParticipating(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var activities = await _activityService.GetParticipatingActivitiesAsync(userId, cancellationToken);
        return View(new ActivityListWithParticipantsViewModel
        {
            Heading = "Activities I'm attending",
            ShowHostLink = true,
            Activities = activities
        });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var activity = await _activityService.GetDetailsAsync(id, userId, cancellationToken);
        if (activity is null)
        {
            return NotFound();
        }

        var canReviewHost = false;
        if (userId is not null && activity.Status == ActivityStatus.Completed)
        {
            var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ActivityId == id && r.ReviewerUserId == userId, cancellationToken);
            canReviewHost = activity.IsUserJoined && !alreadyReviewed && activity.CreatedByUserId != userId;
        }

        var viewModel = new ActivityDetailsViewModel
        {
            Activity = activity,
            IsCreator = userId != null && activity.CreatedByUserId == userId,
            CanReviewHost = canReviewHost,
            CurrentUserId = userId
        };

        return View(viewModel);
    }

    [Authorize]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ActivityFormViewModel
        {
            Categories = await GetCategoriesAsync(cancellationToken)
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.AddressPlaceId))
        {
            ModelState.AddModelError(nameof(model.Address), "Select an address from the suggested results.");
        }

        if (model.StartUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(model.StartUtc), "Start time must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var id = await _activityService.CreateAsync(new ActivityCreateDto
        {
            Title = model.Title,
            Description = model.Description,
            CategoryId = model.CategoryId,
            StartUtc = model.StartUtc,
            DurationMinutes = model.DurationMinutes,
            Address = model.Address,
            City = model.City,
            State = model.State,
            AddressPlaceId = model.AddressPlaceId,
            Capacity = model.Capacity,
            MinAge = model.MinAge
        }, userId, cancellationToken);

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var activity = await _activityService.GetDetailsAsync(id, userId, cancellationToken);
        if (activity is null || activity.CreatedByUserId != userId)
        {
            return NotFound();
        }

        var model = new ActivityFormViewModel
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            CategoryId = await GetCategoryIdAsync(id, cancellationToken),
            StartUtc = activity.StartUtc,
            DurationMinutes = activity.DurationMinutes,
            Address = activity.Address,
            City = activity.City,
            State = activity.State,
            AddressPlaceId = activity.AddressPlaceId ?? string.Empty,
            Capacity = activity.Capacity,
            MinAge = activity.MinAge,
            Categories = await GetCategoriesAsync(cancellationToken)
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityFormViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.AddressPlaceId))
        {
            ModelState.AddModelError(nameof(model.Address), "Select an address from the suggested results.");
        }

        if (model.StartUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(model.StartUtc), "Start time must be in the future.");
        }

        if (model.Id is not null)
        {
            var currentJoined = await _db.ActivityParticipants
                .CountAsync(p => p.ActivityId == model.Id.Value && p.Status == ParticipantStatus.Joined, cancellationToken);
            if (model.Capacity < currentJoined)
            {
                ModelState.AddModelError(nameof(model.Capacity),
                    $"Capacity can't be below the current joined participants ({currentJoined}).");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null || model.Id is null)
        {
            return Forbid();
        }

        var updated = await _activityService.UpdateAsync(new ActivityEditDto
        {
            Id = model.Id.Value,
            Title = model.Title,
            Description = model.Description,
            CategoryId = model.CategoryId,
            StartUtc = model.StartUtc,
            DurationMinutes = model.DurationMinutes,
            Address = model.Address,
            City = model.City,
            State = model.State,
            AddressPlaceId = model.AddressPlaceId,
            Capacity = model.Capacity,
            MinAge = model.MinAge
        }, userId, cancellationToken);

        if (!updated)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var outcome = await _activityService.JoinAsync(id, userId, cancellationToken);
        switch (outcome)
        {
            case JoinOutcome.Joined:
                TempData["JoinMessage"] = "You're in! A confirmation email is on its way.";
                await SendJoinConfirmationAsync(id, userId, cancellationToken);
                break;
            case JoinOutcome.Waitlisted:
                TempData["JoinMessage"] = "Activity is full — you've been added to the waitlist. We'll email you if a spot opens up.";
                break;
            case JoinOutcome.AlreadyParticipating:
                TempData["JoinMessage"] = "You're already on this activity.";
                break;
            case JoinOutcome.NotAllowed:
                TempData["JoinError"] = "You can't join this activity right now.";
                break;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Loads the joiner + activity host info and dispatches an RSVP confirmation email.</summary>
    private async Task SendJoinConfirmationAsync(Guid activityId, string userId, CancellationToken cancellationToken)
    {
        var activity = await _db.Activities
            .AsNoTracking()
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null) return;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Email is null) return;

        var ctx = BuildContext(activity);
        await _notifications.SendActivityJoinedAsync(
            user.Email,
            user.DisplayName ?? user.UserName ?? "there",
            ctx,
            cancellationToken);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var leaveResult = await _activityService.LeaveAsync(id, userId, cancellationToken);
        if (leaveResult.Success && leaveResult.PromotedUserId is not null)
        {
            await SendWaitlistPromotionAsync(id, leaveResult.PromotedUserId, cancellationToken);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Loads the promoted user + activity context and sends the waitlist promotion email.</summary>
    private async Task SendWaitlistPromotionAsync(Guid activityId, string promotedUserId, CancellationToken cancellationToken)
    {
        var activity = await _db.Activities
            .AsNoTracking()
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null) return;

        var promoted = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == promotedUserId, cancellationToken);
        if (promoted?.Email is null) return;

        var ctx = BuildContext(activity);
        await _notifications.SendWaitlistPromotedAsync(
            promoted.Email,
            promoted.DisplayName ?? promoted.UserName ?? "there",
            ctx,
            cancellationToken);

        _logger.LogInformation("Promoted user {UserId} from waitlist for activity {ActivityId}.", promotedUserId, activityId);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        // Capture participant emails BEFORE cancellation in case the service later prunes them.
        // Includes waitlisted users — they should know the activity isn't happening.
        var recipients = await _db.ActivityParticipants
            .AsNoTracking()
            .Where(p => p.ActivityId == id
                        && (p.Status == ParticipantStatus.Joined || p.Status == ParticipantStatus.Waitlisted)
                        && p.UserId != userId)
            .Select(p => new { p.UserId, p.User!.Email, p.User.UserName, p.User.DisplayName })
            .ToListAsync(cancellationToken);

        var cancelled = await _activityService.CancelAsync(id, userId, cancellationToken);
        if (cancelled)
        {
            var activity = await _db.Activities
                .AsNoTracking()
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (activity is not null)
            {
                var ctx = BuildContext(activity);
                foreach (var r in recipients.Where(r => !string.IsNullOrEmpty(r.Email)))
                {
                    await _notifications.SendActivityCancelledAsync(
                        r.Email!,
                        r.DisplayName ?? r.UserName ?? "there",
                        ctx,
                        cancellationToken);
                }
                _logger.LogInformation("Sent cancellation emails for activity {ActivityId} to {Count} participants.",
                    id, recipients.Count);
            }
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Builds an ActivityEmailContext from a loaded Activity (with CreatedByUser).</summary>
    private ActivityEmailContext BuildContext(Activity activity)
    {
        var hostDisplay = activity.CreatedByUser?.DisplayName
                          ?? activity.CreatedByUser?.UserName
                          ?? "the host";
        var detailsUrl = Url.Action(nameof(Details), "Activities", new { id = activity.Id }, Request.Scheme)
                         ?? string.Empty;
        return new ActivityEmailContext(
            activity.Title,
            activity.StartUtc,
            activity.DurationMinutes,
            activity.Address,
            activity.City,
            activity.State,
            hostDisplay,
            detailsUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var activity = await _activityService.GetDetailsAsync(id, userId, cancellationToken);
        if (activity is null || activity.CreatedByUserId != userId)
        {
            return Forbid();
        }

        await _activityService.CompleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IEnumerable<SelectListItem>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync(cancellationToken);
    }

    private async Task<int> GetCategoryIdAsync(Guid activityId, CancellationToken cancellationToken)
    {
        return await _db.Activities
            .Where(a => a.Id == activityId)
            .Select(a => a.CategoryId)
            .FirstAsync(cancellationToken);
    }
}
