import { Link } from 'react-router'
import { Wordmark } from '@/components/cenvo/Wordmark'
import { Button } from '@/components/ui/Button'
import { useLogout } from '@/features/auth/api/auth'
import { canWorkAcrossBranches, currentBranchName } from '@/features/auth/lib/branches'
import { useSession } from '@/lib/auth/session'

/** Temporary home page after login — entry to POS; kitchen and inventory will hang off here later. */
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
          Bạn đã đăng nhập vào {currentBranchName(user)}. Pha chế, kho và báo cáo sẽ có ở bước tiếp theo.
        </p>
        <Link to="/pos" data-module="cafe"
          className="mt-8 inline-flex h-14 items-center rounded-(--radius-control) bg-(--module) px-6 text-lg font-semibold text-white">
          Mở màn bán hàng
        </Link>
      </main>
    </div>
  )
}
