namespace Cenvo.Modules.Hr.Domain;

public static class HrErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string PositionRequired = "Nhập chức vụ.";
    public const string InvalidNationalId = "Số CCCD gồm đúng 12 chữ số.";
    public const string InvalidPhone = "Số điện thoại không hợp lệ — nhập 9 đến 11 chữ số.";
    public const string LeaveBeforeHire = "Ngày nghỉ việc không được trước ngày vào làm.";
    public const string AlreadyLeft = "Nhân viên đã nghỉ việc.";
    public const string NotLeft = "Nhân viên đang làm việc.";
    public const string ContractEndBeforeStart = "Ngày hết hạn hợp đồng phải sau ngày bắt đầu.";
    public const string RateNotFirstOfMonth = "Mức lương mới áp dụng từ ngày 1 của một tháng (kỳ lương theo tháng).";
    public const string InvalidSalary = "Lương và phụ cấp phải lớn hơn hoặc bằng 0.";
    public const string ReasonRequired = "Nhập lý do.";
    public const string RateDateTaken = "Đã có mức lương áp dụng từ tháng này — xóa mức đó (nếu chưa áp dụng) rồi nhập lại.";
    public const string RateAlreadyApplied = "Mức lương đã bắt đầu áp dụng — không xóa được. Nhập mức mới từ tháng sau.";
    public const string InvalidAmount = "Số tiền phải lớn hơn 0.";
    public const string AdjustmentCancelled = "Khoản này đã hủy.";
    public const string CancelOthersAdjustment = "Chỉ hủy được khoản do chính bạn ghi — nhờ người ghi hoặc chủ quán hủy.";
    public const string StaffNotInBranch = "Nhân viên này không làm ở chi nhánh hiện tại.";
    public const string ProfileRequired = "Nhân viên chưa có hồ sơ — điền hồ sơ (chức vụ, ngày vào làm) trước.";
    public const string CannotLeaveSelf = "Không tự cho mình nghỉ việc được — tài khoản của bạn sẽ bị khóa.";
    public const string CannotDropOwnAdmin = "Không bỏ quyền chủ quán của chính mình được — bạn sẽ mất quyền vào màn này.";
    public const string Concurrency = "Khoản này vừa được máy khác cập nhật. Tải lại rồi thử lại.";
    public const string AdjustmentDateInFuture = "Ngày ghi nhận không được sau hôm nay.";
}
