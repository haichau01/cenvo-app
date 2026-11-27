import { createBrowserRouter, Navigate } from 'react-router'
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
    children: [{ index: true, lazy: async () => ({ Component: (await import('@/features/home/pages/HomePage')).HomePage }) }],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
