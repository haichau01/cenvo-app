# Cenvo frontend

ReactJS SPA (React 19 + Vite + TypeScript + React Router + TanStack Query + Tailwind v4). Quy ước: skill `cenvo-frontend` và `cenvo-design-system`.

```bash
npm install
npm run dev        # http://localhost:5173 — /api và /hubs được proxy sang API .NET :5000 (đổi bằng CENVO_API_URL)
npm run build      # kiểm tra kiểu + build
npm run lint
npm test           # Vitest (qua scripts/vitest.mjs để tránh lỗi chữ hoa/thường ổ đĩa trên Windows)
npm run gen:api    # sinh lại src/lib/api/schema.d.ts khi API đổi (API phải đang chạy)
```

Tài khoản mẫu (seed): `admin / Admin@123`, `thungan1 / 123456`, `quanly1 / 123456`, `quanly2 / 123456`.

## Cấu trúc

```
src/
  main.tsx               font, CSS, QueryClient, khôi phục phiên rồi mới render
  routes/                router.tsx (route tải chậm), RequireAuth.tsx
  features/auth/         api/ (useLogin, useSwitchBranch, useLogout), components/, lib/ (hàm thuần + test), pages/
  features/home/         trang chủ tạm
  components/ui/         Button, TextField, FormError
  components/cenvo/      Wordmark
  lib/api/               client.ts (apiFetch, refresh 1 lần, ApiError), schema.d.ts (sinh tự động), types.ts
  lib/auth/session.ts    phiên: access token trong bộ nhớ, refresh token + user trong localStorage
```
