# Cenvo

Hệ thống quản lý **nhiều chi nhánh** cho mô hình kinh doanh kết hợp: **quán cà phê**, **rửa xe / chăm sóc ô tô** và **bán thiết bị gia dụng**.

Nguyên tắc cốt lõi của Cenvo là **dữ liệu liên thông** giữa các phân hệ:

| Nghiệp vụ | Hệ thống tự động |
|---|---|
| Bán 1 ly cà phê | Trừ nguyên liệu trong kho theo công thức (BOM) |
| Hủy hóa đơn đã thu | Hoàn kho bằng bút toán đảo |
| Rửa 1 xe *(GĐ2)* | Trừ hóa chất, ghi hoa hồng nhân viên, cộng giờ chạy thiết bị |
| Bán 1 thiết bị *(GĐ3)* | Trừ hàng, ghi công nợ, mở phiếu bảo hành |

> **Trạng thái:** Giai đoạn 1 (POS cà phê + Kho).
> - Backend: đã hoàn thiện và có test.
> - Frontend: đã có đăng nhập và chọn chi nhánh. POS, màn pha chế và kho đang làm.

---

## 1. Công nghệ

### Backend

| Thành phần | Công nghệ | Phiên bản |
|---|---|---|
| Runtime | .NET | 9.0 |
| Web | ASP.NET Core Minimal API | 9.0 |
| ORM | Entity Framework Core + Npgsql | 9.0.4 |
| Quy ước tên bảng/cột | EFCore.NamingConventions (snake_case) | 9.0.0 |
| Cơ sở dữ liệu | PostgreSQL | 16 |
| Xác thực | JWT Bearer + refresh token xoay vòng | 9.0.4 |
| Băm mật khẩu | ASP.NET Core Identity `PasswordHasher` | 9.0.4 |
| Realtime | SignalR | 9.0 |
| Ghi log | Serilog | 9.0.0 |
| Tài liệu API | Swashbuckle (OpenAPI / Swagger) | 7.3.1 |
| Kiểm thử | xUnit, Testcontainers.PostgreSql, WebApplicationFactory | 2.9.2 / 4.4.0 / 9.0.4 |

### Frontend

| Thành phần | Công nghệ | Phiên bản |
|---|---|---|
| UI | React | 19 |
| Build / dev server | Vite | 8 |
| Ngôn ngữ | TypeScript | 7 |
| Định tuyến | React Router | 7 |
| Server state | TanStack Query | 5 |
| Form & validate | react-hook-form + zod | 7 / 4 |
| CSS | Tailwind CSS (design token Cenvo) | 4 |
| Font | Barlow, Barlow Semi Condensed (tự host qua `@fontsource`) | 5 |
| Kiểu API | openapi-typescript (sinh từ Swagger) | 7 |
| Kiểm thử / lint | Vitest / oxlint | 5 / 1 |

---

## 2. Kiến trúc

### Modular monolith

Backend là **một ứng dụng triển khai duy nhất**, chia thành các module độc lập. Phụ thuộc chỉ đi **một chiều**:

```
SharedKernel ← Infrastructure ← Organization ← Catalog ← Inventory ← Cafe ← Api
```

- **Module sau được phụ thuộc module trước, không bao giờ ngược lại.**
- Khi cần đi ngược chiều thì dùng:
  - **Domain event:** module nguồn phát sự kiện, module khác cài handler.
  - **Interface trong `Contracts/`:** module cần dịch vụ khai báo interface, module khác (hoặc Api) cài đặt.

| Module | Trách nhiệm | Contract công bố |
|---|---|---|
| `SharedKernel` | `BaseEntity`, `IBranchScoped`, `ISoftDelete`, domain event, lỗi nghiệp vụ, `VnTime`, `Roles`, `SystemUser` | `IDocumentNumberGenerator`, `ICurrentUser` |
| `Infrastructure` | `AppDbContext` (global filter, audit, phát event), sinh mã chứng từ, `AddModule()` | |
| `Organization` | Chi nhánh, người dùng, vai trò, đăng nhập, refresh token | `IBranchStockPolicy` |
| `Catalog` | Hàng hóa, nhóm hàng, đơn vị, quy đổi đơn vị, giá theo chi nhánh | `IUnitConverterFactory`, `IProductUsageChecker` |
| `Inventory` | Kho, nhà cung cấp, phiếu nhập/xuất/chuyển/kiểm kê, sổ kho, tồn kho, giá vốn | `ISaleStockIssuer` |
| `Cafe` | Khu vực, bàn, menu, công thức, ca thu ngân, hóa đơn POS, pha chế, báo cáo doanh thu | `IKitchenNotifier` |
| `Api` | Host: endpoint, JWT, phân quyền, SignalR hub, xử lý lỗi, seed dữ liệu | |

