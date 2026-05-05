using FindActivity.Application.Interfaces;
using FindActivity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FindActivity.Web.Services;

/// <summary>
/// Background service that sends a reminder email ~24 hours before each scheduled activity starts.
///
/// Polls every <see cref="PollInterval"/> for Scheduled activities starting in the [24h, 25h) window
/// where <c>ReminderSentUtc</c> is null. Once it fires the reminder, sets <c>ReminderSentUtc</c> so the
/// next tick won't double-send. Idempotent across restarts.
/// </summary>
public class ActivityReminderService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<ActivityReminderService> _logger;
    private readonly string _baseUrl;

    public ActivityReminderService(IServiceProvider services, ILogger<ActivityReminderService> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5010";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Slight initial delay so we don't compete with app startup work.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, "ActivityReminderService tick failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var nowUtc = DateTime.UtcNow;
        var windowStart = nowUtc.AddHours(24);
        var windowEnd = nowUtc.AddHours(25);

        var dueActivities = await db.Activities
            .Include(a => a.CreatedByUser)
            .Where(a => a.Status == ActivityStatus.Scheduled
                        && a.ReminderSentUtc == null
                        && a.StartUtc >= windowStart
                        && a.StartUtc < windowEnd)
            .ToListAsync(cancellationToken);

        if (dueActivities.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Sending 24h reminders for {Count} activities.", dueActivities.Count);

        foreach (var activity in dueActivities)
        {
            var participants = await db.ActivityParticipants
                .AsNoTracking()
                .Where(p => p.ActivityId == activity.Id && p.Status == ParticipantStatus.Joined)
                .Select(p => new { p.User!.Email, p.User.UserName, p.User.DisplayName })
                .ToListAsync(cancellationToken);

            var hostDisplay = activity.CreatedByUser?.DisplayName
                              ?? activity.CreatedByUser?.UserName
                              ?? "the host";
            var detailsUrl = $"{_baseUrl.TrimEnd('/')}/Activities/Details/{activity.Id}";
            var ctx = new ActivityEmailContext(
                activity.Title,
                activity.StartUtc,
                activity.DurationMinutes,
                activity.Address,
                activity.City,
                activity.State,
                hostDisplay,
                detailsUrl);

            foreach (var p in participants.Where(p => !string.IsNullOrEmpty(p.Email)))
            {
                await notifications.SendActivityReminderAsync(
                    p.Email!,
                    p.DisplayName ?? p.UserName ?? "there",
                    ctx,
                    cancellationToken);
            }

            activity.ReminderSentUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
