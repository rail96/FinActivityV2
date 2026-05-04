using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FindActivity.Web.Services;

/// <summary>
/// SendGrid implementation of <see cref="IEmailSender"/>. ASP.NET Core Identity resolves this
/// automatically for confirmation emails, password reset links, and other account messages.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly EmailSenderOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSenderOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            // Fail loudly in logs but don't throw — we don't want signup to crash if email config is missing in dev.
            _logger.LogWarning(
                "SendGrid ApiKey is not configured. Skipping email to {Email} with subject '{Subject}'.",
                email, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning(
                "SendGrid FromEmail is not configured. Skipping email to {Email} with subject '{Subject}'.",
                email, subject);
            return;
        }

        var client = new SendGridClient(_options.ApiKey);
        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(email);
        // Strip HTML for the plain-text fallback so recipients without HTML still get a readable body.
        var plainText = System.Text.RegularExpressions.Regex.Replace(htmlMessage, "<.*?>", string.Empty);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlMessage);

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError(
                "SendGrid failed to send email to {Email}. Status: {Status}. Body: {Body}",
                email, response.StatusCode, body);
        }
        else
        {
            _logger.LogInformation(
                "Sent email to {Email} with subject '{Subject}' via SendGrid (status {Status}).",
                email, subject, response.StatusCode);
        }
    }
}
