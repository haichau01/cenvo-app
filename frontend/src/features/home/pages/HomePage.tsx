import { Link } from 'react-router'
import { Wordmark } from '@/components/cenvo/Wordmark'
import { Button } from '@/components/ui/Button'
import { useLogout } from '@/features/auth/api/auth'
import { canWorkAcrossBranches, currentBranchName } from '@/features/auth/lib/branches'
import { canManageStock, canWash, isManager, isOwner } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'

/** Temporary home page after login — entry to POS, kitchen, car wash and inventory. */
export function HomePage() {
  const user = useSession()!.user
  const logout = useLogout()
  const canSwitch = user.branches.length > 1 || (canWorkAcrossBranches(user) && user.branches.length > 0)

  return (
    <div className="min-h-dvh bg-concrete">
      <header className="flex h-16 items-center gap-4 border-b border-line bg-surface px-6">
        <Wordmark />
        <span className="ml-2 rounded-full bg-concrete px-3 py-1 text-sm font-medium text-ink">{currentBranchName(user)}</span>
        <span className="ml-auto text-sm text-ink-muted">{user.fullName}</span>
        {canSwitch && (
          <Link to="/chon-chi-nhanh" state={{ from: '/' }} className="rounded-(--radius-control) px-3 py-2 text-sm font-medium text-ink hover:bg-ink/5">
            Đổi chi nhánh
          </Link>
        )}
        <Button variant="secondary" onClick={() => logout.mutate()} busy={logout.isPending}>
          Đăng xuất
        </Button>
      </header>
      <main className="mx-auto max-w-2xl px-6 py-16">
        <h1 className="font-display text-screen font-semibold text-ink">Chào {user.fullName}</h1>
        <p className="mt-2 text-ink-muted">
          Bạn đã đăng nhập vào {currentBranchName(user)}.
        </p>
        <nav aria-label="Màn làm việc" className="mt-8 flex flex-wrap gap-3">
          <ScreenLink to="/pos" module="cafe">Mở màn bán hàng</ScreenLink>
          <ScreenLink to="/pha-che" module="cafe">Màn pha chế</ScreenLink>
          {canWash(user) && <ScreenLink to="/rua-xe" module="wash">Hàng chờ rửa xe</ScreenLink>}
          {canWash(user) && <ScreenLink to="/rua-xe/tiep-nhan" module="wash">Tiếp nhận xe</ScreenLink>}
          {canManageStock(user) && <ScreenLink to="/kho/ton-kho" module="core">Kho</ScreenLink>}
          {isManager(user) && <ScreenLink to="/bao-cao" module="core">Báo cáo</ScreenLink>}
          {isOwner(user) && <ScreenLink to="/nhan-su" module="core">Nhân viên</ScreenLink>}
          {isManager(user) && <ScreenLink to="/thuong-phat" module="core">Thưởng, phạt, tạm ứng</ScreenLink>}
        </nav>
      </main>
    </div>
  )
}

function ScreenLink({ to, module, children }: { to: string; module: 'cafe' | 'wash' | 'core'; children: string }) {
  return (
    <Link to={to} data-module={module}
      className="inline-flex h-14 items-center rounded-(--radius-control) bg-(--module) px-6 text-lg font-semibold text-white">
      {children}
    </Link>
  )
}
