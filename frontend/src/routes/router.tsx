import { createBrowserRouter, Navigate } from 'react-router'
import { prefetchKitchen, prefetchPos } from '@/features/cafe/prefetch'
import { canManageStock, isManager } from '@/lib/auth/roles'
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
