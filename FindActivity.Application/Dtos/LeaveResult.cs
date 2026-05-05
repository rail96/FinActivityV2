namespace FindActivity.Application.Dtos;

/// <summary>
/// Outcome of a Leave operation. When a Joined participant leaves, the oldest waitlisted
/// participant (if any) is auto-promoted; <see cref="PromotedUserId"/> identifies them so
/// the caller can send a notification.
/// </summary>
public record LeaveResult(bool Success, string? PromotedUserId);
