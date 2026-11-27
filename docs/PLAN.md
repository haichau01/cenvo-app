> **Ghi chú triển khai (2026-11-27):** refactor GĐ1 đã xong. Có 3 điểm làm khác plan dưới đây:
> - Không thêm `IClock`, vì chưa có test nào cần giả lập thời gian (nguyên tắc "đơn giản trước").
> - Không dùng FluentValidation. Các kiểm tra như "số lượng > 0" đặt trong method của entity.
> - Frontend chốt lại (2026-11-27): **React SPA** (Vite + React Router), không dùng Next.js.
>
> Vì vậy các mục nói về `IClock`, `SystemClock`, `Validator` và "không còn `DateTime.UtcNow`" bên dưới **không áp dụng**.

# Kế hoạch xây dựng hệ thống Cenvo: Cà phê + Rửa xe + Bán thiết bị gia dụng

## Context
Cần một hệ thống web quản lý tập trung, **nhiều chi nhánh**, cho 3 mảng: quán cà phê, rửa xe/chăm sóc ô tô, và bán thiết bị gia dụng (bán lẻ + bán sỉ). Dữ liệu giữa các mảng phải liên thông:
- Bán 1 ly cà phê → kho trừ nguyên liệu theo công thức.
- Rửa 1 xe → kho trừ hóa chất, nhân viên được hoa hồng, thiết bị cộng giờ chạy.
- Bán 1 thiết bị gia dụng → kho trừ hàng, ghi công nợ, mở phiếu bảo hành.

**Stack:** .NET 9 + EF Core + PostgreSQL (backend), React 19 + Vite + TypeScript + React Router + TanStack Query + Tailwind v4 + shadcn/ui (frontend, SPA gọi API .NET). **MVP giai đoạn 1:** POS cà phê + Kho.

**Bản sửa này** bổ sung các quy tắc **Clean Code + SOLID**, áp dụng ở mức vừa đủ, để code dễ đọc. Kèm theo là danh sách refactor cụ thể cho phần backend GĐ1 đã viết, vì phần này hiện có vài service rất dài (`OrderService` khoảng 400 dòng, `InventoryService` khoảng 330 dòng), một file chứa nhiều entity, `switch` theo loại chứng từ, logic sinh mã bị lặp 3 lần, và có chỗ phụ thuộc ngược giữa module.

## Phân hệ
| # | Phân hệ | Mục đích chính |
|---|---------|----------------|
| 1 | Quán cà phê (POS) | Bán hàng, menu, bàn, hóa đơn |
| 2 | Rửa xe | Tiếp nhận xe, hàng chờ, dịch vụ, gói thành viên |
| 3 | Kho hàng | Nguyên liệu, hóa chất, hàng gia dụng; nhập – xuất – tồn – chuyển kho |
| 4 | Bán thiết bị gia dụng | Hóa đơn bán lẻ/sỉ, bảng giá theo nhóm khách, công nợ, bảo hành, serial |
| 5 | Nhân sự | Hồ sơ, chức vụ, hợp đồng, mức lương |
| 6 | Lịch trực | Ca làm, phân ca, đổi ca |
| 7 | Chấm công & lương | Check-in/out, tăng ca, bảng công, bảng lương, hoa hồng |
| 8 | Thiết bị ô tô | Tài sản, bảo trì, sửa chữa, giờ chạy |
| 9 | Báo cáo & Dashboard | Doanh thu, lợi nhuận, tồn kho, hiệu suất theo chi nhánh |

## Kiến trúc (giữ nguyên)
- **Modular monolith:** `SharedKernel ← Infrastructure ← Organization ← Catalog ← Inventory ← Cafe ← Api`. Phụ thuộc chỉ đi một chiều; khi cần đi ngược chiều thì dùng domain event hoặc interface.
- **Lọc chi nhánh và xóa mềm:** global filter trong `AppDbContext`. Audit được ghi tự động; domain event được phát trong `SaveChangesAsync`, nên vẫn atomic.
- **Ghi sổ kho:** chỉ đi qua `StockService`; sổ kho chỉ ghi thêm, không sửa; giá vốn bình quân di động.

---

## Clean Code & SOLID: quy tắc áp dụng cho Cenvo

### Mức độ "vừa đủ" (những gì KHÔNG làm)
- Không thêm generic repository hay UnitOfWork bọc EF, vì `DbContext` đã đóng vai trò đó.
- Không dùng MediatR, không tạo interface cho mọi service.
- Interface chỉ tạo ở **ranh giới giữa các module** và cho **tác động ra bên ngoài** (SignalR, đồng hồ hệ thống, máy in).

