namespace Cenvo.Modules.Attendance.Application.Punching;

public record PunchByCodeInput(string Code);
public record PunchByPinInput(Guid EmployeeId, string Pin);
public record KioskCodeDto(string Code, DateTime ExpiresAt);

public enum PunchAction { CheckIn, CheckOut }

/// <summary>What the phone / POS tells the person right after the punch.</summary>
public record PunchResult(PunchAction Action, string EmployeeName, DateTime At, string? ShiftName, bool NeedsApproval,
    int LateMinutes, int WorkedMinutes)
{
    public static PunchResult Of(PunchAction action, AttendanceRecord r) => new(action, r.EmployeeName,
        action == PunchAction.CheckIn ? r.CheckInAt : r.CheckOutAt!.Value, r.ShiftName, r.Approval == ApprovalStatus.Pending,
        r.LateMinutes, r.WorkedMinutes);
}
