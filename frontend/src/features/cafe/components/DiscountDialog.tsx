import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { MoneyField } from '@/components/ui/MoneyField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { OrderDto } from '@/lib/api/types'
import { isManager } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { formatVnd } from '@/lib/format'
import { useDiscountLine, useDiscountOrder } from '../api/orders'
import {
  DISCOUNT_REASONS, discountAmount, discountBase, needsManagerApproval, totalDiscountAfter,
  type DiscountMode, type DiscountTarget,
} from '../lib/discount'

const QUICK_PERCENTS = [5, 10, 20, 50, 100]

interface DiscountDialogProps {
  order: OrderDto
  target: DiscountTarget | null
  onClose: () => void
}

/** Discount the whole bill or one item. Reason required; above the cashier's limit a manager types their account here. */
export function DiscountDialog({ order, target, onClose }: DiscountDialogProps) {
  const title = target?.kind === 'line' ? `Giảm giá ${target.line.name}` : 'Giảm giá hóa đơn'
  return (
    <Dialog open={target !== null} onClose={onClose} title={title}>
      {target && <DiscountForm key={target.kind === 'line' ? target.line.id : 'order'} order={order} target={target} onDone={onClose} />}
    </Dialog>
  )
}

function DiscountForm({ order, target, onDone }: { order: OrderDto; target: DiscountTarget; onDone: () => void }) {
  const manager = isManager(useSession()!.user)
  const discountOrder = useDiscountOrder()
  const discountLine = useDiscountLine()
  const mutation = target.kind === 'order' ? discountOrder : discountLine
  const [mode, setMode] = useState<DiscountMode>('percent')
  const [value, setValue] = useState(0)
  const [reason, setReason] = useState(target.kind === 'line' ? target.line.discountReason ?? '' : order.discountReason ?? '')
  const [credentials, setCredentials] = useState({ userName: '', password: '' })

  const base = discountBase(order, target)
  const amount = discountAmount(mode, value, base)
  const needsManager = needsManagerApproval(order, totalDiscountAfter(order, target, amount), manager)
  const missing = (amount > 0 && !reason.trim()) || (needsManager && (!credentials.userName || !credentials.password))

  function handleApply() {
    const common = { orderId: order.id, reason: reason.trim() || null, approval: needsManager ? credentials : null }
    const onSuccess = () => {
      toast.success(amount > 0 ? `Đã giảm ${formatVnd(amount)}` : 'Đã bỏ giảm giá')
      onDone()
    }
    if (target.kind === 'line') discountLine.mutate({ ...common, lineId: target.line.id, amount }, { onSuccess })
    else discountOrder.mutate({ ...common, amount: mode === 'amount' ? amount : null, percent: mode === 'percent' ? value : null }, { onSuccess })
  }

  return (
    <div className="flex flex-col gap-5">
      <ModePicker mode={mode} onMode={(next) => { setMode(next); setValue(0) }} />
      {mode === 'percent' && <PercentPicker value={value} onValue={setValue} />}
      {mode === 'amount' && <MoneyField label="Số tiền giảm" value={value} onChange={setValue} autoFocus />}
      <p className="flex items-baseline justify-between" aria-live="polite">
        <span className="text-ink-muted">Giảm trên <Money value={base} /></span>
        <Money value={-amount} className="font-display text-2xl font-bold" />
      </p>

      <ReasonPicker reason={reason} onReason={setReason} />
      {needsManager && (
        <ManagerApprovalFields limit={order.cashierDiscountLimit} value={credentials} onChange={setCredentials} />
      )}
      <FormError message={mutation.error?.message} />
      <Button size="lg" onClick={handleApply} busy={mutation.isPending} disabled={missing}>
        {amount > 0 ? 'Áp dụng giảm giá' : 'Bỏ giảm giá'}
      </Button>
    </div>
  )
}

function ModePicker({ mode, onMode }: { mode: DiscountMode; onMode: (mode: DiscountMode) => void }) {
  const options: { value: DiscountMode; label: string }[] = [{ value: 'percent', label: 'Theo %' }, { value: 'amount', label: 'Số tiền' }]
  return (
    <div role="radiogroup" aria-label="Cách giảm" className="grid grid-cols-2 gap-2">
      {options.map((option) => (
        <button key={option.value} type="button" role="radio" aria-checked={mode === option.value} onClick={() => onMode(option.value)}
          className={`h-12 rounded-(--radius-control) border-2 font-semibold
            ${mode === option.value ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
          {option.label}
        </button>
      ))}
    </div>
  )
}

function PercentPicker({ value, onValue }: { value: number; onValue: (value: number) => void }) {
  return (
    <div className="grid grid-cols-5 gap-2">
      {QUICK_PERCENTS.map((percent) => (
        <button key={percent} type="button" aria-pressed={value === percent} onClick={() => onValue(percent)}
          className={`h-12 rounded-(--radius-control) border font-semibold tabular-nums
            ${value === percent ? 'border-ink bg-ink text-white' : 'border-line bg-surface'}`}>
          {percent}%
        </button>
      ))}
    </div>
  )
}

function ReasonPicker({ reason, onReason }: { reason: string; onReason: (reason: string) => void }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-2">
        {DISCOUNT_REASONS.map((text) => (
          <button key={text} type="button" onClick={() => onReason(text)} aria-pressed={reason === text}
            className={`h-12 rounded-full border px-4 font-medium ${reason === text ? 'border-ink bg-ink text-white' : 'border-line'}`}>
            {text}
          </button>
        ))}
      </div>
      <TextField label="Lý do giảm giá" value={reason} onChange={(event) => onReason(event.target.value)} maxLength={200} />
    </div>
  )
}

interface ManagerApprovalFieldsProps {
  limit: number
  value: { userName: string; password: string }
  onChange: (value: { userName: string; password: string }) => void
}

/** Shown only above the limit: the manager types their own account on this device; nothing is stored. */
function ManagerApprovalFields({ limit, value, onChange }: ManagerApprovalFieldsProps) {
  return (
    <fieldset className="flex flex-col gap-3 rounded-(--radius-panel) border border-lane bg-lane/10 p-4">
      <legend className="px-1 font-semibold">Cần quản lý duyệt</legend>
      <p className="text-sm">Tổng giảm vượt <Money value={limit} /> (10% hóa đơn). Quản lý nhập tài khoản của mình để duyệt.</p>
      <div className="grid grid-cols-2 gap-3">
        <TextField label="Tài khoản quản lý" autoComplete="off" value={value.userName}
          onChange={(event) => onChange({ ...value, userName: event.target.value })} />
        <TextField label="Mật khẩu" type="password" autoComplete="off" value={value.password}
          onChange={(event) => onChange({ ...value, password: event.target.value })} />
      </div>
    </fieldset>
  )
}
