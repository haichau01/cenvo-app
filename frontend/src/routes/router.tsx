import { createBrowserRouter, Navigate } from 'react-router'
import { prefetchKitchen, prefetchPos } from '@/features/cafe/prefetch'
import { prefetchIntake, prefetchQueue } from '@/features/carwash/prefetch'
import { canCheckout, canManageStock, canWash, isManager, isOwner } from '@/lib/auth/roles'
import { RequireAuth } from './RequireAuth'
import { RequireRole } from './RequireRole'

// Each page is lazy-loaded so the POS screen doesn't download inventory/report code.
export const router = createBrowserRouter([
  { path: '/dang-nhap', lazy: async () => ({ Component: (await import('@/features/auth/pages/LoginPage')).LoginPage }) },
  {
    path: '/chon-chi-nhanh',
    lazy: async () => ({ Component: (await import('@/features/auth/pages/BranchPickerPage')).BranchPickerPage }),
  },
  {
    Component: RequireAuth,
    children: [
      { index: true, lazy: async () => ({ Component: (await import('@/features/home/pages/HomePage')).HomePage }) },
      { path: '/lich-cua-toi', lazy: async () => ({ Component: (await import('@/features/schedule/pages/MySchedulePage')).MySchedulePage }) },
      { path: '/cham-cong', lazy: async () => ({ Component: (await import('@/features/attendance/pages/MyAttendancePage')).MyAttendancePage }) },
      { path: '/cham-cong/quet', lazy: async () => ({ Component: (await import('@/features/attendance/pages/PunchPage')).PunchPage }) },
      {
        element: <RequireRole allow={canCheckout} who="thu ngân và quản lý" />,
        children: [
          { path: '/may-cham-cong', lazy: async () => ({ Component: (await import('@/features/attendance/pages/KioskPage')).KioskPage }) },
        ],
      },
      { path: '/in/hoa-don/:id', lazy: async () => ({ Component: (await import('@/features/cafe/pages/ReceiptPreviewPage')).ReceiptPreviewPage }) },
      {
        lazy: async () => ({ Component: (await import('./layouts/CoreLayout')).CoreLayout }),
        children: [
          {
            element: <RequireRole allow={canManageStock} />,
            children: [
              { path: '/kho', element: <Navigate to="/kho/ton-kho" replace /> },
              { path: '/kho/ton-kho', lazy: async () => ({ Component: (await import('@/features/inventory/pages/StockPage')).StockPage }) },
              { path: '/kho/phieu', lazy: async () => ({ Component: (await import('@/features/inventory/pages/DocumentsPage')).DocumentsPage }) },
              { path: '/kho/phieu/moi', lazy: async () => ({ Component: (await import('@/features/inventory/pages/DocumentPage')).DocumentPage }) },
              { path: '/kho/phieu/:id', lazy: async () => ({ Component: (await import('@/features/inventory/pages/DocumentPage')).DocumentPage }) },
            ],
          },
          {
            element: <RequireRole allow={isManager} who="quản lý" />,
            children: [
              { path: '/bao-cao', lazy: async () => ({ Component: (await import('@/features/reports/pages/ReportsPage')).ReportsPage }) },
              { path: '/rua-xe/dich-vu', lazy: async () => ({ Component: (await import('@/features/carwash/pages/WashSetupPage')).WashSetupPage }) },
              { path: '/rua-xe/hoa-hong', lazy: async () => ({ Component: (await import('@/features/carwash/pages/CommissionPage')).CommissionPage }) },
              { path: '/rua-xe/nhac-khach', lazy: async () => ({ Component: (await import('@/features/carwash/pages/RemindersPage')).RemindersPage }) },
              { path: '/thuong-phat', lazy: async () => ({ Component: (await import('@/features/hr/pages/AdjustmentsPage')).AdjustmentsPage }) },
              { path: '/lich-truc', lazy: async () => ({ Component: (await import('@/features/schedule/pages/SchedulePage')).SchedulePage }) },
              { path: '/cham-cong/quan-ly', lazy: async () => ({ Component: (await import('@/features/attendance/pages/AttendancePage')).AttendancePage }) },
            ],
          },
          {
            element: <RequireRole allow={isOwner} who="chủ quán" />,
            children: [
              { path: '/nhan-su', lazy: async () => ({ Component: (await import('@/features/hr/pages/EmployeesPage')).EmployeesPage }) },
              { path: '/nhan-su/moi', lazy: async () => ({ Component: (await import('@/features/hr/pages/EmployeePage')).EmployeePage }) },
              { path: '/nhan-su/:id', lazy: async () => ({ Component: (await import('@/features/hr/pages/EmployeePage')).EmployeePage }) },
            ],
          },
        ],
      },
      {
        lazy: async () => ({ Component: (await import('./layouts/WashLayout')).WashLayout }),
        children: [
          {
            element: <RequireRole allow={canWash} who="nhân viên rửa xe, thu ngân và quản lý" />,
            children: [
              {
                path: '/rua-xe',
                loader: prefetchQueue,
                lazy: async () => ({ Component: (await import('@/features/carwash/pages/WashQueuePage')).WashQueuePage }),
              },
              {
                path: '/rua-xe/tiep-nhan',
                loader: prefetchIntake,
                lazy: async () => ({ Component: (await import('@/features/carwash/pages/IntakePage')).IntakePage }),
              },
              { path: '/rua-xe/khach-hang', lazy: async () => ({ Component: (await import('@/features/carwash/pages/CustomersPage')).CustomersPage }) },
            ],
          },
        ],
      },
      {
        lazy: async () => ({ Component: (await import('./layouts/CafeLayout')).CafeLayout }),
        children: [
          {
            path: '/pos',
            // Start loading menu + floor plan in parallel with the page code (does not block navigation).
            loader: prefetchPos,
            lazy: async () => ({ Component: (await import('@/features/cafe/pages/PosPage')).PosPage }),
          },
          {
            path: '/pha-che',
            loader: prefetchKitchen,
            lazy: async () => ({ Component: (await import('@/features/cafe/pages/KitchenPage')).KitchenPage }),
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
