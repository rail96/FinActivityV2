using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FindActivity.Web.Models.Profile;

public class ProfileEditViewModel
{
    [StringLength(80)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [StringLength(500)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "About you")]
    public string? Bio { get; set; }

    /// <summary>Existing avatar URL — round-tripped through a hidden field so it isn't lost when no new file is picked.</summary>
    public string? AvatarPath { get; set; }

    /// <summary>New avatar upload. Optional: when null the existing AvatarPath is preserved.</summary>
    [Display(Name = "Profile picture")]
    public IFormFile? AvatarFile { get; set; }
}
