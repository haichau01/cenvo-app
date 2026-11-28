namespace Cenvo.Modules.Cafe.Domain;

public static class CafeErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string TableRequired = "Chọn bàn cho khách ngồi tại quán.";
    public const string OrderClosed = "Hóa đơn đã đóng, không sửa được nữa.";
    public const string OrderAlreadyCancelled = "Hóa đơn đã hủy trước đó.";
    public const string QuantityMustBePositive = "Số lượng phải lớn hơn 0 — muốn bỏ món thì dùng Xóa món.";
    public const string InvalidDiscount = "Giảm giá không hợp lệ.";
    public const string InvalidDiscountPercent = "Phần trăm giảm giá phải từ 0 đến 100.";
    public const string DiscountReasonRequired = "Nhập lý do giảm giá — lý do hiện trong báo cáo ca.";
    public const string ItemNotOnMenu = "Món không có trên menu hoặc đã ngừng bán.";
    public const string InvalidTopping = "Topping không hợp lệ.";
    public const string PreparingNeedsManager = "Món đang pha chế — cần quản lý để hủy.";
    public const string EmptyOrder = "Hóa đơn chưa có món.";
    public const string ShiftNotOpen = "Bạn cần mở ca trước khi thanh toán.";
    public const string ShiftAlreadyOpen = "Bạn đang có ca chưa đóng — đóng ca cũ trước.";
    public const string NoOpenShift = "Bạn chưa mở ca.";
    public const string InvalidAmount = "Số tiền không hợp lệ.";
    public const string NonCashExceedsTotal = "Tiền chuyển khoản/thẻ vượt quá tổng hóa đơn.";
    public const string CancelReasonRequired = "Nhập lý do hủy.";
    public const string PaidCancelNeedsManager = "Cần quyền quản lý để hủy hóa đơn đã thanh toán.";
    public const string MoveToSameOrder = "Không thể chuyển sang chính hóa đơn này.";
    public const string AreaHasTables = "Khu vực còn bàn, hãy xóa bàn trước.";
    public const string TableOccupied = "Bàn đang có khách.";
    public const string RecipeQuantityMustBePositive = "Định lượng phải lớn hơn 0.";
    public const string RecipeSelfReference = "Món không thể là nguyên liệu của chính nó.";

    public static string CustomerOwes(decimal amount) => $"Khách còn thiếu {amount:N0}đ.";

    public static string DiscountNeedsManager(decimal cashierLimit) =>
        $"Tổng giảm giá vượt {DiscountPolicy.CashierMaxPercent:0}% hóa đơn ({cashierLimit:N0}đ) — cần quản lý nhập tài khoản để duyệt.";

    public static string KitchenMoveNotAllowed(KitchenStatus from, KitchenStatus to) =>
        $"Không thể chuyển món từ “{KitchenLabels[from]}” sang “{KitchenLabels[to]}”. Chỉ được lùi một bước để sửa thao tác nhầm.";

    private static readonly Dictionary<KitchenStatus, string> KitchenLabels = new()
    {
        [KitchenStatus.New] = "Chưa gửi",
        [KitchenStatus.Sent] = "Đã gửi",
        [KitchenStatus.Preparing] = "Đang pha",
        [KitchenStatus.Done] = "Đã xong",
        [KitchenStatus.Served] = "Đã mang ra",
    };
}
