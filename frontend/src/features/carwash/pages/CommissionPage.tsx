import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Money } from '@/components/cenvo/Money'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { controlClass } from '@/components/ui/fieldStyles'
import { vnDate } from '@/lib/format'
import { useCommissionReport } from '../api/reports'

/** First day of the current month in Vietnam (yyyy-MM-dd). */
function monthStart(today: string): string {
  return `${today.slice(0, 8)}01`
}

/** Managers: commission earned per washer for completed washes (cancelled tickets earn nothing). Feeds payroll later. */
export function CommissionPage() {
  const [today] = useState(() => vnDate(Date.now()))
  const [from, setFrom] = useState(() => monthStart(today))
  const [to, setTo] = useState(today)
  const report = useCommissionReport(from, to)

  return (
    <div data-module="wash" className="flex flex-col gap-6">
      <PageHeader title="Hoa hồng rửa xe" description="Tính theo ngày rửa xong; mỗi phiếu chia đều cho người rửa." />
      <div className="flex items-end gap-3">
        <label className="flex flex-col gap-1.5 text-sm font-medium">Từ ngày
          <input type="date" value={from} max={to} onChange={(event) => setFrom(event.target.value)} className={controlClass} />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">Đến ngày
          <input type="date" value={to} min={from} onChange={(event) => setTo(event.target.value)} className={controlClass} />
        </label>
      </div>
      {report.data && report.data.staff.length === 0 && (
        <EmptyState title="Chưa có xe nào rửa xong trong khoảng này" description="Hoa hồng ghi nhận khi nhân viên bấm “Rửa xong”." />
      )}
      {report.data && report.data.staff.length > 0 && (
        <table className="w-full max-w-3xl overflow-hidden rounded-(--radius-panel) bg-surface text-table">
          <thead className="border-b border-line text-caption text-ink-muted">
            <tr><th className="px-3 py-2 text-left font-medium">Nhân viên</th><th className="px-3 py-2 text-right font-medium">Số xe</th>
              <th className="px-3 py-2 text-right font-medium">Hoa hồng</th></tr>
          </thead>
          <tbody className="divide-y divide-line">
            {report.data.staff.map((row) => (
              <tr key={row.userId} className="h-11">
                <td className="px-3">{row.fullName}</td>
                <td className="px-3 text-right tabular-nums">{row.ticketCount}</td>
                <td className="px-3 text-right"><Money value={row.commission} /></td>
              </tr>
            ))}
          </tbody>
          <tfoot className="border-t border-line font-semibold">
            <tr className="h-11"><td className="px-3">Tổng</td><td /><td className="px-3 text-right"><Money value={report.data.total} /></td></tr>
          </tfoot>
        </table>
      )}
    </div>
  )
}
