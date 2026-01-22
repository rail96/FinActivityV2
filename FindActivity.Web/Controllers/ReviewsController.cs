using System.Security.Claims;
using FindActivity.Application.Dtos;
using FindActivity.Application.Interfaces;
using FindActivity.Application.Services;
using FindActivity.Domain.Enums;
using FindActivity.Web.Models.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IAppDbContext _db;

    public ReviewsController(IReviewService reviewService, IAppDbContext db)
    {
        _reviewService = reviewService;
        _db = db;
    }

    public async Task<IActionResult> Create(Guid activityId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var activity = await _db.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null || activity.Status != ActivityStatus.Completed)
        {
            return NotFound();
        }

        var endUtc = activity.StartUtc.AddMinutes(activity.DurationMinutes);
        if (endUtc > DateTime.UtcNow)
        {
            return Forbid();
        }

        var isJoined = activity.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined);
        if (!isJoined)
        {
            return Forbid();
        }

        if (activity.CreatedByUserId == userId)
        {
            return Forbid();
        }

        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ActivityId == activityId && r.ReviewerUserId == userId, cancellationToken);
        if (alreadyReviewed)
        {
            return RedirectToAction("Details", "Activities", new { id = activityId });
        }

        return View(new ReviewCreateViewModel
        {
            ActivityId = activityId,
            RevieweeUserId = activity.CreatedByUserId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var success = await _reviewService.CreateReviewAsync(new ReviewCreateDto
        {
            ActivityId = model.ActivityId,
            RevieweeUserId = model.RevieweeUserId,
            Rating = model.Rating,
            Comment = model.Comment
        }, userId, cancellationToken);

        return RedirectToAction("Details", "Activities", new { id = model.ActivityId });
    }
}
