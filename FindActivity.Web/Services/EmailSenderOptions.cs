namespace FindActivity.Web.Services;

/// <summary>
/// Configuration for the SendGrid-backed <see cref="Microsoft.AspNetCore.Identity.UI.Services.IEmailSender"/>.
/// Bound from the "SendGrid" section of configuration (appsettings / user-secrets / environment).
/// </summary>
public class EmailSenderOptions
{
    /// <summary>SendGrid API key. Keep out of source control; use user-secrets in dev.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender email address (must be verified in the SendGrid dashboard).</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Friendly display name shown as the sender.</summary>
    public string FromName { get; set; } = "FindActivity";
}
