using FindActivity.Application.Dtos;
using FindActivity.Application.Interfaces;
using FindActivity.Domain.Entities;
using FindActivity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IAppDbContext _db;

    public ActivityService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(ActivityCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            CategoryId = dto.CategoryId,
            StartUtc = dto.StartUtc,
            DurationMinutes = dto.DurationMinutes,
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim(),
            AddressPlaceId = dto.AddressPlaceId?.Trim(),
            Capacity = dto.Capacity,
            MinAge = dto.MinAge,
            CreatedByUserId = userId,
            Status = ActivityStatus.Scheduled,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(cancellationToken);
        return activity.Id;
    }

    public async Task<bool> UpdateAsync(ActivityEditDto dto, string userId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken);
        if (activity is null || activity.CreatedByUserId != userId || activity.Status != ActivityStatus.Scheduled)
        {
            return false;
        }

        activity.Title = dto.Title.Trim();
        activity.Description = dto.Description.Trim();
        activity.CategoryId = dto.CategoryId;
        activity.StartUtc = dto.StartUtc;
        activity.DurationMinutes = dto.DurationMinutes;
        activity.Address = dto.Address.Trim();
        activity.City = dto.City.Trim();
        activity.State = dto.State.Trim();
        activity.AddressPlaceId = dto.AddressPlaceId?.Trim();
        activity.Capacity = dto.Capacity;
        activity.MinAge = dto.MinAge;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ActivityDetailsDto?> GetDetailsAsync(Guid id, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities
            .Include(a => a.Category)
            .Include(a => a.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return null;
        }

        var joinedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        var isUserJoined = currentUserId != null && activity.Participants.Any(p => p.UserId == currentUserId && p.Status == ParticipantStatus.Joined);

        var joinedParticipants = GetJoinedParticipants(activity);

        return new ActivityDetailsDto
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            CategoryName = activity.Category?.Name ?? string.Empty,
            StartUtc = activity.StartUtc,
            DurationMinutes = activity.DurationMinutes,
            Address = activity.Address,
            City = activity.City,
            State = activity.State,
            AddressPlaceId = activity.AddressPlaceId,
            Capacity = activity.Capacity,
            MinAge = activity.MinAge,
            CreatedByUserId = activity.CreatedByUserId,
            Status = activity.Status,
            JoinedCount = joinedCount,
            IsUserJoined = isUserJoined,
            Participants = joinedParticipants
        };
    }

    public async Task<IReadOnlyList<ActivityListItemDto>> SearchAsync(ActivitySearchParamsDto filters, CancellationToken cancellationToken = default)
    {
        var query = _db.Activities
            .Include(a => a.Category)
            .Include(a => a.Participants)
            .Where(a => a.Status == ActivityStatus.Scheduled)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.City))
        {
            var city = filters.City.Trim();
            query = query.Where(a => a.City == city);
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == filters.CategoryId.Value);
        }

        if (filters.StartUtcFrom.HasValue)
        {
            query = query.Where(a => a.StartUtc >= filters.StartUtcFrom.Value);
        }

        if (filters.StartUtcTo.HasValue)
        {
            query = query.Where(a => a.StartUtc <= filters.StartUtcTo.Value);
        }

        var results = await query
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);

        return results.Select(a => new ActivityListItemDto
        {
            Id = a.Id,
            Title = a.Title,
            Address = a.Address,
            City = a.City,
            State = a.State,
            CategoryName = a.Category?.Name ?? string.Empty,
            StartUtc = a.StartUtc,
            DurationMinutes = a.DurationMinutes,
            Capacity = a.Capacity,
            JoinedCount = a.Participants.Count(p => p.Status == ParticipantStatus.Joined),
            Status = a.Status
        }).ToList();
    }

    public async Task<IReadOnlyList<ActivityWithParticipantsDto>> GetHostedActivitiesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var activities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Participants)
            .ThenInclude(p => p.User)
            .Where(a => a.CreatedByUserId == userId)
            .OrderByDescending(a => a.StartUtc)
            .ToListAsync(cancellationToken);

        return activities.Select(MapActivityWithParticipants).ToList();
    }

    public async Task<IReadOnlyList<ActivityWithParticipantsDto>> GetParticipatingActivitiesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var activities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Participants)
            .ThenInclude(p => p.User)
            .Where(a => a.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined))
            .OrderByDescending(a => a.StartUtc)
            .ToListAsync(cancellationToken);

        return activities.Select(MapActivityWithParticipants).ToList();
    }

    public async Task<bool> JoinAsync(Guid activityId, string userId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null || activity.Status != ActivityStatus.Scheduled)
        {
            return false;
        }

        if (activity.CreatedByUserId == userId)
        {
            return false;
        }

        var existing = activity.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is not null)
        {
            if (existing.Status == ParticipantStatus.Joined)
            {
                return true;
            }

            existing.Status = ParticipantStatus.Joined;
            existing.JoinedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var joinedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        if (joinedCount >= activity.Capacity)
        {
            return false;
        }

        _db.ActivityParticipants.Add(new ActivityParticipant
        {
            ActivityId = activity.Id,
            UserId = userId,
            Status = ParticipantStatus.Joined,
            JoinedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> LeaveAsync(Guid activityId, string userId, CancellationToken cancellationToken = default)
    {
        var participant = await _db.ActivityParticipants
            .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.UserId == userId, cancellationToken);

        if (participant is null)
        {
            return false;
        }

        participant.Status = ParticipantStatus.Left;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid activityId, string userId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null || activity.CreatedByUserId != userId)
        {
            return false;
        }

        activity.Status = ActivityStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CompleteAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        if (activity is null || activity.Status != ActivityStatus.Scheduled)
        {
            return false;
        }

        if (activity.StartUtc.AddMinutes(activity.DurationMinutes) > DateTime.UtcNow)
        {
            return false;
        }

        activity.Status = ActivityStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ActivityWithParticipantsDto MapActivityWithParticipants(Activity activity)
    {
        return new ActivityWithParticipantsDto
        {
            Id = activity.Id,
            Title = activity.Title,
            City = activity.City,
            State = activity.State,
            CategoryName = activity.Category?.Name ?? string.Empty,
            StartUtc = activity.StartUtc,
            DurationMinutes = activity.DurationMinutes,
            Capacity = activity.Capacity,
            JoinedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Joined),
            Status = activity.Status,
            CreatedByUserId = activity.CreatedByUserId,
            Participants = GetJoinedParticipants(activity)
        };
    }

    private static IReadOnlyList<ParticipantSummaryDto> GetJoinedParticipants(Activity activity)
    {
        return activity.Participants
            .Where(p => p.Status == ParticipantStatus.Joined)
            .Select(MapParticipant)
            .OrderBy(p => p.DisplayName)
            .ToList();
    }

    private static ParticipantSummaryDto MapParticipant(ActivityParticipant participant)
    {
        var user = participant.User;
        var displayName = user?.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user?.UserName ?? "Unknown";
        }

        return new ParticipantSummaryDto
        {
            UserId = participant.UserId,
            DisplayName = displayName,
            Bio = user?.Bio,
            RatingAvg = user?.RatingAvg ?? 0,
            RatingCount = user?.RatingCount ?? 0
        };
    }
}
