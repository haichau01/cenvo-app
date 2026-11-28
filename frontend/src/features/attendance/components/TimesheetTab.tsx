import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import type { TimesheetTotals } from '@/lib/api/types'
import { useTimesheet } from '../api/attendance'
import { formatMinutes, monthTitle, shiftMonth } from '../lib/attendance'

const COLUMNS: { label: string; title?: string; value: (t: TimesheetTotals) => string | number }[] = [
  { label: 'Ca xếp', value: (t) => t.plannedShifts },
  { label: 'Ca đã làm', value: (t) => t.workedShifts },
  { label: 'Vắng', value: (t) => t.absentShifts },
  { label: 'Ngày thường', value: (t) => t.workDays },
  { label: 'Lễ đi làm', value: (t) => t.holidaysWorked },
  { label: 'Lễ nghỉ', value: (t) => t.holidaysOff },
  { label: 'Ngày công', title: 'Ngày thường + lễ đi làm × hệ số + lễ nghỉ', value: (t) => t.paidDays },
  { label: 'Giờ làm', value: (t) => formatMinutes(t.workedMinutes) },
  { label: 'Đi muộn', value: (t) => t.lateCount },
  { label: 'Về sớm', value: (t) => t.earlyLeaveCount },
  { label: 'Tăng ca', value: (t) => formatMinutes(t.overtimeMinutes) },
  { label: 'Tăng ca lễ', value: (t) => formatMinutes(t.holidayOvertimeMinutes) },
]

/** The month per person: what payroll will read (paid days, approved overtime) and what is still open. */
export function TimesheetTab({ today }: { today: string }) {
  const [month, setMonth] = useState(() => shiftMonth(today, 0))
  const sheet = useTimesheet(month)

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-2">
        <Button variant="ghost" onClick={() => setMonth(shiftMonth(month, -1))}>Tháng trước</Button>
        <p className="w-40 text-center font-display text-block font-semibold">{monthTitle(month)}</p>
        <Button variant="ghost" disabled={month >= shiftMonth(today, 0)} onClick={() => setMonth(shiftMonth(month, 1))}>Tháng sau</Button>
        {sheet.data && sheet.data.holidays.length > 0 && (
          <p className="ml-4 text-ink-muted">Ngày lễ: {sheet.data.holidays.map((h) => `${h.name} (×${h.multiplier})`).join(', ')}</p>
        )}
      </div>
      <FormError message={sheet.error?.message} />
      {sheet.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {sheet.data && sheet.data.rows.length === 0 && <EmptyState title="Tháng này chưa có ca nào được xếp hay chấm công" />}
      {sheet.data && sheet.data.rows.length > 0 && (
        <div className="overflow-x-auto rounded-(--radius-panel) bg-surface">
          <table className="w-full text-table">
            <thead className="border-b border-line text-caption text-ink-muted">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Nhân viên</th>
                {COLUMNS.map((c) => <th key={c.label} title={c.title} className="px-2 py-2 text-right font-medium">{c.label}</th>)}
                <th className="px-4 py-2 text-left font-medium">Còn phải xử lý</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {sheet.data.rows.map((row) => (
                <tr key={row.employeeId} className="h-11">
                  <td className="px-4 font-medium">{row.employeeName}</td>
                  {COLUMNS.map((c) => <td key={c.label} className="px-2 text-right tabular-nums">{c.value(row.totals)}</td>)}
                  <td className="px-4 text-caption"><Open totals={row.totals} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function Open({ totals }: { totals: TimesheetTotals }) {
  const items = [
    totals.pendingApprovals > 0 && `${totals.pendingApprovals} chờ duyệt`,
    totals.forgottenCheckOuts > 0 && `${totals.forgottenCheckOuts} quên chấm ra`,
    totals.unreviewedOvertimeMinutes > 0 && `${formatMinutes(totals.unreviewedOvertimeMinutes)} tăng ca chờ duyệt`,
    totals.ownScreenPunches > 0 && `${totals.ownScreenPunches} lượt tự chấm trên máy mình`,
  ].filter(Boolean)
  return items.length === 0 ? <span className="text-ink-muted">—</span> : <span className="font-medium text-stop">{items.join(' · ')}</span>
}
