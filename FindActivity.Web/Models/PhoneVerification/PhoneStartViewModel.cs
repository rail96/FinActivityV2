using System.ComponentModel.DataAnnotations;

namespace FindActivity.Web.Models.PhoneVerification;

public class PhoneStartViewModel
{
    [Required]
    [Phone]
    [Display(Name = "Phone number (E.164 format, e.g. +12065551234)")]
    [RegularExpression(@"^\+\d{8,15}$", ErrorMessage = "Use international format starting with + and country code, e.g. +12065551234.")]
    public string PhoneNumber { get; set; } = string.Empty;
}
