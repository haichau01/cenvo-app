namespace Cenvo.Modules.Inventory.Domain;

public static class InventoryErrors
{
    public const string NoBranch = "Chưa chọn chi nhánh làm việc.";
    public const string NoWarehouse = "Chi nhánh chưa có kho — tạo kho trước khi bán hàng hoặc nhập xuất.";
    public const string WarehouseNotInBranch = "Kho không thuộc chi nhánh hiện tại.";
    public const string NotManualType = "Loại chứng từ này không được tạo thủ công.";
    public const string TypeCannotChange = "Không đổi được loại chứng từ — hãy tạo phiếu mới.";
    public const string TransferTargetRequired = "Chọn kho đến khác kho đi.";
    public const string QuantityMustBePositive = "Số lượng phải lớn hơn 0.";
    public const string StocktakeDuplicateProduct = "Phiếu kiểm kê không được trùng sản phẩm.";
    public const string NotDraft = "Chỉ sửa hoặc duyệt được chứng từ nháp.";
    public const string DeleteOnlyDraft = "Chỉ xóa được chứng từ nháp — chứng từ đã duyệt thì dùng Hủy.";
    public const string NoLines = "Chứng từ chưa có dòng hàng.";
    public const string NotAwaitingReceipt = "Phiếu không ở trạng thái chờ nhận.";
    public const string OnlyDestinationReceives = "Chỉ chi nhánh có kho đến mới được xác nhận nhận hàng.";
    public const string OnlyPostedCanBeCancelled = "Chỉ hủy được chứng từ đã duyệt.";
    public const string SaleIssueCancelFromInvoice = "Phiếu xuất bán được hủy từ hóa đơn bán hàng.";
    public const string UnsupportedType = "Loại chứng từ không được hỗ trợ.";

    public static string NotTracked(string product) => $"'{product}' không quản lý tồn kho nên không nhập/xuất được.";

    public static string NotEnoughStock(decimal onHand, decimal issue) =>
        $"Không đủ tồn kho để xuất (tồn {onHand:0.###}, cần xuất {issue:0.###}). Kiểm tra lại số lượng hoặc nhập thêm hàng.";
}
