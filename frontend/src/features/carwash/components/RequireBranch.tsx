import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { useSession } from '@/lib/auth/session'

/** Car wash screens work inside one branch; an admin viewing the whole chain picks one first. */
export function RequireBranch({ from, children }: { from: string; children: ReactNode }) {
  const user = useSession()!.user
  if (user.branchId != null) return children
  return (
    <EmptyState className="flex-1" title="Chọn chi nhánh để làm việc"
      description="Đang xem toàn chuỗi. Màn rửa xe cần làm việc tại một chi nhánh cụ thể."
      action={<Link to="/chon-chi-nhanh" state={{ from }} className="font-semibold text-(--module) underline">Chọn chi nhánh</Link>} />
  )
}
