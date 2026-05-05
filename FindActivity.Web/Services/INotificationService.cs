namespace FindActivity.Web.Services;

/// <summary>
/// High-level notifications for activity-related events. Wraps the underlying email sender
/// and gives templated, content-specific helpers so callers don't construct emails manually.
/// </summary>
public interface INotificationService
{
    Task SendActivityJoinedAsync(string toEmail, string toName, ActivityEmailContext context, CancellationToken cancellationToken = default);

    Task SendActivityCancelledAsync(string toEmail, string toName, ActivityEmailContext context, CancellationToken cancellationToken = default);

    Task SendActivityReminderAsync(string toEmail, string toName, ActivityEmailContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of activity data needed to render a notification email. The caller is expected
/// to populate this with already-loaded data; the notification service does no extra DB queries.
/// </summary>
public record ActivityEmailContext(
    string Title,
    DateTime StartUtc,
    int DurationMinutes,
    string Address,
    string City,
    string State,
    string HostDisplayName,
    string DetailsUrl);
