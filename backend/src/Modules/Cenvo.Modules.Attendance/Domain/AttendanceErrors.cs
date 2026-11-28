namespace Cenvo.Modules.Attendance.Domain;

public static class AttendanceErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string InvalidCode = "Mã QR đã hết hạn hoặc không đúng — quét lại mã đang hiện trên máy ở quán.";
    public const string NotInBranch = "Bạn không làm ở chi nhánh này hoặc tài khoản đã khóa.";
    public const string JustPunched = "Bạn vừa chấm công xong — đợi 1 phút rồi thử lại nếu cần.";
    public static string OpenElsewhere(string branch) => $"Bạn đang chấm vào ở {branch} — chấm ra ở đó trước.";
    public const string AlreadyCheckedOut = "Lượt chấm công này đã có giờ ra.";
    public const string ReasonRequired = "Nhập lý do sửa để lưu lịch sử.";
    public const string CheckOutBeforeIn = "Giờ ra phải sau giờ vào.";
    public const string TooLong = "Một lượt chấm công dài tối đa 24 giờ.";
    public const string FutureTime = "Không nhập giờ trong tương lai.";
    public const string NotPending = "Lượt chấm công này không chờ duyệt.";
    public const string OvertimeNotAllowed = "Chỉ duyệt tăng ca cho lượt đã chấm ra và đã được tính công.";
    public static string OvertimeTooMuch(int max) => $"Tăng ca tối đa {max} phút (phần làm sau giờ kết thúc ca).";
    public const string OwnRecord = "Không tự duyệt hoặc sửa chấm công của chính mình — nhờ chủ quán.";
    public const string ShiftOtherEmployee = "Ca được chọn không phải của nhân viên này.";
    public const string ShiftAlreadyPunched = "Ca này đã có lượt chấm công — sửa lượt đó thay vì thêm mới.";
    public const string ChangedElsewhere = "Lượt chấm công vừa được thay đổi ở máy khác. Tải lại rồi thử lại.";
    public const string InvalidMonth = "Tháng không hợp lệ.";
    public const string HolidayNameRequired = "Nhập tên ngày lễ, ví dụ Quốc khánh.";
    public const string InvalidMultiplier = "Hệ số ngày lễ từ 1 đến 5.";
    public const string HolidayDateTaken = "Ngày này đã có trong danh sách ngày lễ.";
}