### S: Single Responsibility (mỗi lớp một lý do để thay đổi)
- Mỗi service phục vụ **một nhóm nghiệp vụ**, khoảng ≤ 200 dòng. Service **đọc** (Query) tách khỏi service **ghi** (Command).
- **Bất biến nghiệp vụ nằm trong entity.** Service chỉ điều phối: load → gọi method của entity → `SaveChangesAsync` → notify.
- Phép tính thuần (tiền thừa, giá vốn, quy đổi, BOM) được viết thành lớp static thuần và có unit test.

### O: Open/Closed (thêm tính năng bằng cách thêm lớp, không sửa `switch`)
- Cách duyệt từng loại chứng từ kho → `IStockPostingStrategy`, mỗi loại chứng từ một lớp.
- Nguồn xuất bán (Cafe, CarWash, Retail) đều gọi chung một contract `ISaleStockIssuer`, nên thêm phân hệ mới không phải sửa Inventory.

### L: Liskov (lớp thay thế phải hành xử đúng hợp đồng)
- `CurrentUser.Override` hiện trả `IsInRole = true` với mọi role, nên seed/job nền ngầm có toàn quyền. Thay bằng lớp `SystemUser : ICurrentUser` có role tường minh (`Admin`), và dùng qua `ICurrentUserAccessor.RunAs(...)`.

### I: Interface Segregation (interface nhỏ, đúng nhu cầu bên gọi)
- Cafe chỉ cần `ISaleStockIssuer` (2 hàm `IssueForSale`, `CancelByRef`), không cần cả `InventoryService`.
- Inventory chỉ cần `IBranchStockPolicy.AllowNegativeStock(branchId)`, không cần cả `BranchService`.
- Mọi module chỉ cần `IUnitConverterFactory.Create(productIds)`, không cần cả `CatalogService`.

### D: Dependency Inversion (module cấp cao không phụ thuộc chi tiết)
- `CatalogService` hiện chạy raw SQL vào bảng `stock_movements`, tức Catalog đang biết bảng của Inventory (phụ thuộc ngược). Sửa: Catalog khai báo `IProductUsageChecker`, Inventory cài đặt.
- `DateTime.UtcNow` rải rác → đổi sang `IClock` (Infrastructure cài `SystemClock`, test dùng `FakeClock`).
- 3 bản `NextCode` (kho, hóa đơn, ca) → gộp thành một `IDocumentNumberGenerator` duy nhất (tiền tố + `yyMMdd-####` theo chi nhánh). Thay đổi này cũng xử lý luôn chỗ lặp code (DRY).

### Clean Code: quy tắc viết
| Quy tắc | Ví dụ trong Cenvo |
|---|---|
| Một kiểu public / một file, đặt tên file theo kiểu | `CafeOrder.cs`, `CafeOrderLine.cs`; record Input/Dto nhỏ được gom theo feature: `OrderDtos.cs` |
| Method ≤ ~30 dòng, guard clause đặt ở đầu, tối đa 2 cấp lồng | Tách `Pay` thành `EnsureShiftOpen` → `PaymentCalculator.Calculate` → `order.MarkPaid` |
| Không dùng `goto`, không dùng ternary lồng nhau | Viết lại `MoveLines` (hiện có `goto move`) thành `ResolveTargetOrder()` |
| Không gán tuple nhiều trường | Thay `(a.Name, a.SortOrder) = (...)` bằng method entity `area.Rename(name, sortOrder)` |
| Không có magic string/number | `StockRefTypes.CafeOrder`, `Limits.MaxListSize = 500`, `KitchenQueue.LookbackHours = 12` |
| Tên nói lên ý định | `GetOpenOrdersAsync`, `CreateDraftAsync`; bool đặt tên `Is/Has/Can/Allow` |
| Validate input tách khỏi nghiệp vụ | FluentValidation: `AddLineInputValidator`; service không còn chuỗi `if` kiểm tra input |
| Thông báo lỗi tập trung theo module | `CafeErrors.ShiftNotOpen`, `InventoryErrors.NotEnoughStock(onHand, qty)` (tiếng Việt) |
| Mapping tách riêng | `OrderMappings.ToDto()`; projection `Expression` dùng lại được trong query |
| Comment giải thích *tại sao*, không giải thích *làm gì* | Comment và XML doc viết **tiếng Anh** (thông báo lỗi/UI vẫn tiếng Việt); xóa comment chỉ lặp lại code |