### Cấu trúc bên trong một module

```
Cenvo.Modules.<Ten>/
  Domain/                 Entity (mỗi entity một file) + bất biến nghiệp vụ, Enums, Events, <Ten>Errors
    Calculations/         Hàm tính toán thuần (giá vốn, tiền thối, công thức) — có unit test
  Application/<Feature>/  <Feature>Service (ghi) + <Feature>QueryService (đọc) + DTO/Input
  Contracts/              Interface công bố cho module khác
  Persistence/            <Ten>Model : IModuleModel — cấu hình EF (index, FK, ràng buộc)
  Integration/            Handler xử lý domain event của module khác
```

### Luồng liên thông (ví dụ thanh toán hóa đơn)

```
CheckoutService.Pay
  └─ CafeOrder.Pay(...)                       tính tiền, chuyển trạng thái, Raise(CafeOrderPaid)
       └─ SaveChangesAsync (1 transaction)
            ├─ phát CafeOrderPaid → CafeOrderPaidHandler
            │     └─ RecipeExpander (món + topping → nguyên liệu, quy về đơn vị cơ sở)
            │     └─ ISaleStockIssuer.IssueForSale → phiếu xuất bán → StockService.Post
            └─ COMMIT  (hóa đơn và sổ kho cùng thành công hoặc cùng thất bại)
  └─ IKitchenNotifier → SignalR group "branch:{id}"
```

### Nguyên tắc Clean Code & SOLID áp dụng

- **Bất biến trong entity:** `CafeOrder.Pay/Cancel/MoveLinesTo`, `StockDocument.MarkPosted`, `StockBalance.Receive/Issue`. Service chỉ điều phối các bước: load → gọi method → lưu → thông báo.
- **Một service, một nhóm nghiệp vụ**, khoảng ≤ 200 dòng. Service đọc tách riêng khỏi service ghi.
- **Open/Closed:** mỗi loại phiếu kho có một `IStockPostingStrategy` riêng (Receipt, Issue, Transfer, Stocktake). Thêm loại phiếu mới thì thêm một lớp, không sửa `switch`.
- **Interface nhỏ, chỉ đặt ở ranh giới module.** Không dùng generic repository, không MediatR. `DbContext` đã đóng vai trò Unit of Work.
- **Đăng ký DI tự động:** `AddModule(assembly)` tự quét các lớp có hậu tố `Service | Issuer | Checker | Policy | Factory | Strategy`, cùng `IModuleModel` và `IDomainEventHandler<>`.

---

## 3. Đặc tả dữ liệu

### Đa chi nhánh

- **Lọc tự động:** entity thuộc chi nhánh cài `IBranchScoped`. `AppDbContext` áp **global query filter** theo chi nhánh của người dùng hiện tại, và tự gán `BranchId` khi thêm mới.
- **Chế độ toàn chuỗi:** Admin chưa chọn chi nhánh thì xem được dữ liệu cả chuỗi. Nhân viên chỉ thấy các chi nhánh được gán.
- **Quy tắc bắt buộc:** không dùng `IgnoreQueryFilters()` trong query có lọc chi nhánh, vì nó tắt filter của **cả câu query** (kể cả bảng join). Tên hiển thị của bản ghi đã xóa mềm phải tra bằng một query riêng theo danh sách id.

### Kiểu số và thời gian

| Loại dữ liệu | Kiểu PostgreSQL | Ghi chú |
|---|---|---|
| Tiền | `numeric(18,2)` | Làm tròn VND về 0 chữ số ở số cuối |
| Số lượng | `numeric(18,3)` | Tồn kho luôn tính theo **đơn vị cơ sở** |
| Đơn giá, giá vốn | `numeric(18,4)` | Giá vốn **bình quân di động** |
| Hệ số quy đổi | `numeric(18,6)` | vd. 1 thùng = 18 240 g |
| Thời gian | `timestamptz` (UTC) | Cắt ngày và hiển thị theo giờ Việt Nam qua `VnTime` |
| Khóa chính | `uuid` | Sinh phía ứng dụng |

