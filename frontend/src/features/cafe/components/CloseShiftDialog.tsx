import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { Dialog } from '@/components/ui/Dialog'
import { MoneyField } from '@/components/ui/MoneyField'
import { toast } from '@/components/ui/toastStore'
import type { ShiftSummary } from '@/lib/api/types'
import { formatTime, formatVnd } from '@/lib/format'
import { cafeKeys } from '../api/keys'
import { useCloseShift } from '../api/shifts'

interface CloseShiftDialogProps {
  shift: ShiftSummary | null
  onClose: () => void
}

/**
 * End-of-shift cash reconciliation. Blind count: the expected cash (and total revenue, which reveals it)
 * is shown only after the cashier submits the counted amount, so the count can't just be copied.
 */
export function CloseShiftDialog({ shift, onClose }: CloseShiftDialogProps) {
  return (
    <Dialog open={shift !== null} onClose={onClose} title={shift ? `Đóng ca ${shift.code}` : ''}>
      {shift && <CloseShiftForm key={shift.id} shift={shift} onDone={onClose} />}
    </Dialog>
  )
}

function CloseShiftForm({ shift, onDone }: { shift: ShiftSummary; onDone: () => void }) {
  const queryClient = useQueryClient()
  const closeShift = useCloseShift()
  const [countedCash, setCountedCash] = useState(0)
  const [note, setNote] = useState('')

  function handleConfirm() {
    closeShift.mutate({ countedCash, note: note.trim() || null }, {
      onSuccess: (closed) => {
        toast.success(`Đã đóng ca ${closed.code} · ${differenceText(closed.difference ?? 0)}`)
        queryClient.setQueryData(cafeKeys.currentShift, null)
        onDone()
      },
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-table">
        <Row label="Mở lúc" value={formatTime(shift.openedAt)} />
        <Row label="Số hóa đơn" value={`${shift.orderCount} (hủy ${shift.cancelledCount})`} />
        {/* Car wash tickets / packages paid in this shift are in the same till. */}
        {shift.otherTakings.filter((takings) => takings.count > 0).map((takings) => (
          <Row key={takings.source} label={takings.source} value={`${takings.count} phiếu`} />
        ))}
        <Row label="Chuyển khoản" value={<Money value={shift.byMethod.Transfer ?? 0} />} />
        <Row label="Thẻ" value={<Money value={shift.byMethod.Card ?? 0} />} />
      </dl>
      {shift.discounts.length > 0 && <ShiftDiscounts discounts={shift.discounts} />}
      <p className="text-ink-muted">Đếm toàn bộ tiền mặt trong két (kể cả tiền đầu ca) rồi nhập vào. Hệ thống báo lệch sau khi đóng ca.</p>
      <MoneyField label="Tiền mặt đếm được" value={countedCash} onChange={setCountedCash} autoFocus />
      <label className="flex flex-col gap-1.5">
        <span className="font-medium">Ghi chú (nếu lệch)</span>
        <input value={note} onChange={(event) => setNote(event.target.value)} maxLength={300}
          className="h-12 rounded-(--radius-control) border border-line bg-surface px-3 outline-none
            focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam" />
      </label>
      <FormError message={closeShift.error?.message} />
      <Button size="lg" onClick={handleConfirm} busy={closeShift.isPending}>Đóng ca</Button>
    </div>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-3">
      <dt className="text-ink-muted">{label}</dt>
      <dd className="text-right font-medium tabular-nums">{value}</dd>
    </div>
  )
}

function differenceText(difference: number): string {
  if (difference === 0) return 'Khớp tiền'
  return difference > 0 ? `Dư ${formatVnd(difference)}` : `Thiếu ${formatVnd(-difference)}`
}

/** Every discounted bill of the shift with its reason and approver — reviewed before closing. */
function ShiftDiscounts({ discounts }: { discounts: ShiftSummary['discounts'] }) {
  return (
    <section aria-label="Giảm giá trong ca" className="flex flex-col gap-1 text-table">
      <h3 className="font-semibold">Giảm giá trong ca</h3>
      <ul className="divide-y divide-line">
        {discounts.map((discount) => (
          <li key={discount.orderCode} className="flex gap-3 py-1.5">
            <span className="tabular-nums text-ink-muted">{discount.orderCode}</span>
            <span className="flex-1">
              {discount.reasons}
              {discount.approvedByName && <span className="text-ink-muted"> · duyệt: {discount.approvedByName}</span>}
            </span>
            <Money value={-discount.amount} />
          </li>
        ))}
      </ul>
    </section>
  )
}