### Cấu trúc thư mục mới của một module (vertical slice)
```
Cenvo.Modules.Cafe/
├─ Domain/                 # entity + bất biến nghiệp vụ, không phụ thuộc EF
│  ├─ CafeOrder.cs         # AddLine, UpdateLine, RemoveLine, ApplyDiscount, MarkPaid, Cancel, MoveLinesTo
│  ├─ CafeOrderLine.cs  CafeOrderLineTopping.cs  CafePayment.cs
│  ├─ Shift.cs             # Open, Close(countedCash)
│  ├─ Area.cs  DiningTable.cs  CafeItem.cs  RecipeLine.cs
│  ├─ Enums.cs  Events.cs  CafeErrors.cs
│  └─ Calculations/        # PaymentCalculator.cs, RecipeExpander.cs (thuần, có unit test)
├─ Application/
│  ├─ Orders/              # OrderQueryService, OrderEditingService, CheckoutService,
│  │                       # TableTransferService, OrderDtos.cs, OrderInputs.cs, Validators.cs, OrderMappings.cs
│  ├─ Kitchen/             # KitchenService (hàng chờ + đổi trạng thái)
│  ├─ Shifts/              # ShiftService, ShiftSummaryCalculator
│  ├─ Setup/               # AreaTableService, MenuService, RecipeService
│  └─ Reports/             # SalesReportService
├─ Integration/            # CafeOrderPaidHandler, CafeOrderCancelledHandler (dùng ISaleStockIssuer)
├─ Persistence/            # CafeModel.cs → tách thành Configurations/<Entity>Configuration.cs
└─ Contracts/              # IKitchenNotifier
```
Inventory cũng chia theo cách tương tự:
- `Domain/`: `StockDocument.Post/Cancel/Receive`.
- `Application/Documents`, `Application/Warehouses`, `Application/Suppliers`, `Application/Queries`.
- `Posting/`: `StockService` cùng các `IStockPostingStrategy`: `ReceiptPosting`, `IssuePosting`, `TransferPosting`, `StocktakePosting`.
- `Contracts/`: `ISaleStockIssuer`, đi kèm cài đặt `SaleStockIssuer`.

---

## Kế hoạch refactor backend GĐ1 (hành vi giữ nguyên)

**Bước 0: Lưới an toàn trước khi refactor.**
- Tạo migration `Initial`.
- Viết integration test bằng Testcontainers cho các luồng:
  - Nhập 1 kg cà phê → bán 2 ly × 20 g → tồn còn 960 g → hủy hóa đơn → tồn về 1000 g.
  - Chuyển kho giữa 2 chi nhánh.
  - Kiểm kê sinh chênh lệch.
  - Cô lập dữ liệu giữa chi nhánh A và B.
- Viết unit test cho `CostCalculator`, `RecipeExpander`, `UnitConverter`.
- Từ bước này trở đi, mọi bước refactor đều phải giữ các test này xanh.

**Bước 1: SharedKernel / Infrastructure.**
- Thêm `IClock`, `IDocumentNumberGenerator` (cài đặt trong Infrastructure), `Limits`, `StockRefTypes`.
- Thêm `SystemUser` và `ICurrentUserAccessor.RunAs`; bỏ `CurrentUser.Override`.
- Mở rộng `AddModule`:
  - Đăng ký thêm các lớp tên kết thúc bằng `Service`, `Strategy`, `Issuer`, `Checker`, `Validator`.
  - Đăng ký luôn các interface `Cenvo.*` mà lớp đó cài.

**Bước 2: Catalog.**
- Tách file theo `Domain/` và `Application/`.
- Tách `CatalogService` thành `UnitService`, `CategoryService`, `ProductService`, `ProductQueryService`.
- Tạo `UnitConverterFactory : IUnitConverterFactory`.
- Khai báo `IProductUsageChecker` để thay raw SQL.

**Bước 3: Inventory.**
- Tách `InventoryService` thành `WarehouseService`, `SupplierService`, `StockDocumentService` (tạo/sửa/duyệt/hủy/nhận), `StockDocumentQueryService`, `SaleStockIssuer`.
- Đưa các chuyển trạng thái vào entity `StockDocument`.
- Thay `switch (doc.Type)` bằng `IStockPostingStrategy`.
- Viết lại `Project(...)` (hiện lồng subquery) thành projection cộng join tra tên một lần.
- Cài đặt `IProductUsageChecker` và `IBranchStockPolicy`. Organization khai báo interface `IBranchStockPolicy`, `BranchService` cài.