### Chứng từ và toàn vẹn

- **Sổ kho chỉ ghi thêm:** `StockMovement` không bao giờ bị sửa hay xóa. Hủy chứng từ thì ghi **bút toán đảo**.
- **Không xóa cứng:**
  - Danh mục chỉ xóa mềm (`ISoftDelete`).
  - Chứng từ và hóa đơn chỉ được **hủy, kèm lý do**.
- **Khóa lạc quan:** chứng từ tiền và tồn kho có cột `xmin` (`[Timestamp] uint Version`). Hai máy sửa cùng lúc thì máy sau nhận lỗi **409**.
- **Mã chứng từ:** tiền tố + `yyMMdd-####`, đánh số theo từng chi nhánh, vd. `HD261127-0001`. Số được sinh bằng upsert nguyên tử trên bảng `document_sequence`, nên hai máy POS bấm cùng lúc vẫn nhận hai số khác nhau.
- **Audit:** `CreatedAt/By` và `UpdatedAt/By` được ghi tự động.

---

## 4. Bảo mật

- **Access token (JWT):** sống 60 phút. Claim gồm người dùng, vai trò, chi nhánh đang làm việc.
- **Refresh token:**
  - Xoay vòng mỗi lần dùng.
  - Lưu **dạng băm** trong DB.
  - Thu hồi được khi đăng xuất.
- **Phân quyền** bằng policy theo vai trò:

| Policy | Vai trò |
|---|---|
| `Admin` | Admin |
| `Managers` | Admin, BranchManager |
| `Cashiers` | Admin, BranchManager, Cashier |
| `Storekeepers` | Admin, BranchManager, Storekeeper |

- **Quyền dữ liệu** (đúng chi nhánh) do global filter đảm bảo, và có integration test kiểm chứng giữa 2 chi nhánh.
- **Tác vụ nhạy cảm cần quản lý:** hủy hóa đơn đã thu, hủy món đang pha.
- **Frontend:**
  - Access token chỉ giữ trong bộ nhớ.
  - Refresh token lưu `localStorage` (cân nhắc chuyển sang cookie `HttpOnly` trước khi triển khai thật).
- **Lỗi trả về:** dạng ProblemDetails `{ status, title }`, với `title` là câu tiếng Việt. Mã lỗi:

| Mã | Khi nào |
|---|---|
| 400 | Lỗi nghiệp vụ |
| 403 | Sai vai trò |
| 404 | Không tìm thấy hoặc thuộc chi nhánh khác |
| 409 | Xung đột ghi đồng thời |

---

## 5. Hiệu năng

**Mục tiêu:** thao tác POS phản hồi **< 300 ms**.

- **Query đọc:** `AsNoTracking()` + projection thẳng sang DTO. Không load cả aggregate để trả danh sách.
- **Không N+1:** gom danh sách id, chạy một query `Contains`, rồi dựng dictionary.
- **Index:** mọi cột lọc/sắp xếp thường xuyên đều có index bắt đầu bằng `BranchId`, vd. `(BranchId, Status)`, `(BranchId, PaidAt)`.
- **Danh sách luôn có giới hạn:** tối đa `Limits.MaxListSize = 500`.
- **Báo cáo:** tính bằng `GROUP BY` trong SQL.
- **Realtime:** server chỉ đẩy tín hiệu "đã thay đổi" theo group chi nhánh, client tự tải lại đúng phần cần.
- **Frontend:** mỗi route tải chậm (lazy), nên màn POS không phải tải code kho/báo cáo.

---

## 6. API

Tài liệu đầy đủ ở Swagger: **http://localhost:5000/swagger**

