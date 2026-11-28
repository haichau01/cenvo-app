import { useState } from 'react'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import type { AttendanceRow, TimesheetTotals } from '@/lib/api/types'
import { formatDate, formatTime, vnDate } from '@/lib/format'
import { useMyAttendance } from '../api/attendance'
import { PhoneShell } from '../components/PhoneShell'
import { PinSetup } from '../components/PinSetup'
import { formatMinutes, monthTitle, rowStatus, shiftMonth } from '../lib/attendance'

/** Every staff member, on their phone: this month's punches and what they count for, in every branch. */
export function MyAttendancePage() {
  const [thisMonth] = useState(() => shiftMonth(vnDate(Date.now()), 0))
  const [month, setMonth] = useState(thisMonth)
  const mine = useMyAttendance(month)

  return (
    <PhoneShell>
      <div>
        <h1 className="font-display text-screen font-semibold">Chấm công của tôi</h1>
        <p className="text-ink-muted">Vào ca / ra ca: quét mã QR trên máy ở quán bằng camera điện thoại.</p>
      </div>
      <div className="flex items-center justify-between">
        <Button variant="ghost" onClick={() => setMonth(shiftMonth(month, -1))}>Tháng trước</Button>
        <p className="font-display text-block font-semibold">{monthTitle(month)}</p>
        <Button variant="ghost" disabled={month >= thisMonth} onClick={() => setMonth(shiftMonth(month, 1))}>Tháng sau</Button>
      </div>
      <FormError message={mine.error?.message} />
      {mine.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {mine.data && <Totals totals={mine.data.totals} holidays={mine.data.holidays.length} />}
      {mine.data && <PunchList rows={mine.data.records} />}
      <PinSetup />
    </PhoneShell>
  )
}

function Totals({ totals, holidays }: { totals: TimesheetTotals; holidays: number }) {
  const tiles: [string, string][] = [
    ['Ngày công', String(totals.paidDays)],
    ['Giờ làm', formatMinutes(totals.workedMinutes)],
    ['Tăng ca đã duyệt', formatMinutes(totals.overtimeMinutes + totals.holidayOvertimeMinutes)],
    ['Đi muộn', `${totals.lateCount} lần`],
  ]
  return (
    <section className="flex flex-col gap-2">
      <dl className="grid grid-cols-2 gap-2">
        {tiles.map(([label, value]) => (
          <div key={label} className="rounded-(--radius-panel) bg-surface px-4 py-3">
            <dt className="text-caption text-ink-muted">{label}</dt>
            <dd className="font-display text-block font-semibold tabular-nums">{value}</dd>
          </div>
        ))}
      </dl>
      <p className="text-caption text-ink-muted">
        {holidays > 0 && `Gồm ${holidays} ngày lễ trong tháng. `}
        {totals.pendingApprovals > 0 && `${totals.pendingApprovals} lượt ngoài lịch đang chờ quản lý duyệt. `}
        {totals.absentShifts > 0 && `Vắng ${totals.absentShifts} ca đã xếp.`}
      </p>
    </section>
  )
}

function PunchList({ rows }: { rows: AttendanceRow[] }) {
  if (rows.length === 0) return <p className="rounded-(--radius-panel) bg-surface p-4 text-ink-muted">Tháng này chưa có lượt chấm công nào.</p>
  return (
    <ol className="flex flex-col gap-2">
      {[...rows].reverse().map((row) => {
        const status = rowStatus(row)
        return (
          <li key={row.id} className="flex items-start justify-between gap-3 rounded-(--radius-panel) bg-surface px-4 py-3">
            <div>
              <p className="font-semibold">{formatDate(row.workDate)} · {row.shiftName ?? 'Ngoài lịch'}</p>
              <p className="tabular-nums">{formatTime(row.checkInAt)} – {row.checkOutAt ? formatTime(row.checkOutAt) : '…'}
                {row.lateMinutes > 0 && <span className="text-stop"> · muộn {formatMinutes(row.lateMinutes)}</span>}</p>
              <p className="text-caption text-ink-muted">{row.branchName}</p>
            </div>
            <StatusPill label={status.label} tone={status.tone} />
          </li>
        )
      })}
    </ol>
  )
}
