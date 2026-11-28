import { Button } from '@/components/ui/Button'
import type { ShiftSummary } from '@/lib/api/types'

export type PosView = 'tables' | 'menu'

interface PosTopBarProps {
  view: PosView
  onView: (view: PosView) => void
  hasOrder: boolean
  branchName: string
  canCheckout: boolean
  shift: ShiftSummary | null | undefined
  onOpenShift: () => void
  onCloseShift: () => void
  onReprint: () => void
}

export function PosTopBar({ view, onView, hasOrder, branchName, canCheckout, shift, onOpenShift, onCloseShift, onReprint }: PosTopBarProps) {
  return (
    <header className="flex h-16 shrink-0 items-center gap-4 border-b border-line bg-surface px-6">
      <div role="tablist" aria-label="Chế độ xem" className="flex rounded-(--radius-control) bg-concrete p-1">
        <ViewTab active={view === 'tables'} onClick={() => onView('tables')}>Sơ đồ bàn</ViewTab>
        <ViewTab active={view === 'menu'} onClick={() => onView('menu')} disabled={!hasOrder}>Thực đơn</ViewTab>
      </div>
      <span className="text-sm text-ink-muted">{branchName}</span>
      <div className="ml-auto flex items-center gap-3">
        <Button variant="ghost" onClick={onReprint}>In lại</Button>
        {canCheckout && <ShiftStatus shift={shift} onOpen={onOpenShift} onClose={onCloseShift} />}
      </div>
    </header>
  )
}

interface ViewTabProps {
  active: boolean
  onClick: () => void
  disabled?: boolean
  children: string
}

function ViewTab({ active, onClick, disabled, children }: ViewTabProps) {
  return (
    <button type="button" role="tab" aria-selected={active} onClick={onClick} disabled={disabled}
      className={`h-10 rounded-[4px] px-4 font-semibold disabled:opacity-40 ${active ? 'bg-surface text-ink' : 'text-ink-muted'}`}>
      {children}
    </button>
  )
}

interface ShiftStatusProps {
  shift: ShiftSummary | null | undefined
  onOpen: () => void
  onClose: () => void
}

function ShiftStatus({ shift, onOpen, onClose }: ShiftStatusProps) {
  if (shift === undefined) return null
  if (shift === null) {
    return (
      <div className="flex items-center gap-3">
        <span className="inline-flex items-center gap-1.5 rounded-full bg-lane px-3 py-1 text-sm font-semibold text-ink">
          <span aria-hidden className="size-2 rounded-full bg-ink" />Chưa mở ca
        </span>
        <Button onClick={onOpen}>Mở ca</Button>
      </div>
    )
  }
  return (
    <div className="flex items-center gap-3">
      <span className="inline-flex items-center gap-1.5 text-sm font-medium">
        <span aria-hidden className="size-2 rounded-full bg-go" />Ca {shift.code}
      </span>
      <Button variant="secondary" onClick={onClose}>Đóng ca</Button>
    </div>
  )
}
