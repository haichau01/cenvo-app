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
- **Màn pha chế `/pha-che` (2026-11-27):** ✅
  - 3 cột Chờ pha → Đang pha → Chờ mang ra; một chạm để chuyển cột, nút "Lùi" để sửa chạm nhầm (khớp `KitchenFlow`).
  - Realtime: món gửi từ POS hiện sau khoảng 50 ms, nháy nền `lane` một lần; POS thấy trạng thái món đổi theo.
  - Cờ "Chờ lâu" khi món chưa xong sau 10 phút; chế độ tối cho ca đêm, lưu trên máy.
  - Thao tác tuần tự (TanStack scope) + cập nhật lạc quan; kiểm bằng Playwright 2 máy.
- **Màn kho (2026-11-27):** ✅ bố cục desktop (`CoreLayout`, sidebar 240px), chỉ thủ kho/quản lý vào được (`RequireRole`).
  - `/kho/ton-kho`: lọc kho, dưới mức tối thiểu, tìm không dấu (lọc ở trình duyệt); bấm dòng mở thẻ kho theo khoảng ngày.
  - `/kho/phieu`: danh sách lọc theo loại/trạng thái; báo phiếu chuyển kho đang chờ nhận.
  - `/kho/phieu/:id`: soạn 6 loại phiếu (cấu hình `DOCUMENT_TYPES`, không switch), chọn đơn vị quy đổi, lưu nháp/ghi sổ/xóa nháp; xem phiếu đã ghi sổ; hủy có lý do (quản lý); xác nhận nhận hàng chuyển kho; kiểm kê hiện sổ sách/thực tế/chênh lệch.
  - Kiểm chứng: Playwright 1440×900 với thukho1, quanly1, quanly2, thungan1 — nhập 2 hộp = 2.000 ml, hủy hoàn kho, kiểm kê điều chỉnh, chuyển kho 2 chi nhánh.
  - Còn treo: `/api/catalog/products?q=` và `/api/inventory/balances?q=` dùng ILIKE nên phân biệt dấu (cần extension `unaccent` nếu muốn tìm ở server).
- **Báo cáo bán hàng `/bao-cao` (2026-11-27):** ✅ chỉ quản lý.
  - Chọn kỳ: Hôm nay / Hôm qua / 7 ngày / Tháng này / Tháng trước hoặc ngày tùy chọn (giờ VN).
  - Ô số: doanh thu (nổi bật), lãi gộp + biên, giá vốn, số hóa đơn, trung bình, số hủy.
  - Biểu đồ cột doanh thu theo ngày (SVG tự vẽ, không thêm thư viện; tooltip bằng chuột và bàn phím; có dạng bảng).
  - Món bán chạy, hình thức thanh toán, đối soát ca (chênh lệch tiền mặt), nguyên liệu đã dùng.
  - Định dạng ngày giờ gom về `lib/format` (`formatDate`, `formatDateTime`, `formatShortDateTime`).
- **In hóa đơn (2026-11-27):** ✅
  - Backend `GET /api/cafe/orders/{id}/receipt` (`ReceiptQueryService`; thông tin chi nhánh qua contract mới `IBranchDirectory`; tên thu ngân lấy từ ca) + 3 integration test (có cô lập chi nhánh).
  - POS: "In tạm tính"; ô "In hóa đơn sau khi thanh toán" (nhớ theo máy, mặc định bật); "In lại" hóa đơn đã thanh toán hôm nay.
  - `/in/hoa-don/:id`: xem trước, chọn khổ 80/58 mm theo máy, in.
  - In trong trang (không dùng iframe, để không xoay refresh token); khổ giấy đo theo chiều cao thật, PDF kiểm được 80 × 121 mm, 1 trang.
