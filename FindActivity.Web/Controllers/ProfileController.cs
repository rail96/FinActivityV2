using System.Security.Claims;
using FindActivity.Application.Interfaces;
using FindActivity.Web.Models.Profile;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Controllers;

public class ProfileController : Controller
{
    private readonly IAppDbContext _db;

    public ProfileController(IAppDbContext db)
    {
        _db = db;
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

        var reviews = await _db.Reviews
            .Where(r => r.RevieweeUserId == userId)
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new ProfileDetailsViewModel.ReviewSummary
            {
                ActivityId = r.ActivityId,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewerUserId = r.ReviewerUserId,
                CreatedUtc = r.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return View(new ProfileDetailsViewModel
        {
            User = user,
            Reviews = reviews
        });
    }
}
