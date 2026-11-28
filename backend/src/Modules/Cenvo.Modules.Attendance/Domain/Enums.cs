namespace Cenvo.Modules.Attendance.Domain;

public enum PunchMethod
{
    /// <summary>The person's phone scanned the rotating code shown on the shop POS.</summary>
    Qr,
    /// <summary>Typed on the shop POS (fallback) — flagged for the manager to look at.</summary>
    Pin,
    /// <summary>Entered or corrected by a manager, with a reason.</summary>
    Manual,
}

public enum ApprovalStatus
{
    /// <summary>Punch on a planned shift: counts on its own.</summary>
    NotNeeded,
    /// <summary>Punch without a planned shift: waits for the branch manager.</summary>
    Pending,
    Approved,
    Rejected,
}