| Nhóm | Tiền tố | Nội dung |
|---|---|---|
| Xác thực | `/api/auth` | `login`, `refresh`, `logout`, `switch-branch` |
| Chi nhánh & người dùng | `/api/branches`, `/api/users` | Quản trị chi nhánh, tài khoản, vai trò |
| Danh mục | `/api/catalog` | Đơn vị, nhóm hàng, hàng hóa, quy đổi, giá theo chi nhánh |
| Kho | `/api/inventory` | Kho, nhà cung cấp, phiếu kho (tạo/sửa/duyệt/nhận/hủy), tồn kho, tồn dưới mức, thẻ kho, tiêu hao |
| Cà phê: thiết lập | `/api/cafe` | Khu vực, bàn, menu, topping, công thức |
| Cà phê: hóa đơn | `/api/cafe/orders` | Mở hóa đơn, thêm/sửa/xóa món, giảm giá, chuyển/gộp bàn, gửi pha chế, thanh toán, hủy |
| Cà phê: pha chế | `/api/cafe/kitchen` | Hàng chờ pha chế, đổi trạng thái món |
| Cà phê: ca | `/api/cafe/shifts` | Mở ca, đóng ca (đối soát tiền mặt), tổng kết ca |
| Báo cáo | `/api/reports` | Doanh thu cà phê theo khoảng ngày |
| Realtime | `/hubs/kitchen` | SignalR, group theo chi nhánh |
| Sức khỏe | `/health` | |

---

## 7. Chạy trên máy

### Yêu cầu

- .NET SDK 9
- Node.js 22 trở lên, npm 10 trở lên
- Docker Desktop (để chạy PostgreSQL và integration test)

### Các bước

```bash
# 1. PostgreSQL (lần đầu tạo container; các lần sau chỉ cần `docker start cenvo-dev-db`)
docker run -d --name cenvo-dev-db -e POSTGRES_USER=cenvo -e POSTGRES_PASSWORD=cenvo \
  -e POSTGRES_DB=cenvo -p 5433:5432 postgres:16-alpine

# 2. Backend — tự migrate và seed dữ liệu mẫu ở môi trường Development
cd backend
export ConnectionStrings__Default="Host=localhost;Port=5433;Database=cenvo;Username=cenvo;Password=cenvo"
#   PowerShell: $env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=cenvo;Username=cenvo;Password=cenvo"
dotnet run --project src/Cenvo.Api          # http://localhost:5000

# 3. Frontend (terminal khác)
cd frontend
npm install
npm run dev                                 # http://localhost:5173 — /api, /hubs được proxy sang :5000
```

### Tài khoản mẫu

| Tài khoản | Mật khẩu | Vai trò | Chi nhánh |
|---|---|---|---|
| `admin` | `Admin@123` | Admin | Toàn chuỗi |
| `quanly1` | `123456` | Quản lý | Cenvo Quận 1 |
| `thungan1` | `123456` | Thu ngân | Cenvo Quận 1 |
| `phache1` | `123456` | Pha chế | Cenvo Quận 1 |
| `thukho1` | `123456` | Thủ kho | Cenvo Quận 1 |
| `quanly2` | `123456` | Quản lý | Cenvo Thủ Đức |
| `thungan2` | `123456` | Thu ngân | Cenvo Thủ Đức |

Dữ liệu mẫu còn có:
- 11 nguyên liệu, 10 món và topping kèm công thức.
- 12 bàn cho mỗi chi nhánh.
- Tồn đầu kỳ ở cả 2 kho.

### Cấu hình

| Khóa (`appsettings.json` / biến môi trường) | Mặc định | Ý nghĩa |
|---|---|---|
| `ConnectionStrings:Default` | `Host=localhost;Port=5432;...` | Chuỗi kết nối PostgreSQL |
| `Jwt:Key` | *(giá trị giữ chỗ)* | **Bắt buộc đổi khi triển khai**, tối thiểu 32 ký tự |
| `Jwt:AccessMinutes` | `60` | Thời hạn access token (phút) |
| `Database:Migrate` | `true` | Tự chạy migration khi khởi động |
| `Database:Seed` | `false` (`true` ở Development) | Nạp dữ liệu mẫu khi DB trống |
| `Cors:Origins` | `http://localhost:5173` | Origin frontend được phép gọi |
| `CENVO_API_URL` *(frontend, khi dev)* | `http://localhost:5000` | Địa chỉ API mà Vite proxy tới |

Biến môi trường dùng `__` thay cho `:`, vd. `Jwt__Key`.

