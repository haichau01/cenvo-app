import { createBrowserRouter, Navigate } from 'react-router'
import { prefetchKitchen, prefetchPos } from '@/features/cafe/prefetch'
import { RequireAuth } from './RequireAuth'

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