- **Phân hệ Rửa xe (2026-11-28):** ✅ chưa commit.
  - Nghiệp vụ đã chốt với người dùng: chỉ nhận ô tô (4–5 chỗ, 7 chỗ, bán tải, SUV); thu tiền tại POS, **chung ca thu ngân**; hoa hồng cấu hình theo dịch vụ (% giá hoặc số tiền/xe), chia đều cho người rửa, tính trên giá niêm yết kể cả khi gói trả.
  - Backend `Cenvo.Modules.CarWash` (phụ thuộc Cafe):
    - Dịch vụ (`WashOffering`): giá theo loại xe, định mức hóa chất mỗi xe.
    - Phiếu: Chờ → Đang rửa → Xong → Đã giao. "Rửa xong" chia hoa hồng và phát `WashTicketCompleted` để trừ hóa chất qua `ISaleStockIssuer`. Hủy phiếu đã rửa thì hoàn kho; phiếu đã rửa hoặc đã thu tiền cần quản lý.
    - Khách nhận diện bằng SĐT, xe bằng biển số chuẩn hóa; cả hai dùng chung toàn chuỗi. Không nhập SĐT thì phiếu thuộc chủ xe đã biết.
    - Gói thành viên: bán trong ca; trừ lượt khi thêm dịch vụ vào phiếu, hoàn lượt khi bỏ dịch vụ hoặc hủy phiếu; chỉ hủy được gói chưa dùng, tại chi nhánh đã bán.
    - Ảnh hiện trạng: kiểm định dạng bằng byte đầu (JPEG/PNG/WebP, ≤ 5 MB, ≤ 8 ảnh). Lưu đĩa ngoài web root (`Storage:Photos:Root`), chỉ đọc được qua phiếu (có lọc chi nhánh).
    - Cafe thêm `ICashierShifts` và `IShiftTakingsSource`: đóng ca đối soát cả tiền rửa xe và tiền bán gói. Organization thêm `IStaffDirectory`, role `Washer`.
    - Hub `/hubs/carwash`, tín hiệu `ticketsChanged`. Migration `AddCarWash` (chỉ tạo bảng mới). Seed rửa xe chạy riêng một lần, nên DB cũ cũng được bổ sung (`ruaxe1..3`).
  - Frontend:
    - `/rua-xe`: hàng chờ 4 làn, realtime, chế độ tối, sheet chi tiết (dịch vụ, người rửa, ảnh, thu tiền, hủy).
    - `/rua-xe/tiep-nhan`: tra biển số, tự điền xe quen và dùng gói.
    - `/rua-xe/khach-hang`: lịch sử, bán và hủy gói.
    - `/rua-xe/dich-vu` và `/rua-xe/hoa-hong` cho quản lý.
    - Chip `<Plate>`; realtime nhiều hub; `TouchRail` dùng chung.
  - Kiểm chứng:
    - `dotnet test` 77/77 (unit: hoa hồng, biển số, SĐT, ảnh, vòng đời phiếu; integration: trừ/hoàn hóa chất, tiền vào ca, gói, tra biển số, washer bị 403 khi thu tiền, ảnh và cô lập chi nhánh).
    - `npm test` 54/54, lint và build sạch.
    - Playwright + Edge 1180×820 / 1440×900: tiếp nhận → ảnh → máy thứ hai bắt đầu và xong → máy thu ngân thấy realtime → thu tiền; không có lỗi console.
  - Còn lại: in phiếu rửa xe; nhắc khách quay lại; báo cáo doanh thu rửa xe; Payroll đọc hoa hồng (event đã mang sẵn dữ liệu).
