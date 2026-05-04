using System.ComponentModel.DataAnnotations;
using FindActivity.Domain.Enums;

namespace FindActivity.Web.Models.Reports;

public class ReportCreateViewModel
{
    [Required]
    public ReportTargetType TargetType { get; set; }

    public Guid? TargetActivityId { get; set; }

    public string? TargetUserId { get; set; }

    [Required]
    [Display(Name = "Reason")]
    public ReportReason Reason { get; set; }

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Additional details")]
    public string? Details { get; set; }

    /// <summary>
    /// Where to send the user back after submission. Resolved to a local URL by the controller.
    /// </summary>
    public string? ReturnUrl { get; set; }
}
