using FindActivity.Domain.Entities;
using FindActivity.Web.Models.PhoneVerification;
using FindActivity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FindActivity.Web.Controllers;

/// <summary>
/// Two-step phone verification flow:
///   1. POST Start  -> sends an OTP code via Twilio Verify, redirects to Confirm.
///   2. POST Confirm -> checks the user-entered code; on success, marks PhoneNumberConfirmed = true.
/// </summary>
[Authorize]
public class PhoneVerificationController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPhoneVerificationService _phoneVerification;
    private readonly ILogger<PhoneVerificationController> _logger;

    public PhoneVerificationController(
        UserManager<ApplicationUser> userManager,
        IPhoneVerificationService phoneVerification,
        ILogger<PhoneVerificationController> logger)
    {
        _userManager = userManager;
        _phoneVerification = phoneVerification;
        _logger = logger;
    }

    public async Task<IActionResult> Start()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        return View(new PhoneStartViewModel
        {
            PhoneNumber = user.PhoneNumber ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(PhoneStartViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _phoneVerification.SendCodeAsync(model.PhoneNumber, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Couldn't send the verification code.");
            return View(model);
        }

        TempData["PhoneVerification:LastSentTo"] = model.PhoneNumber;
        return RedirectToAction(nameof(Confirm), new { phoneNumber = model.PhoneNumber });
    }

    public IActionResult Confirm(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return RedirectToAction(nameof(Start));
        }
        return View(new PhoneConfirmViewModel { PhoneNumber = phoneNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(PhoneConfirmViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Forbid();

        var result = await _phoneVerification.CheckCodeAsync(model.PhoneNumber, model.Code, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(model.Code), result.Error ?? "Code is incorrect or expired.");
            return View(model);
        }

        // Save the phone number and mark it confirmed in one shot via the UserManager. SetPhoneNumberAsync
        // resets the confirmation flag, so we explicitly set both.
        var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
        if (!setPhoneResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Couldn't save the phone number.");
            return View(model);
        }

        // Mark as confirmed via a token round-trip (cleanest way without bypassing Identity).
        var confirmToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
        var confirmResult = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, confirmToken);
        if (!confirmResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Couldn't confirm the phone number.");
            return View(model);
        }

        _logger.LogInformation("User {UserId} verified phone number.", user.Id);
        TempData["ProfileMessage"] = "Phone number verified.";
        return RedirectToAction("Details", "Profile", new { id = user.Id });
    }
}
