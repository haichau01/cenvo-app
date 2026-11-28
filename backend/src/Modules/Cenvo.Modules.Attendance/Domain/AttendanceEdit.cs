namespace Cenvo.Modules.Attendance.Domain;

/// <summary>A manager's change to a punch: the times before and after, who, when and why. Append-only.</summary>
public class AttendanceEdit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttendanceRecordId { get; set; }
    public DateTime At { get; set; }
    public Guid EditorId { get; set; }
    public string EditorName { get; set; } = "";
    /// <summary>Null when the manager entered the whole punch.</summary>
    public DateTime? OldCheckIn { get; set; }
    public DateTime? OldCheckOut { get; set; }
    public DateTime NewCheckIn { get; set; }
    public DateTime? NewCheckOut { get; set; }
    public string Reason { get; set; } = "";

    /// <param name="newCheckIn">Null = the record is new, so its own times are the new ones.</param>
    internal static AttendanceEdit Create(AttendanceRecord record, DateTime? newCheckIn, DateTime? newCheckOut, Editor editor,
        string reason, DateTime now)
    {
        var isNew = newCheckIn == null;
        return new AttendanceEdit
        {
            AttendanceRecordId = record.Id, At = now, EditorId = editor.UserId, EditorName = editor.Name, Reason = reason,
            OldCheckIn = isNew ? null : record.CheckInAt,
            OldCheckOut = isNew ? null : record.CheckOutAt,
            NewCheckIn = newCheckIn ?? record.CheckInAt,
            NewCheckOut = isNew ? record.CheckOutAt : newCheckOut,
        };
    }
}
