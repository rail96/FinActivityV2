namespace FindActivity.Web.Services;

public interface IPhoneVerificationService
{
    /// <summary>Sends a verification code to <paramref name="phoneNumber"/> (E.164 format) via Twilio Verify.</summary>
    Task<PhoneVerificationResult> SendCodeAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Checks the user-supplied <paramref name="code"/> against the latest code Twilio sent to <paramref name="phoneNumber"/>.</summary>
    Task<PhoneVerificationResult> CheckCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}

public record PhoneVerificationResult(bool Success, string? Error);
