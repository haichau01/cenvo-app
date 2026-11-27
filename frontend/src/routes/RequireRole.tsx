import { Link, Outlet } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import type { UserIdentity } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'

/** Hides screens the user's role cannot use. Navigation only — the backend still checks every request. */
export function RequireRole({ allow }: { allow: (user: UserIdentity) => boolean }) {
  const user = useSession()!.user
  if (allow(user)) return <Outlet />
  return (
    <EmptyState className="min-h-[60vh]" title="Bạn chưa có quyền vào màn này"
      description="Màn kho dành cho thủ kho và quản lý. Nhờ quản lý cấp quyền nếu bạn cần dùng."
      action={<Link to="/" className="font-semibold underline">Về trang chủ</Link>} />
  )
}
