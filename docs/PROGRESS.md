# Tiến độ refactor Clean Code + SOLID: backend GĐ1

> Cập nhật: 2026-11-27. Plan: `docs/PLAN.md`.

## Trạng thái hiện tại: ✅ REFACTOR GĐ1 HOÀN TẤT

- `dotnet build`: 0 lỗi, 0 cảnh báo.
- `dotnet test`: **28/28 qua**, gồm 16 unit test và 12 integration test. 2 test cô lập chi nhánh từng hỏng nay đã qua.
- Ngày 2026-11-27 làm thêm:
  - Viết lại `Program.cs` và `DataSeeder.cs`.
  - Tạo lại migration `Initial`.
  - Thêm `PaymentCalculatorTests` và `KitchenIsolationTests`.
  - Sửa lỗi sai tên bảng trong `DocumentNumberGenerator`: câu SQL ghi `document_sequences`, trong khi bảng thật tên `document_sequence`.
  - CORS mặc định `http://localhost:5173` (Vite).
- Đã cập nhật tài liệu: `CLAUDE.md`, skill `cenvo-backend`, skill `cenvo-review` và `docs/PLAN.md`.
- **Frontend ReactJS (2026-11-27):**
  - ✅ Khởi tạo `frontend/`: Vite + React 19 + TS + React Router + TanStack Query + Tailwind v4 + token Cenvo + font Barlow.
  - ✅ Màn đăng nhập.
  - ✅ Chọn và đổi chi nhánh.
  - ✅ Trang chủ tạm.
  - ✅ Phiên đăng nhập: tự refresh token, giữ phiên khi tải lại trang, đăng xuất có thu hồi token.
  - Kiểm chứng:
    - `npm run build`, `npm run lint` sạch; `npm test` 3/3.
    - Chạy thử bằng Playwright + Edge ở tablet, điện thoại và desktop: sai mật khẩu báo lỗi tiếng Việt; thu ngân vào thẳng Quận 1; admin vào toàn chuỗi rồi đổi sang Thủ Đức; tải lại trang vẫn giữ phiên.
  - Backend: bật `NonNullableReferenceTypesAsRequired` trong Swagger để sinh kiểu TS chính xác.
- **Màn POS cà phê `/pos` (2026-11-27):** ✅
  - Sơ đồ bàn theo khu + hóa đơn mang đi; thực đơn theo nhóm; tùy chọn món (số lượng, topping mỗi ly, ghi chú).
  - Panel hóa đơn: tăng/giảm/xóa món, gửi pha chế, thanh toán (tiền mặt gợi ý mệnh giá, chuyển khoản, thẻ), hủy có lý do.
  - Mở ca / đóng ca **đếm mù** (không hiện tiền mặt dự kiến trước khi nhập số đếm; báo lệch sau khi đóng).
  - Realtime SignalR `ordersChanged` → máy khác thấy bàn đổi ngay; banner khi mất kết nối.
  - Các thao tác trên hóa đơn chạy tuần tự (TanStack `scope`) — trước đó chạm nhanh gây 409 và mất món.
  - Backend: sửa `/api/cafe/menu` trả 500 (OrderBy trên DTO), thêm `PosMenuTests`; Swagger sinh enum dạng chuỗi.
  - Kiểm chứng: `dotnet test` 29/29; `npm test` 15/15, lint, build sạch; chạy Playwright + Edge 1180×820 trọn luồng.
  - Chưa làm: chuyển/gộp bàn, giảm giá, in hóa đơn, sửa ghi chú dòng đã thêm.
- **Refactor query dễ đọc (2026-11-27):** ✅
  - Query Object `ProductQueries` (FinishedGoods/ActiveOnly/Matching/WithSellingPrice): giá bán chi nhánh từng chép 3 nơi, nay chỉ viết một lần.
  - `MenuQueryService` tách khỏi `MenuService`; `CafeItem.Configure`; trạng thái pha chế đi qua `CafeOrder.ChangeKitchenStatus` + `KitchenFlow` (tiến tùy ý, lùi 1 bước).
  - `LowStock` tính trong SQL; DTO dựng trong SQL đổi sang thuộc tính init (JSON không đổi).
  - Sửa 2 lỗi có từ trước: báo cáo `/api/reports/cafe-sales` trả 500; số hóa đơn hủy đếm theo `PaidAt` (luôn thiếu).
  - Skill mới `cenvo-readable-query`. Test: 41/41.
- **Việc tiếp theo:** màn pha chế (hàng chờ realtime), kho, báo cáo ngày; phần còn lại của POS ở trên.
- Còn treo: 2 cảnh báo EF về global filter (Unit ↔ ProductUnit, Branch ↔ UserBranch). Chưa ảnh hưởng hành vi.

> Phần dưới là nhật ký quá trình refactor, giữ lại để tham khảo.

## Đã xong