---

## 8. Kiểm thử & chất lượng

```bash
# Backend: unit + integration (Testcontainers tự dựng PostgreSQL, cần Docker đang chạy)
dotnet test backend/Cenvo.sln

# Frontend
cd frontend
npm run build      # kiểm tra kiểu TypeScript + build
npm run lint       # oxlint
npm test           # Vitest
npm run gen:api    # sinh lại kiểu API từ Swagger khi backend đổi
```

| Loại test | Phạm vi |
|---|---|
| Unit (backend) | `CostCalculator` (giá vốn bình quân), `UnitConverter` (quy đổi), `RecipeExpander` (công thức + topping), `PaymentCalculator` (tiền thối, chia nhiều phương thức) |
| Integration (backend) | Bán hàng trừ kho theo công thức rồi hủy thì hoàn kho; topping; ca thu ngân và đối soát; nhập mua; chuyển kho 2 chi nhánh; kiểm kê; chặn xuất âm; **cô lập dữ liệu giữa chi nhánh** (hóa đơn, tồn kho, hàng chờ pha chế); phân quyền theo vai trò |
| Unit (frontend) | Quy tắc chọn chi nhánh sau đăng nhập |

Integration test gọi HTTP thật qua `WebApplicationFactory` và đọc JSON bằng `JsonNode`, nên test không phụ thuộc kiểu C# nội bộ.

---

## 9. Cấu trúc thư mục

```
backend/
  Cenvo.sln
  src/
    Cenvo.SharedKernel/
    Cenvo.Infrastructure/          AppDbContext, migration (Persistence/Migrations), DI
    Cenvo.Api/                     Program.cs, Endpoints/, Auth/, Hubs/, Seed/
    Modules/
      Cenvo.Modules.Organization/
      Cenvo.Modules.Catalog/
      Cenvo.Modules.Inventory/     + Posting/ (StockService, IStockPostingStrategy)
      Cenvo.Modules.Cafe/
  tests/Cenvo.Tests/               Unit/, Integration/
frontend/
  src/
    routes/                        router (lazy), RequireAuth
    features/<phan-he>/            api/, components/, lib/, pages/
    components/ui, components/cenvo
    lib/api (apiFetch, kiểu sinh từ Swagger), lib/auth (phiên đăng nhập)
docs/
  PLAN.md                          Kế hoạch tổng thể + quy tắc Clean Code/SOLID
  PROGRESS.md                      Nhật ký tiến độ
```

**Tạo migration** khi đổi entity hoặc model:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <TenMigration> -p src/Cenvo.Infrastructure -s src/Cenvo.Api -o Persistence/Migrations
```

---

## 10. Lộ trình

| Giai đoạn | Nội dung | Trạng thái |
|---|---|---|
| GĐ1 | POS cà phê + Kho: backend | ✅ Xong |
| GĐ1 | Frontend: đăng nhập, chọn chi nhánh | ✅ Xong |
| GĐ1 | Frontend: POS, màn pha chế (SignalR), kho, báo cáo ngày | ⏳ Đang làm |
| GĐ2 | Rửa xe (tiếp nhận, hàng chờ theo biển số, gói thành viên) + Thiết bị ô tô (bảo trì, giờ chạy) | Kế hoạch |
| GĐ3 | Bán thiết bị gia dụng: hóa đơn lẻ/sỉ, bảng giá theo nhóm khách, công nợ, serial, bảo hành | Kế hoạch |
| GĐ4 | Nhân sự, lịch trực, chấm công (PWA, QR + GPS), bảng lương, hoa hồng | Kế hoạch |
| GĐ5 | Báo cáo & dashboard toàn chuỗi | Kế hoạch |

Chi tiết xem [docs/PLAN.md](docs/PLAN.md) và [docs/PROGRESS.md](docs/PROGRESS.md).

### Vấn đề đã biết

- EF Core cảnh báo về global filter trên quan hệ bắt buộc (`Unit` ↔ `ProductUnit`, `Branch` ↔ `UserBranch`). Hiện chưa ảnh hưởng hành vi.
- Refresh token ở frontend đang lưu trong `localStorage`. Nên chuyển sang cookie `HttpOnly` trước khi đưa vào vận hành thật.
