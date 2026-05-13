using FindActivity.Application.Dtos;
using FindActivity.Application.Geo;
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
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Capacity = dto.Capacity,
            MinAge = dto.MinAge,
            CoverImagePath = dto.CoverImagePath,
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
        activity.Latitude = dto.Latitude;
        activity.Longitude = dto.Longitude;
        activity.Capacity = dto.Capacity;
        activity.MinAge = dto.MinAge;
        // Only overwrite the cover image when the caller actually provided one. Allows "keep current image" semantics on edit.
        if (dto.CoverImagePath is not null)
        {
            activity.CoverImagePath = dto.CoverImagePath;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ActivityDetailsDto?> GetDetailsAsync(Guid id, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities
            .Include(a => a.Category)
            .Include(a => a.CreatedByUser)
            .Include(a => a.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return null;
        }

        var joinedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        var waitlistedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Waitlisted);
        var isUserJoined = currentUserId != null && activity.Participants.Any(p => p.UserId == currentUserId && p.Status == ParticipantStatus.Joined);

        // Compute the current user's position on the waitlist (1-based) by ordering by JoinedUtc.
        int? userWaitlistPosition = null;
        bool isUserWaitlisted = false;
        if (currentUserId is not null)
        {
            var orderedWaitlist = activity.Participants
                .Where(p => p.Status == ParticipantStatus.Waitlisted)
                .OrderBy(p => p.JoinedUtc)
                .Select((p, idx) => new { p.UserId, Position = idx + 1 })
                .FirstOrDefault(x => x.UserId == currentUserId);
            if (orderedWaitlist is not null)
            {
                isUserWaitlisted = true;
                userWaitlistPosition = orderedWaitlist.Position;
            }
        }

        var participants = GetJoinedAndWaitlistedParticipants(activity);

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
            Latitude = activity.Latitude,
            Longitude = activity.Longitude,
            Capacity = activity.Capacity,
            MinAge = activity.MinAge,
            CoverImagePath = activity.CoverImagePath,
            CreatedByUserId = activity.CreatedByUserId,
            HostDisplayName = activity.CreatedByUser?.DisplayName ?? activity.CreatedByUser?.UserName,
            HostAvatarPath = activity.CreatedByUser?.AvatarPath,
            HostPhoneVerified = activity.CreatedByUser?.PhoneNumberConfirmed ?? false,
            Status = activity.Status,
            JoinedCount = joinedCount,
            WaitlistedCount = waitlistedCount,
            IsUserJoined = isUserJoined,
            IsUserWaitlisted = isUserWaitlisted,
            UserWaitlistPosition = userWaitlistPosition,
            Participants = participants
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

        // Distance filter runs in memory after the SQL query — SQL Server has no built-in haversine
        // and the result set here is already small (filtered by city/category/date). Activities without
        // coordinates are excluded when a distance filter is active.
        if (filters.MaxDistanceKm is { } maxKm)
        {
            var centerLat = filters.CenterLat ?? GeoMath.SeattleLat;
            var centerLng = filters.CenterLng ?? GeoMath.SeattleLng;
            results = results
                .Where(a => a.Latitude.HasValue && a.Longitude.HasValue
                            && GeoMath.DistanceKm(centerLat, centerLng, a.Latitude.Value, a.Longitude.Value) <= maxKm)
                .ToList();
        }

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
            Status = a.Status,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
            CoverImagePath = a.CoverImagePath
        }).ToList();
    }

    public async Task<IReadOnlyList<ActivityWithParticipantsDto>> GetHostedActivitiesAsync(string userId, ActivityStatus? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Activities
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Participants)
            .ThenInclude(p => p.User)
            .Where(a => a.CreatedByUserId == userId);

        if (statusFilter.HasValue)
        {
            query = query.Where(a => a.Status == statusFilter.Value);
        }

        var activities = await query.ToListAsync(cancellationToken);

        // Order: Scheduled by StartUtc ascending (earliest first), others by StartUtc descending (most recent first).
        var ordered = activities
            .OrderBy(a => a.Status == ActivityStatus.Scheduled ? 0 : 1)
            .ThenBy(a => a.Status == ActivityStatus.Scheduled ? a.StartUtc : DateTime.MaxValue)
            .ThenByDescending(a => a.StartUtc)
            .ToList();

        return ordered.Select(MapActivityWithParticipants).ToList();
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

    public async Task<JoinOutcome> JoinAsync(Guid activityId, string userId, CancellationToken cancellationToken = default)
    {
        var activity = await _db.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null || activity.Status != ActivityStatus.Scheduled)
        {
            return JoinOutcome.NotAllowed;
        }

        if (activity.CreatedByUserId == userId)
        {
            return JoinOutcome.NotAllowed;
        }

        var joinedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        var hasCapacity = joinedCount < activity.Capacity;

        var existing = activity.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is not null)
        {
            if (existing.Status == ParticipantStatus.Joined || existing.Status == ParticipantStatus.Waitlisted)
            {
                return JoinOutcome.AlreadyParticipating;
            }

            // Re-joining after Left/Removed: take the next available slot, otherwise waitlist.
            existing.Status = hasCapacity ? ParticipantStatus.Joined : ParticipantStatus.Waitlisted;
            existing.JoinedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return hasCapacity ? JoinOutcome.Joined : JoinOutcome.Waitlisted;
        }

        var newStatus = hasCapacity ? ParticipantStatus.Joined : ParticipantStatus.Waitlisted;
        _db.ActivityParticipants.Add(new ActivityParticipant
        {
            ActivityId = activity.Id,
            UserId = userId,
            Status = newStatus,
            JoinedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return hasCapacity ? JoinOutcome.Joined : JoinOutcome.Waitlisted;
    }

    public async Task<LeaveResult> LeaveAsync(Guid activityId, string userId, CancellationToken cancellationToken = default)
    {
        var participant = await _db.ActivityParticipants
            .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.UserId == userId, cancellationToken);

        if (participant is null)
        {
            return new LeaveResult(false, null);
        }

        var wasJoined = participant.Status == ParticipantStatus.Joined;
        participant.Status = ParticipantStatus.Left;

        // If a confirmed participant just freed a slot, promote the oldest waitlisted.
        string? promotedUserId = null;
        if (wasJoined)
        {
            var nextUp = await _db.ActivityParticipants
                .Where(p => p.ActivityId == activityId && p.Status == ParticipantStatus.Waitlisted)
                .OrderBy(p => p.JoinedUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextUp is not null)
            {
                nextUp.Status = ParticipantStatus.Joined;
                // Keep the original JoinedUtc so the queue order is preserved and the promotion is a true upgrade.
                promotedUserId = nextUp.UserId;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new LeaveResult(true, promotedUserId);
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
            WaitlistedCount = activity.Participants.Count(p => p.Status == ParticipantStatus.Waitlisted),
            CoverImagePath = activity.CoverImagePath,
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

    /// <summary>
    /// Joined participants alphabetical, then waitlisted in queue order. Status is preserved on the DTO
    /// so the view can render the two groups distinctly.
    /// </summary>
    private static IReadOnlyList<ParticipantSummaryDto> GetJoinedAndWaitlistedParticipants(Activity activity)
    {
        var joined = activity.Participants
            .Where(p => p.Status == ParticipantStatus.Joined)
            .OrderBy(p => p.User?.DisplayName ?? p.User?.UserName ?? string.Empty)
            .Select(MapParticipant)
            .ToList();
        var waitlisted = activity.Participants
            .Where(p => p.Status == ParticipantStatus.Waitlisted)
            .OrderBy(p => p.JoinedUtc)
            .Select(MapParticipant)
            .ToList();
        return joined.Concat(waitlisted).ToList();
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
            RatingCount = user?.RatingCount ?? 0,
            AvatarPath = user?.AvatarPath,
            Status = participant.Status
        };
    }
}
