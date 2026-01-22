using System.ComponentModel.DataAnnotations;

namespace FindActivity.Web.Models.Reviews;

public class ReviewCreateViewModel
{
    [Required]
    public Guid ActivityId { get; set; }

    [Required]
    public string RevieweeUserId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
