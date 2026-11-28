namespace Cenvo.Modules.CarWash.Domain;

public static class CarWashErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string InvalidPlate = "Biển số không hợp lệ — nhập 5 đến 12 chữ và số, ví dụ 51A-123.45.";
    public const string InvalidPhone = "Số điện thoại không hợp lệ — nhập 9 đến 11 chữ số.";
    public const string ServiceNameRequired = "Nhập tên dịch vụ.";
    public const string ServiceCodeTaken = "Mã dịch vụ đã được dùng — chọn mã khác.";
    public const string ServicePriceRequired = "Dịch vụ cần ít nhất một mức giá theo loại xe.";
    public const string InvalidPrice = "Giá phải lớn hơn hoặc bằng 0.";
    public const string InvalidCommission = "Hoa hồng không hợp lệ: phần trăm từ 0 đến 100, số tiền từ 0 trở lên.";
    public const string InvalidDuration = "Thời gian thực hiện phải từ 1 đến 600 phút.";
    public const string SupplyQuantityMustBePositive = "Định mức hóa chất phải lớn hơn 0.";
    public const string ServiceUnavailable = "Dịch vụ không còn được cung cấp.";
    public const string ServicesRequired = "Chọn ít nhất một dịch vụ.";
    public const string ServiceAlreadyOnTicket = "Dịch vụ này đã có trên phiếu.";
    public const string StaffRequired = "Gán ít nhất một nhân viên rửa xe trước khi bắt đầu.";
    public const string StaffNotInBranch = "Nhân viên được chọn không làm ở chi nhánh này hoặc không phải nhân viên rửa xe.";
    public const string TicketNotEditable = "Phiếu đã rửa xong, đã thu tiền hoặc đã hủy — không sửa dịch vụ, nhân viên được nữa.";
    public const string OnlyWaitingCanStart = "Chỉ bắt đầu rửa được xe đang chờ.";
    public const string OnlyWashingCanReturn = "Chỉ đưa lại hàng chờ được xe đang rửa.";
    public const string OnlyWashingCanComplete = "Chỉ hoàn tất được xe đang rửa.";
    public const string OnlyDoneCanDeliver = "Chỉ giao được xe đã rửa xong.";
    public const string DeliverNeedsPayment = "Phiếu chưa thu tiền — thanh toán trước khi giao xe.";
    public const string AlreadyPaid = "Phiếu đã thu tiền trước đó.";
    public const string TicketCancelled = "Phiếu đã hủy.";
    public const string CancelReasonRequired = "Nhập lý do hủy.";
    public const string CancelNeedsManager = "Phiếu đã rửa xong hoặc đã thu tiền — cần quyền quản lý để hủy.";
    public const string ShiftNotOpen = "Bạn cần mở ca thu ngân trước khi thu tiền.";
    public const string Concurrency = "Phiếu vừa được máy khác cập nhật. Tải lại rồi thử lại.";
    public const string CustomerPhoneRequired = "Nhập số điện thoại khách hàng.";
    public const string PhoneTaken = "Số điện thoại đã thuộc khách hàng khác.";
    public const string PackageNameRequired = "Nhập tên gói.";
    public const string PackageCodeTaken = "Mã gói đã được dùng — chọn mã khác.";
    public const string InvalidPackageUses = "Số lượt của gói phải từ 1 đến 100.";
    public const string InvalidPackageValidity = "Thời hạn gói phải từ 1 đến 1.095 ngày.";
    public const string PackageUnavailable = "Gói thành viên không còn bán.";
    public const string MembershipNeedsCustomer = "Phiếu chưa có khách hàng — nhập số điện thoại khách để dùng gói.";
    public const string MembershipNotUsable = "Gói không dùng được cho dịch vụ này: sai khách, sai dịch vụ/loại xe, hết lượt hoặc hết hạn.";
    public const string MembershipCancelled = "Gói thành viên đã hủy.";
    public const string MembershipUsedCannotCancel = "Gói đã dùng lượt — không hủy được. Muốn hoàn tiền phần còn lại, liên hệ quản lý chuỗi.";
    public const string MembershipCancelNeedsManager = "Cần quyền quản lý để hủy gói thành viên.";
    public const string MembershipOtherBranch = "Gói được bán ở chi nhánh khác — chỉ chi nhánh đã bán mới hủy và hoàn tiền được.";
    public const string NoPhoto = "Chưa chọn ảnh.";
    public const string PhotoTooLarge = "Ảnh quá lớn — tối đa 5 MB mỗi ảnh.";
    public const string PhotoNotImage = "Chỉ nhận ảnh JPEG, PNG hoặc WebP.";
    public const string TooManyPhotos = "Mỗi phiếu tối đa 8 ảnh — xóa bớt ảnh cũ trước.";
    public const string PhotoLocked = "Phiếu đã rửa xong hoặc đã hủy — không xóa ảnh hiện trạng được nữa.";

    public static string VehicleHasOpenTicket(string code) => $"Xe này đang có phiếu {code} chưa giao — mở phiếu đó thay vì tạo phiếu mới.";

    public static string NoPriceForVehicle(string service) => $"Dịch vụ “{service}” chưa có giá cho loại xe này — quản lý cần đặt giá trước.";
}
