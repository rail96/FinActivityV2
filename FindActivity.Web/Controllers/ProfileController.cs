using System.Security.Claims;
using FindActivity.Application.Interfaces;
using FindActivity.Web.Models.Profile;
using FindActivity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Controllers;

public class ProfileController : Controller
{
    private readonly IAppDbContext _db;
    private readonly AvatarImageService _avatars;

    public ProfileController(IAppDbContext db, AvatarImageService avatars)
    {
        _db = db;
        _avatars = avatars;
    }

    public async Task<IActionResult> Details(string? id, CancellationToken cancellationToken)
    {
        var userId = id ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return NotFound();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // Join to AspNetUsers so each review carries the reviewer's display name + avatar for inline rendering.
        var reviews = await (from r in _db.Reviews
                             where r.RevieweeUserId == userId
                             join u in _db.Users on r.ReviewerUserId equals u.Id into reviewerJoin
                             from reviewer in reviewerJoin.DefaultIfEmpty()
                             orderby r.CreatedUtc descending
                             select new ProfileDetailsViewModel.ReviewSummary
                             {
                                 ActivityId = r.ActivityId,
                                 Rating = r.Rating,
                                 Comment = r.Comment,
                                 ReviewerUserId = r.ReviewerUserId,
                                 ReviewerDisplayName = reviewer != null ? (reviewer.DisplayName ?? reviewer.UserName) : null,
                                 ReviewerAvatarPath = reviewer != null ? reviewer.AvatarPath : null,
                                 CreatedUtc = r.CreatedUtc
                             }).ToListAsync(cancellationToken);

        return View(new ProfileDetailsViewModel
        {
            User = user,
            Reviews = reviews
        });
    }

    [Authorize]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return NotFound();

        return View(new ProfileEditViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarPath = user.AvatarPath
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return NotFound();

        // Optional avatar upload: replace if provided, keep otherwise.
        if (model.AvatarFile is not null)
        {
            var (savedPath, error) = await _avatars.SaveAsync(model.AvatarFile, cancellationToken);
            if (error is not null)
            {
                ModelState.AddModelError(nameof(model.AvatarFile), error);
                return View(model);
            }
            // Best-effort delete of the old image now that we have a new one.
            _avatars.TryDelete(user.AvatarPath);
            user.AvatarPath = savedPath;
        }

        user.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? null : model.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        TempData["ProfileMessage"] = "Profile updated.";
        return RedirectToAction(nameof(Details), new { id = userId });
    }
}
