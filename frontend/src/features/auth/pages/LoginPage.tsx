import { Navigate, useLocation } from 'react-router'
import { useSession } from '@/lib/auth/session'
import { useLogin } from '../api/auth'
import { AuthLayout } from '../components/AuthLayout'
import { LoginForm } from '../components/LoginForm'

export function LoginPage() {
  const session = useSession()
  const from = (useLocation().state as { from?: string } | null)?.from ?? '/'
  const login = useLogin()

  // Already signed in → go to the intended page; RequireAuth redirects to the branch picker if needed.
  if (session) return <Navigate to={from} replace />

  return (
    <AuthLayout>
      <h1 className="font-display text-screen font-semibold text-ink">Đăng nhập</h1>
      <p className="mt-1.5 mb-8 text-ink-muted">Dùng tài khoản quản lý chi nhánh cấp cho bạn.</p>
      <LoginForm onSubmit={(values) => login.mutate(values)} busy={login.isPending} error={login.error?.message} />
    </AuthLayout>
  )
}
