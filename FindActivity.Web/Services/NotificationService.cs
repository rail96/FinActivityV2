using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace FindActivity.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEmailSender emailSender, ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public Task SendActivityJoinedAsync(string toEmail, string toName, ActivityEmailContext ctx, CancellationToken cancellationToken = default)
    {
        var subject = $"You're going: {ctx.Title}";
        var body = WrapEmail(
            $"<h2>You're in!</h2>" +
            $"<p>Hi {Encode(toName)}, you've successfully RSVP'd to <strong>{Encode(ctx.Title)}</strong>.</p>" +
            BuildActivityCard(ctx) +
            $"<p>If your plans change, please leave the activity from the page above so others can join.</p>");
        return SendSafelyAsync(toEmail, subject, body, "join confirmation");
    }

    public Task SendActivityCancelledAsync(string toEmail, string toName, ActivityEmailContext ctx, CancellationToken cancellationToken = default)
    {
        var subject = $"Cancelled: {ctx.Title}";
        var body = WrapEmail(
            $"<h2>Activity cancelled</h2>" +
            $"<p>Hi {Encode(toName)}, the host has cancelled <strong>{Encode(ctx.Title)}</strong>. " +
            $"You don't need to take any action.</p>" +
            BuildActivityCard(ctx) +
            $"<p>Sorry for the disruption — there are plenty of other activities on FindActivity.</p>");
        return SendSafelyAsync(toEmail, subject, body, "cancellation");
    }

    public Task SendActivityReminderAsync(string toEmail, string toName, ActivityEmailContext ctx, CancellationToken cancellationToken = default)
    {
        var subject = $"Reminder: {ctx.Title} is tomorrow";
        var body = WrapEmail(
            $"<h2>See you tomorrow!</h2>" +
            $"<p>Hi {Encode(toName)}, this is a reminder that <strong>{Encode(ctx.Title)}</strong> is happening within the next 24 hours.</p>" +
            BuildActivityCard(ctx) +
            $"<p>If you can't make it anymore, please leave the activity from the page above.</p>");
        return SendSafelyAsync(toEmail, subject, body, "24h reminder");
    }

    public Task SendWaitlistPromotedAsync(string toEmail, string toName, ActivityEmailContext ctx, CancellationToken cancellationToken = default)
    {
        var subject = $"You're off the waitlist: {ctx.Title}";
        var body = WrapEmail(
            $"<h2>You're in!</h2>" +
            $"<p>Hi {Encode(toName)}, a slot opened up and you've been promoted from the waitlist for " +
            $"<strong>{Encode(ctx.Title)}</strong>. You're now a confirmed participant.</p>" +
            BuildActivityCard(ctx) +
            $"<p>If your plans have changed, please leave the activity from the page above so the next person on the waitlist can take the spot.</p>");
        return SendSafelyAsync(toEmail, subject, body, "waitlist promotion");
    }

    /// <summary>
    /// Wraps content with an outer email shell. Plain inline styles only — many mail clients
    /// strip stylesheets, so we keep this minimal and rely on default rendering.
    /// </summary>
    private static string WrapEmail(string innerHtml) =>
        $"<div style=\"font-family: -apple-system, Segoe UI, Roboto, sans-serif; color: #222; max-width: 560px;\">" +
        innerHtml +
        $"<hr><p style=\"font-size: 12px; color: #888;\">FindActivity &middot; You're receiving this because you have an account on FindActivity.</p>" +
        $"</div>";

    private static string BuildActivityCard(ActivityEmailContext ctx) =>
        $"<div style=\"border:1px solid #ddd; border-radius:6px; padding:16px; margin:12px 0;\">" +
        $"<p style=\"margin:0 0 8px 0;\"><strong>{Encode(ctx.Title)}</strong></p>" +
        $"<p style=\"margin:0;\">When: {ctx.StartUtc:f} UTC ({ctx.DurationMinutes} min)</p>" +
        $"<p style=\"margin:0;\">Where: {Encode(ctx.Address)}, {Encode(ctx.City)}, {Encode(ctx.State)}</p>" +
        $"<p style=\"margin:0;\">Host: {Encode(ctx.HostDisplayName)}</p>" +
        $"<p style=\"margin:8px 0 0 0;\"><a href=\"{Encode(ctx.DetailsUrl)}\">View activity</a></p>" +
        $"</div>";

    private static string Encode(string? s) => HtmlEncoder.Default.Encode(s ?? string.Empty);

    /// <summary>
    /// Wraps the email send so notification failures don't blow up the request. We log and move on —
    /// the underlying EmailSender already handles unconfigured / failed sends gracefully.
    /// </summary>
    private async Task SendSafelyAsync(string toEmail, string subject, string body, string kind)
    {
        try
        {
            await _emailSender.SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Kind} email to {Email}.", kind, toEmail);
        }
    }
}