- Rửa xe đã commit (`766e1b3`, nhánh `feat/cafe-pos`).
- **POS: giảm giá + chuyển/gộp/tách bàn (2026-11-28, nhánh `feat/pos-table-discount`):** ✅ chưa commit.
  - Quy tắc người dùng chốt:
    - Thu ngân tự giảm tối đa 10% hóa đơn; tổng giảm tính cả giảm theo món và giảm cả hóa đơn.
    - Vượt 10% thì quản lý nhập tài khoản của mình ngay trên máy thu ngân (`IManagerApproval`). Quản lý đang đăng nhập thì tự duyệt.
    - Bắt buộc lý do; giảm được cả hóa đơn lẫn từng món.
  - Duyệt chỉ phủ khoản giảm bằng hoặc nhỏ hơn lúc duyệt: giảm thêm thì phải duyệt lại. Lúc thanh toán kiểm lại một lần nữa (phòng trường hợp bỏ món hoặc gộp bàn làm vượt mức).
  - Giảm theo % lưu `DiscountPercent`, nên tự tính lại khi thêm, bỏ hoặc tách món. Trước đây % bị lưu thành số tiền cố định: tách món thì 20% thành 33%.
  - Ghi người giảm và người duyệt trên hóa đơn; `ShiftSummary.Discounts` liệt kê từng hóa đơn có giảm giá (lý do, người duyệt), hiện ở dialog đóng ca.
  - Rate limit `Credentials` (10 lần/phút theo người dùng, hoặc theo IP khi chưa đăng nhập) cho đăng nhập và giảm giá. Test nâng giới hạn qua `RateLimits:CredentialAttemptsPerMinute`.
  - API đổi:
    - `PUT /orders/{id}/lines/{lineId}` không còn nhận `discountAmount`.
    - Thêm `PUT /orders/{id}/lines/{lineId}/discount`.
    - Giảm giá được tách sang `DiscountService`.
  - Migration `AddDiscountAudit`: chỉ thêm cột nullable.
  - Frontend:
    - `DiscountDialog`: %/số tiền, lý do nhanh, ô quản lý duyệt chỉ hiện khi vượt mức.
    - `MoveDialog`: chọn món, chọn bàn trống (chuyển hoặc tách), bàn có khách (gộp), hoặc hóa đơn mang đi mới.
  - Kiểm chứng:
    - `dotnet test` 92/92; `npm test` 58/58; lint và build sạch.
    - Playwright + Edge 1180×820: sai mật khẩu quản lý bị chặn; quản lý duyệt 20%; giảm theo món phải duyệt lại; tách món sang mang đi.
- POS giảm giá + chuyển bàn đã commit (`fcab949`).
- **In phiếu rửa xe + báo cáo doanh thu rửa xe (2026-11-28, nhánh `feat/pos-table-discount`):** ✅ chưa commit.
  - Người dùng chốt: in cả phiếu nhận xe, hóa đơn rửa xe, phiếu bán gói; báo cáo là tab trong `/bao-cao` (Cà phê / Rửa xe / Tổng) với doanh thu, số xe, theo dịch vụ/loại xe, gói, hóa chất, hoa hồng, lãi gộp.
  - In: `GET /api/carwash/tickets/{id}/receipt` (WashStaff, lọc chi nhánh), `GET /api/carwash/memberships/{id}/receipt` (thu ngân, chỉ chi nhánh đã bán, nơi khác 404). Tên thu ngân qua `ICashierShifts.CashierName`. Một phiếu in theo trạng thái: chưa thu tiền = "Phiếu nhận xe" (giữ để nhận xe), đã thu = "Hóa đơn rửa xe".
  - Frontend: khung phiếu chung `components/cenvo/Slip.tsx` (hóa đơn cà phê cũng dùng); ô "In phiếu nhận xe" ở tiếp nhận, "In hóa đơn sau khi thu tiền / bán gói" (nhớ theo máy, mặc định bật); nút In trong sheet phiếu; "In lại" gói ở trang khách hàng. `vehicle.ts` chuyển lên `lib/`.
  - Báo cáo: `GET /api/reports/carwash-sales` (`WashSalesReportService`, gom nhóm trong SQL, cắt ngày VN bằng `AT TIME ZONE`). Phiếu tính theo **giờ thu tiền** (khớp đối soát ca). Doanh thu = tiền rửa + tiền bán gói; lãi gộp = doanh thu − hóa chất − hoa hồng; lượt trừ gói tính theo giá niêm yết, không phải tiền trong kỳ. Giá vốn hóa chất qua contract mới `Inventory/Contracts/ISaleCostQuery` (trả `IQueryable` để ghép SQL). Migration `AddWashSalesIndexes`: chỉ thêm 2 index.
  - Biểu đồ tổng quát (cột chồng Cà phê robusta / Rửa xe foam, có chú giải + bảng). Validator dataviz: cặp màu tách tốt (ΔE ≥ 14), robusta dưới ngưỡng chroma, bù bằng chú giải/khe/bảng.
  - Kiểm chứng: `dotnet test` 102/102; `npm test` 64/64, lint, build sạch; Playwright + Edge: in 3 loại phiếu (khổ 80 × 126 mm), báo cáo 3 tab, không lỗi console.
- **Việc tiếp theo:** commit (chạy `/cenvo-security` + `/cenvo-review` trước — đã chạy, không có mục Cao); sau đó nhắc khách quay lại, Payroll đọc hoa hồng.
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
