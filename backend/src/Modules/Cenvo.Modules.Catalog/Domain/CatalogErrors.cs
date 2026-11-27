namespace Cenvo.Modules.Catalog.Domain;

public static class CatalogErrors
{
    public static string DuplicateCode(string code) => $"Mã sản phẩm '{code}' đã tồn tại, hãy chọn mã khác.";
    public static string MissingConversion(string product) =>
        $"Sản phẩm '{product}' chưa khai báo quy đổi cho đơn vị này — thêm ở mục Đơn vị quy đổi của sản phẩm.";
    public const string BaseUnitLocked = "Sản phẩm đã phát sinh kho nên không đổi được đơn vị cơ sở.";
}
