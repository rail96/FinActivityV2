namespace FindActivity.Application.Dtos;

/// <summary>What happened when a user tried to join an activity.</summary>
public enum JoinOutcome
{
    /// <summary>User is now a confirmed participant.</summary>
    Joined = 0,

    /// <summary>Activity was full; user added to the waitlist.</summary>
    Waitlisted = 1,

    /// <summary>User was already participating (Joined or Waitlisted) — no-op.</summary>
    AlreadyParticipating = 2,

    /// <summary>Activity isn't joinable (cancelled, completed, host trying to join own, etc.).</summary>
    NotAllowed = 3
}
