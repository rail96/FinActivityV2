using System.Security.Claims;
using FindActivity.Application.Dtos;
using FindActivity.Application.Interfaces;
using FindActivity.Application.Services;
using FindActivity.Domain.Enums;
using FindActivity.Web.Models.Activities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FindActivity.Web.Controllers;

public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IAppDbContext _db;

    public ActivitiesController(IActivityService activityService, IAppDbContext db)
    {
        _activityService = activityService;
        _db = db;
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
    public async Task<IActionResult> MyHosted(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var activities = await _activityService.GetHostedActivitiesAsync(userId, cancellationToken);
        return View(new ActivityListWithParticipantsViewModel
        {
            Heading = "My hosted activities",
            ShowHostLink = false,
            Activities = activities
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

        await _activityService.JoinAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
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

        await _activityService.LeaveAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
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

        await _activityService.CancelAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
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
