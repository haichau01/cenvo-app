namespace Cenvo.Modules.Schedule.Domain;

public static class ScheduleErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string TemplateNameRequired = "Nhập tên ca, ví dụ Sáng.";
    public const string TemplateZeroLength = "Giờ bắt đầu và giờ kết thúc ca không được trùng nhau.";
    public const string TemplateTooLong = "Một ca dài tối đa 16 giờ — chia thành hai ca.";
    public const string InvalidMinStaff = "Số người tối thiểu mỗi bộ phận từ 0 đến 50.";
    public const string TemplateInactive = "Ca này đã ngừng dùng — bật lại trong \"Ca làm\" hoặc chọn ca khác.";
    public const string StaffNotInBranch = "Nhân viên không làm ở chi nhánh này hoặc tài khoản đã khóa.";
    public const string AlreadyAssigned = "Nhân viên đã có trong ca này.";
    public const string PastDayLocked = "Không sửa lịch của ngày đã qua — lịch cũ dùng để đối chiếu chấm công.";
    public const string InvalidMaxWeeklyHours = "Giờ làm tối đa mỗi tuần từ 1 đến 168.";
    public const string InvalidRange = "Khoảng ngày không hợp lệ — tối đa 31 ngày.";

    public static string Overlap(string employee, string shift, DateOnly date, string branch) =>
        $"{employee} đã có ca {shift} ngày {date:dd/MM/yyyy} tại {branch} trùng giờ — bỏ ca đó trước hoặc chọn người khác.";
}
