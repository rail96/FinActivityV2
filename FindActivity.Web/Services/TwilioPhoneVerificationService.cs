using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Verify.V2.Service;

namespace FindActivity.Web.Services;

/// <summary>
/// Twilio Verify-based implementation of <see cref="IPhoneVerificationService"/>.
/// Twilio Verify handles the OTP generation, SMS sending, expiry, and rate limits — we just call its API.
/// </summary>
public class TwilioPhoneVerificationService : IPhoneVerificationService
{
    private readonly PhoneVerificationOptions _options;
    private readonly ILogger<TwilioPhoneVerificationService> _logger;

    public TwilioPhoneVerificationService(IOptions<PhoneVerificationOptions> options, ILogger<TwilioPhoneVerificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PhoneVerificationResult> SendCodeAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(out var error))
        {
            return new PhoneVerificationResult(false, error);
        }

        try
        {
            var client = BuildClient();
            await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: "sms",
                pathServiceSid: _options.VerifyServiceSid,
                client: client);
            _logger.LogInformation("Sent Twilio Verify code to {Phone}.", phoneNumber);
            return new PhoneVerificationResult(true, null);
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            _logger.LogWarning(ex, "Twilio rejected SendCode for {Phone}: {Message}", phoneNumber, ex.Message);
            return new PhoneVerificationResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending Twilio Verify code to {Phone}.", phoneNumber);
            return new PhoneVerificationResult(false, "Couldn't send the verification code. Please try again.");
        }
    }

    public async Task<PhoneVerificationResult> CheckCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(out var error))
        {
            return new PhoneVerificationResult(false, error);
        }

        try
        {
            var client = BuildClient();
            var check = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _options.VerifyServiceSid,
                client: client);

            // Twilio returns Status = "approved" on success; "pending" otherwise.
            if (string.Equals(check.Status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Twilio Verify approved code for {Phone}.", phoneNumber);
                return new PhoneVerificationResult(true, null);
            }

            return new PhoneVerificationResult(false, "Code is incorrect or expired.");
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            _logger.LogWarning(ex, "Twilio rejected CheckCode for {Phone}: {Message}", phoneNumber, ex.Message);
            return new PhoneVerificationResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking Twilio Verify code for {Phone}.", phoneNumber);
            return new PhoneVerificationResult(false, "Couldn't check the verification code. Please try again.");
        }
    }

    private bool IsConfigured(out string? error)
    {
        if (string.IsNullOrWhiteSpace(_options.AccountSid)
            || string.IsNullOrWhiteSpace(_options.AuthToken)
            || string.IsNullOrWhiteSpace(_options.VerifyServiceSid))
        {
            _logger.LogWarning("Twilio configuration is incomplete; phone verification is disabled.");
            error = "Phone verification isn't configured on the server.";
            return false;
        }
        error = null;
        return true;
    }

    private ITwilioRestClient BuildClient() =>
        new TwilioRestClient(_options.AccountSid, _options.AuthToken);
}
