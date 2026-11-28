namespace Cenvo.Modules.Organization.Domain;

public static class OrganizationErrors
{
    public const string WrongCredentials = "Sai tên đăng nhập hoặc mật khẩu.";
    public const string SessionExpired = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
    public const string UserLocked = "Tài khoản đã bị khóa, liên hệ quản lý.";
    public const string NoBranchAccess = "Bạn không có quyền làm việc ở chi nhánh này.";
    public const string InvalidRole = "Vai trò không hợp lệ.";
    public const string PasswordRequired = "Cần nhập mật khẩu cho tài khoản mới.";
    public const string InvalidUserName = "Tên đăng nhập gồm 3–50 ký tự: chữ không dấu, số, dấu chấm, gạch ngang hoặc gạch dưới.";
    public const string UserNameTaken = "Tên đăng nhập đã có người dùng — chọn tên khác.";
    public const string FullNameRequired = "Nhập họ tên.";
    public const string PasswordTooShort = "Mật khẩu cần ít nhất 6 ký tự.";
    public const string UnknownBranch = "Chi nhánh được chọn không tồn tại hoặc đã ngừng hoạt động.";
    public const string NotBranchManager = "Tài khoản này không phải quản lý của chi nhánh — nhờ quản lý chi nhánh nhập tài khoản của họ.";
}