### Bước 0: Lưới an toàn ✅
- Cài `dotnet-ef` làm local tool (`backend/.config/dotnet-tools.json`).
- Có migration `Initial` (`src/Cenvo.Infrastructure/Persistence/Migrations`). **Cần tạo lại**, xem bước còn lại số 2.
- Test trong `tests/Cenvo.Tests`:
  - Integration (Testcontainers + `WebApplicationFactory`, gọi HTTP thật, đọc JSON bằng `JsonNode` nên không phụ thuộc kiểu C#):
    - `CafeSaleFlowTests`: bán trừ kho theo công thức rồi hủy thì hoàn kho; topping trừ theo từng ly; thanh toán khi chưa mở ca bị chặn; đóng ca đối soát tiền mặt.
    - `InventoryFlowTests`: nhập mua; chuyển kho 2 chi nhánh; kiểm kê; hủy phiếu nhập; chặn xuất âm.
    - `BranchIsolationTests`: không xem được hóa đơn và tồn kho của chi nhánh khác; thu ngân không vào được kho (403).
  - Unit: `CostCalculatorTests`, `RecipeExpanderTests`, `UnitConverterTests`.

### Lỗi đã phát hiện nhờ test
1. `/api/inventory/balances` trả 500, do lọc `ILike` *sau* khi `Select` sang record. **Đã sửa** trong `StockQueryService` mới.
2. **Rò rỉ dữ liệu giữa chi nhánh:** `IgnoreQueryFilters()` có hiệu lực với *toàn bộ* câu query, nên join `Product`/`Unit` kèm `IgnoreQueryFilters` sẽ tắt luôn filter chi nhánh của `StockBalance`. Lỗi tương tự có ở hàng chờ bếp (join `DiningTable`). **Đã sửa trong code mới:**
   - Không dùng `IgnoreQueryFilters` trong query có lọc chi nhánh.
   - Tên hiển thị được tra bằng query riêng theo danh sách id, rồi dựng dictionary.
   - Chỗ nào bắt buộc dùng thì có comment nêu lý do.

### Bước 1: SharedKernel / Infrastructure ✅
- SharedKernel tách mỗi kiểu một file. Thêm:
  - `SystemUser` (danh tính hệ thống, quyền Admin tường minh) để thay `CurrentUser.Override`, vốn trả `IsInRole = true` với mọi role.
  - `Roles.IsManager()`.
  - `Limits.MaxListSize`.
  - `IDocumentNumberGenerator`.
- Infrastructure:
  - `DocumentNumberGenerator` + bảng `document_sequences`: upsert nguyên tử, thay cho 3 bản `NextCode` đang lặp.
  - Khóa Guid được đặt `ValueGeneratedNever`, nên chỉ cần thêm dòng con vào collection là EF tự hiểu là bản ghi mới (không cần `db.Add`).
  - `AddModule` đăng ký lớp có hậu tố `Service|Issuer|Checker|Policy|Factory|Strategy`, kèm các interface `Cenvo.*` mà lớp đó cài.

### Bước 2–4: các module ✅ (cấu trúc `Domain/ Application/<Feature>/ Contracts/ Persistence/ Integration/`, mỗi project có `GlobalUsings.cs`)
- **Organization:**
  - `AppUser.SetPassword/VerifyPassword/SetRoles/SetBranches`.
  - Tách thành `AuthService`, `BranchService`, `UserService`.
  - Thêm `Contracts/IBranchStockPolicy` (`BranchService` cài).
  - Thêm `OrganizationErrors`.
- **Catalog:**
  - Tách thành `UnitService`, `CategoryService`, `ProductService`, `ProductQueryService`, `UnitConverterFactory`.
  - Thêm `Contracts/IUnitConverterFactory`, `Contracts/IProductUsageChecker` (Inventory cài; thay câu raw SQL vào `stock_movements`).
  - Thêm `CatalogErrors`.
- **Inventory:**
  - `StockDocument` tự giữ vòng đời: `CreateDraft`, `SetHeader`, `ReplaceLines`, `MarkPosted`, `MarkReceived`, `MarkCancelled`.
  - `StockBalance` tự tính giá vốn: `Receive`, `Issue`, `ReverseReceipt`.
  - `Posting/`: `StockService` (sổ kho) và 4 `IStockPostingStrategy` (Receipt, Issue, Transfer, Stocktake), thay cho `switch`.
  - Service: `WarehouseService`, `SupplierService`, `StockDocumentService`, `StockDocumentQueryService`, `StockQueryService`, `SaleStockIssuer : Contracts/ISaleStockIssuer`, `ProductUsageChecker`.
  - Thêm `InventoryErrors`.
- **Cafe:**
  - `CafeOrder` giữ toàn bộ quy tắc: `Open`, `AddLine`, `UpdateLine`, `RemoveLine`, `ApplyDiscount`, `MoveToTable`, `MoveLinesTo`, `SendToKitchen`, `Pay`, `Cancel`.
  - `Shift.Open/Close`.
  - Hàm thuần trong `Domain/Calculations/`: `PaymentCalculator`, `RecipeExpander`.
  - Service: `OrderQueryService`, `OrderEditingService`, `TableTransferService` (bỏ `goto`), `CheckoutService`, `KitchenService`, `ShiftService`, `AreaTableService`, `MenuService`, `RecipeService` (lưu công thức atomic, bỏ `ExecuteDelete`), `SalesReportService` (gom nhóm bằng SQL).
  - Handler chỉ phụ thuộc `ISaleStockIssuer` và `IUnitConverterFactory`.
  - Thêm `CafeErrors`; hằng số `CafeOrder.StockRefType` / `CodePrefix`.

### Bước 5: Api 🟡 (đang làm)
- ✅ Đã xóa `Controllers/`. Đã có `Endpoints/{Auth,Organization,Catalog,Inventory,Cafe}Endpoints.cs` (Minimal API, `MapGroup`), giữ **nguyên route và JSON** để test cũ chạy được.
- ✅ `Auth/Policies.cs` (Admin, Managers, Cashiers, Storekeepers); `CurrentUser.RunAs(ICurrentUser)`; claim đổi tên thành `CenvoClaims`.
- ✅ `ErrorHandler`: concurrency → 409, `BadHttpRequestException` → 400.
- ✅ `GlobalUsings.cs` (Api), `Hubs/KitchenHub.cs` (đã đổi using).

## Còn lại (làm theo thứ tự)

1. **`Program.cs`:**
   - `AddModule(typeof(OrganizationModel/CatalogModel/InventoryModel/CafeModel).Assembly)`.
   - Thêm using `Cenvo.Modules.Cafe.Contracts` cho `IKitchenNotifier`.
   - Bỏ `AddControllers`/`MapControllers`; thay bằng `ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()))`.
   - `AddAuthorization(o => o.AddCenvoPolicies())`.
   - Gọi `app.MapAuthEndpoints(); MapOrganizationEndpoints(); MapCatalogEndpoints(); MapInventoryEndpoints(); MapCafeEndpoints();`.
   - Khối migrate đổi `Override(...)` thành `RunAs(SystemUser.AllBranches)`.
2. **`Seed/DataSeeder.cs`:**
   - Tách thành `SeedBranchesAndUsers`, `SeedCatalog`, `SeedMenu`, `SeedFloorPlans`, `SeedOpeningStock`.
   - Dùng `RunAs(SystemUser.AllBranches / ForBranch(id))`.
   - Đổi `InventoryService` thành `StockDocumentService` (`StockDocumentInput`, `StockDocumentLineInput`).
   - Mật khẩu đặt bằng `user.SetPassword(...)`, role bằng `SetRoles`.
3. **Migration:** chưa có dữ liệu thật, nên xóa thư mục `Persistence/Migrations` rồi tạo lại `Initial` (thêm bảng `document_sequences` và vài index mới):
   `dotnet ef migrations add Initial -p src/Cenvo.Infrastructure -s src/Cenvo.Api -o Persistence/Migrations`.
   Còn 2 cảnh báo EF về global filter (Unit ↔ ProductUnit, Branch ↔ UserBranch); chưa xử lý, chỉ ghi lại.
4. **Test:**
   - Sửa namespace trong unit test: `Cenvo.Modules.Inventory.Domain` (CostCalculator), `Cenvo.Modules.Catalog.Domain` (UnitConverter), `Cenvo.Modules.Cafe.Domain` + `.Domain.Calculations` (SoldItem, RecipeItem, RecipeExpander).
   - Thêm `PaymentCalculatorTests` (tiền thối, chia tiền mặt + chuyển khoản, thiếu tiền, non-cash vượt tổng).
   - Thêm test cô lập hàng chờ bếp giữa 2 chi nhánh.
   - Chạy `dotnet test` (cần Docker) → **phải qua 100%**.
5. **Kiểm tra tiêu chí đọc code** bằng grep: không còn `goto`; không còn file `Entities.cs`; không còn raw SQL chéo module; không còn service nào trên khoảng 200 dòng.
6. **Tài liệu:**
   - Cập nhật `CLAUDE.md` và `.claude/skills/cenvo-backend/SKILL.md` theo cấu trúc mới: `Domain/`, `Application/<Feature>/`, `Contracts/`; hậu tố DI; quy tắc **không dùng `IgnoreQueryFilters` trong query có lọc chi nhánh**; `IDocumentNumberGenerator`; `ISaleStockIssuer`.
   - Thêm mục Clean Code & SOLID vào checklist review.
   - Chép plan vào `docs/PLAN.md`.

## Quyết định đã chốt (khác plan gốc, có lý do)
- **Không thêm `IClock`:** chưa có test nào cần giả lập thời gian; nguyên tắc "đơn giản trước" của dự án.
- **Không dùng FluentValidation:** các kiểm tra như "số lượng > 0" là quy tắc nghiệp vụ, đặt luôn trong method của entity.
- **Frontend:** chốt **ReactJS SPA (Vite + React Router)** theo yêu cầu 2026-11-27, không dùng Next.js.

## Lệnh hay dùng
```bash
cd backend
dotnet build
dotnet test                      # cần Docker Desktop đang chạy
dotnet run --project src/Cenvo.Api   # http://localhost:5000/swagger — admin/Admin@123, thungan1/123456
```
