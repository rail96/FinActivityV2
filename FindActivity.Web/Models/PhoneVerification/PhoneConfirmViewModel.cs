using System.ComponentModel.DataAnnotations;

namespace FindActivity.Web.Models.PhoneVerification;

public class PhoneConfirmViewModel
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
}
