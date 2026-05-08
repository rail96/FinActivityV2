namespace FindActivity.Web.Services;

/// <summary>
/// Twilio Verify configuration. Bound from the "Twilio" section of configuration.
/// Keep AccountSid + AuthToken in user-secrets in dev; never commit them.
/// </summary>
public class PhoneVerificationOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>SID of the Twilio Verify service (begins with "VA"). Created in Twilio Console &rarr; Verify &rarr; Services.</summary>
    public string VerifyServiceSid { get; set; } = string.Empty;
}
