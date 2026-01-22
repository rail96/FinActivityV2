using FindActivity.Application.Dtos;

namespace FindActivity.Application.Services;

public interface IActivityService
{
    Task<ActivityDetailsDto?> GetDetailsAsync(Guid id, string? currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityListItemDto>> SearchAsync(ActivitySearchParamsDto filters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityWithParticipantsDto>> GetHostedActivitiesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityWithParticipantsDto>> GetParticipatingActivitiesAsync(string userId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ActivityCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ActivityEditDto dto, string userId, CancellationToken cancellationToken = default);
    Task<bool> JoinAsync(Guid activityId, string userId, CancellationToken cancellationToken = default);
    Task<bool> LeaveAsync(Guid activityId, string userId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid activityId, string userId, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(Guid activityId, CancellationToken cancellationToken = default);
}
