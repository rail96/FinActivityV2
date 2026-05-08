using Microsoft.AspNetCore.Identity;

namespace FindActivity.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public DateTime? BirthDate { get; set; }
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }

    /// <summary>
    /// Relative URL of the saved avatar in wwwroot/uploads/avatars/, e.g. "/uploads/avatars/{guid}.jpg".
    /// Null when the user hasn't uploaded one yet.
    /// </summary>
    public string? AvatarPath { get; set; }

    /// <summary>
    /// If set and in the future, the user is suspended (cannot sign in) until this UTC moment.
    /// Set by moderators on the admin dashboard. <c>null</c> means the account is in good standing.
    /// </summary>
    public DateTime? BannedUntilUtc { get; set; }

    /// <summary>Convenience computed property — true while the suspension is active.</summary>
    public bool IsSuspended =>
        BannedUntilUtc.HasValue && BannedUntilUtc.Value > DateTime.UtcNow;
}