**Bước 4: Cafe.**
- Chuyển logic từ `OrderService` vào `CafeOrder`: thêm/gộp dòng, giảm giá, thanh toán, hủy, chuyển dòng.
- Tách `PaymentCalculator` (tiền thừa, tiền mặt cần thu) làm hàm thuần.
- Tách `OrderService` thành 5 service như ở cấu trúc thư mục trên; viết lại `MoveLines` để bỏ `goto`.
- `CafeSetupService` → 3 service (`AreaTableService`, `MenuService`, `RecipeService`); `ShiftService` dùng `IDocumentNumberGenerator`.
- Handler tích hợp chỉ phụ thuộc vào `ISaleStockIssuer` và `IUnitConverterFactory`.

**Bước 5: Api.** Thực hiện theo quy ước của skill `cenvo-backend`:
- Chuyển controller sang **Minimal API**: mỗi module một file `Endpoints/<Module>Endpoints.cs`, dùng `MapGroup` + `RequireAuthorization`.
- Thêm filter FluentValidation.
- Map lỗi: `DbUpdateConcurrencyException` → 409.
- `DataSeeder` tách thành các hàm `SeedBranches`, `SeedCatalog`, `SeedMenu`, `SeedOpeningStock`.

**Bước 6: Cập nhật tài liệu.**
- Sửa `CLAUDE.md` và skill `.claude/skills/cenvo-backend/SKILL.md` theo cấu trúc mới (thay mẫu `Entities.cs` một file bằng `Domain/`, `Application/<Feature>/`, `Contracts/`).
- Thêm mục "Clean Code & SOLID" vào checklist review.
- Lưu plan này vào `docs/PLAN.md`.

## Các giai đoạn tiếp theo (áp dụng cùng quy tắc ngay từ đầu)
- **GĐ1 (còn lại): frontend ReactJS** (SPA Vite, chạy `http://localhost:5173`, gọi API .NET `:5000`), làm theo `cenvo-frontend` và `cenvo-design-system`.
  - Màn hình: đăng nhập + chọn chi nhánh, POS (bàn, menu, hóa đơn, thanh toán, mở/đóng ca), màn pha chế (SignalR), kho (tồn, phiếu nhập/xuất/chuyển/kiểm kê, thẻ kho), báo cáo ngày.
  - Cấu trúc:
    - `src/routes/`: React Router, mỗi phân hệ một nhóm route, tải chậm từng route bằng `lazy()`.
    - `src/features/<phan-he>/`: `api.ts`, `components/`, `lib/`.
    - `src/components/{ui,cenvo}/`.
    - `src/lib/{api,format,realtime}.ts`.
  - Quy tắc Clean Code áp dụng như backend:
    - Component ≤ ~150 dòng.
    - Logic tách sang hook hoặc hàm thuần.
    - Kiểu sinh từ Swagger (`openapi-typescript`).
    - Không tự tính tiền phía client để lưu.
  - Kiểm chứng: `npm run build`, `npm run lint`; chạy thử luồng mở ca → order → thanh toán → thấy món trên màn pha chế → tồn kho giảm.
- **GĐ2: Rửa xe + Thiết bị.**
  - `WashTicketCompleted` → `ISaleStockIssuer` trừ hóa chất.
  - Hoa hồng nhân viên do Payroll lắng nghe event.
  - Giờ chạy thiết bị do Equipment lắng nghe event.
- **GĐ3: Bán gia dụng.**
  - Bán lẻ/sỉ, bảng giá theo nhóm khách, công nợ phải thu/phải trả, serial, bảo hành, đổi trả.
  - Xuất kho dùng `RetailInvoiceIssued` → `ISaleStockIssuer`.
- **GĐ4: Nhân sự, Lịch trực, Chấm công, Lương.** Phần tính công và tính lương viết thành hàm thuần (`PayrollCalculator`).
- **GĐ5: Báo cáo & Dashboard.** Module `Reports` chỉ đọc dữ liệu từ các module khác, không module nào phụ thuộc vào nó.

## Kiểm thử và xác nhận
1. `dotnet build backend/Cenvo.sln`: 0 lỗi, 0 cảnh báo nullable.
2. `dotnet test backend/Cenvo.sln` (cần Docker): toàn bộ unit test và integration test ở Bước 0 vẫn xanh sau **mỗi** bước refactor.
3. Kiểm tra tiêu chí đọc code bằng grep:
   - Không còn file `Entities.cs` gom nhiều kiểu.
   - Không còn `goto`.
   - Không còn `DateTime.UtcNow` trong module (chỉ còn trong `SystemClock`).
   - Không còn raw SQL chéo module.
   - Không còn service nào trên khoảng 200 dòng.
4. `dotnet run --project backend/src/Cenvo.Api`: mở Swagger tại `:5000`, đăng nhập `thungan1/123456`, rồi chạy luồng mở ca → order → thanh toán → kiểm tra phiếu xuất bán và thẻ kho.
