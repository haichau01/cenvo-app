import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Money } from '@/components/cenvo/Money'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import type { PayAdjustment } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'
import { formatDate, vnDate } from '@/lib/format'
import { useAdjustments } from '../api/adjustments'
import { CancelAdjustmentDialog, RecordAdjustmentDialog } from '../components/AdjustmentDialogs'
import { ADJUSTMENT_KINDS, adjustmentLabel, adjustmentSign } from '../lib/labels'
import { monthRange } from '../lib/period'

/** Managers: advances, bonuses and penalties of their branch, by month. They feed the monthly payroll later. */
export function AdjustmentsPage() {
  const user = useSession()!.user
  const [month, setMonth] = useState(() => vnDate(Date.now()).slice(0, 7))
  const { from, to } = monthRange(month)
  const adjustments = useAdjustments(from, to)
  const [recording, setRecording] = useState(false)
  const [cancelling, setCancelling] = useState<PayAdjustment | null>(null)
  const active = (adjustments.data ?? []).filter((a) => a.status === 'Active')

  return (
    <>
      <PageHeader title="Thưởng, phạt, tạm ứng" description="Ghi kèm lý do; tính vào bảng lương tháng. Ghi nhầm thì hủy, không xóa."
        actions={user.branchId != null && <Button onClick={() => setRecording(true)}>Ghi khoản mới</Button>} />
      {user.branchId == null && <p className="mb-4 text-ink-muted">Đang xem toàn chuỗi — chọn một chi nhánh để ghi khoản mới.</p>}
      <div className="mb-4 flex flex-wrap items-end gap-6">
        <TextField label="Tháng" type="month" value={month} onChange={(e) => e.target.value && setMonth(e.target.value)} className="w-48" />
        {ADJUSTMENT_KINDS.map((kind) => (
          <div key={kind.value} className="flex flex-col">
            <span className="text-caption text-ink-muted">{kind.label}</span>
            <Money className="font-display text-block font-semibold"
              value={active.filter((a) => a.kind === kind.value).reduce((sum, a) => sum + a.amount, 0)} />
          </div>
        ))}
      </div>
      <FormError message={adjustments.error?.message} />
      {adjustments.data?.length === 0 && (
        <EmptyState title="Chưa có khoản nào trong tháng này"
          action={user.branchId != null && <Button variant="secondary" onClick={() => setRecording(true)}>Ghi khoản mới</Button>} />
      )}
      {adjustments.data && adjustments.data.length > 0 && (
        <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
          <thead className="border-b border-line text-caption text-ink-muted">
            <tr>
              <th className="px-3 py-2 text-left font-medium">Ngày</th>
              <th className="px-3 py-2 text-left font-medium">Nhân viên</th>
              <th className="px-3 py-2 text-left font-medium">Loại</th>
              <th className="px-3 py-2 text-right font-medium">Số tiền</th>
              <th className="px-3 py-2 text-left font-medium">Lý do</th>
              <th className="px-3 py-2 text-left font-medium">Người ghi</th>
              <th className="px-3 py-2"><span className="sr-only">Thao tác</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {adjustments.data.map((a) => (
              <tr key={a.id} className={`h-12 ${a.status === 'Cancelled' ? 'text-ink-muted' : ''}`}>
                <td className="px-3 tabular-nums">{formatDate(a.date)}</td>
                <td className="px-3 font-medium">{a.employeeName}</td>
                <td className="px-3">{adjustmentLabel(a.kind)}</td>
                <td className="px-3 text-right">
                  <Money value={a.status === 'Cancelled' ? a.amount : adjustmentSign(a.kind) * a.amount}
                    className={a.status === 'Cancelled' ? 'line-through' : ''} />
                </td>
                <td className="px-3">{a.reason}{a.cancelReason && <p className="text-caption">Hủy: {a.cancelReason}</p>}</td>
                <td className="px-3">{a.recordedByName}</td>
                <td className="px-3 text-right">
                  {a.status === 'Cancelled'
                    ? <StatusPill label="Đã hủy" tone="muted" />
                    : a.canCancel && <Button variant="ghost" onClick={() => setCancelling(a)}>Hủy</Button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {recording && <RecordAdjustmentDialog onClose={() => setRecording(false)} />}
      {cancelling && <CancelAdjustmentDialog adjustment={cancelling} onClose={() => setCancelling(null)} />}
    </>
  )
}
