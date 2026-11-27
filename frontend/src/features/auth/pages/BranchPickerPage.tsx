import { Navigate, useLocation, useNavigate } from 'react-router'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import type { BranchSummary } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'
import { useLogout, useSwitchBranch } from '../api/auth'
import { AuthLayout } from '../components/AuthLayout'

/** Pick the working branch: after login (several branches) or via the "Đổi chi nhánh" (switch branch) link. */
export function BranchPickerPage() {
  const session = useSession()
  const navigate = useNavigate()
  const from = (useLocation().state as { from?: string } | null)?.from ?? '/'
  const switchBranch = useSwitchBranch()
  const logout = useLogout()

  if (!session) return <Navigate to="/dang-nhap" replace />
  const { user } = session

  function handlePick(branch: BranchSummary) {
    if (branch.id === user.branchId) return navigate(from, { replace: true })
    switchBranch.mutate(branch.id, { onSuccess: () => navigate(from, { replace: true }) })
  }

  return (
    <AuthLayout>
      <h1 className="font-display text-screen font-semibold text-ink">Chọn chi nhánh</h1>
      <p className="mt-1.5 mb-6 text-ink-muted">Chào {user.fullName}. Hôm nay bạn làm việc ở đâu?</p>
      <ul className="flex flex-col gap-3">
        {user.branches.map((branch) => (
          <li key={branch.id}>
            <BranchOption
              branch={branch}
              current={branch.id === user.branchId}
              busy={switchBranch.isPending && switchBranch.variables === branch.id}
              disabled={switchBranch.isPending}
              onPick={() => handlePick(branch)}
            />
          </li>
        ))}
      </ul>
      <div className="mt-5">
        <FormError message={switchBranch.error?.message} />
      </div>
      <Button variant="ghost" className="mt-4 self-start" onClick={() => logout.mutate()} busy={logout.isPending}>
        Đăng nhập tài khoản khác
      </Button>
    </AuthLayout>
  )
}

interface BranchOptionProps {
  branch: BranchSummary
  current: boolean
  busy: boolean
  disabled: boolean
  onPick: () => void
}

function BranchOption({ branch, current, busy, disabled, onPick }: BranchOptionProps) {
  return (
    <button
      type="button"
      onClick={onPick}
      disabled={disabled}
      aria-busy={busy || undefined}
      className={`flex min-h-16 w-full items-center gap-4 rounded-(--radius-panel) border bg-surface px-4 py-3 text-left
        transition-colors hover:border-ink/40 active:bg-concrete disabled:cursor-wait
        ${current ? 'border-ink' : 'border-line'}`}
    >
      <span className="font-display text-sm font-semibold text-ink-muted tabular-nums">{branch.code}</span>
      <span className="flex-1 text-lg font-medium text-ink">{branch.name}</span>
      {current && <span className="text-caption font-medium text-ink-muted">Đang làm việc</span>}
      {busy && <span aria-hidden className="size-4 animate-spin rounded-full border-2 border-ink border-r-transparent" />}
    </button>
  )
}
