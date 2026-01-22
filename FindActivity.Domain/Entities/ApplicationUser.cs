using Microsoft.AspNetCore.Identity;

namespace FindActivity.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public DateTime? BirthDate { get; set; }
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
}
