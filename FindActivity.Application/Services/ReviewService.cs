using FindActivity.Application.Dtos;
using FindActivity.Application.Interfaces;
using FindActivity.Domain.Entities;
using FindActivity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IAppDbContext _db;

    public ReviewService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CreateReviewAsync(ReviewCreateDto dto, string reviewerUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Rating is < 1 or > 5)
        {
            return false;
        }

        var activity = await _db.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == dto.ActivityId, cancellationToken);

        if (activity is null || activity.Status != ActivityStatus.Completed)
        {
            return false;
        }

        var endUtc = activity.StartUtc.AddMinutes(activity.DurationMinutes);
        if (endUtc > DateTime.UtcNow)
        {
            return false;
        }

        var isJoined = activity.Participants.Any(p => p.UserId == reviewerUserId && p.Status == ParticipantStatus.Joined);
        if (!isJoined)
        {
            return false;
        }

        if (dto.RevieweeUserId == reviewerUserId)
        {
            return false;
        }

        if (dto.RevieweeUserId != activity.CreatedByUserId)
        {
            return false;
        }

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.ActivityId == dto.ActivityId && r.ReviewerUserId == reviewerUserId, cancellationToken);
        if (alreadyReviewed)
        {
            return false;
        }

        _db.Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            ActivityId = dto.ActivityId,
            ReviewerUserId = reviewerUserId,
            RevieweeUserId = dto.RevieweeUserId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        await RecomputeUserRatingAsync(dto.RevieweeUserId, cancellationToken);
        return true;
    }

    private async Task RecomputeUserRatingAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var ratings = await _db.Reviews
            .Where(r => r.RevieweeUserId == userId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
        {
            user.RatingAvg = 0;
            user.RatingCount = 0;
        }
        else
        {
            user.RatingCount = ratings.Count;
            user.RatingAvg = ratings.Average();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
