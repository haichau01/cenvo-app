import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import type { ScheduleWeek } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'
import { vnDate } from '@/lib/format'
import { useWeek } from '../api/schedule'
import { ShiftCell } from '../components/ShiftCell'
import { TemplatesDialog } from '../components/TemplatesDialog'
import { addDays, dayLabel, DEPARTMENT_SHORT, hoursLabel, shiftHours, weekDays, weekStartOf } from '../lib/week'

/** Managers: who works which shift this week, with missing staff and long weeks flagged. Staff see changes at once. */
export function SchedulePage() {
  const user = useSession()!.user
  const [today] = useState(() => vnDate(Date.now()))
  const [monday, setMonday] = useState(() => weekStartOf(today))
  const [editingShifts, setEditingShifts] = useState(false)
  const week = useWeek(monday)

  if (user.branchId == null) return <EmptyState title="Chọn một chi nhánh để xếp lịch" description="Lịch trực xếp riêng cho từng chi nhánh." />

  const lastDay = dayLabel(addDays(monday, 6)).date
  return (
    <>
      <PageHeader title="Lịch trực" description={`Tuần ${dayLabel(monday).date} – ${lastDay}. Nhân viên thấy ca ngay khi được xếp.`}
        actions={<>
          <Button variant="secondary" onClick={() => setMonday(addDays(monday, -7))}>Tuần trước</Button>
          <Button variant="secondary" onClick={() => setMonday(weekStartOf(today))} disabled={monday === weekStartOf(today)}>Tuần này</Button>
          <Button variant="secondary" onClick={() => setMonday(addDays(monday, 7))}>Tuần sau</Button>
          <Button onClick={() => setEditingShifts(true)}>Ca làm</Button>
        </>} />
      <FormError message={week.error?.message} />
      {week.isPending && <div className="h-96 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {week.data && week.data.templates.length === 0 && (
        <EmptyState title="Chưa có ca làm nào" description="Tạo các ca của chi nhánh (Sáng, Chiều, Tối…) rồi xếp người vào lưới."
          action={<Button onClick={() => setEditingShifts(true)}>Tạo ca làm</Button>} />
      )}
      {week.data && week.data.templates.length > 0 && (
        <div className="flex flex-col gap-6">
          <WeekGrid week={week.data} today={today} />
          <StaffHours week={week.data} />
        </div>
      )}
      <TemplatesDialog open={editingShifts} onClose={() => setEditingShifts(false)} />
    </>
  )
}

function WeekGrid({ week, today }: { week: ScheduleWeek; today: string }) {
  const days = weekDays(week.weekStart)
  return (
    <div className="overflow-x-auto rounded-(--radius-panel) bg-surface">
      <table className="w-full min-w-[64rem] table-fixed text-table">
        <thead className="border-b border-line text-caption text-ink-muted">
          <tr>
            <th className="w-36 px-3 py-2 text-left font-medium">Ca</th>
            {days.map((day) => {
              const label = dayLabel(day)
              return (
                <th key={day} className={`px-2 py-2 text-left font-medium ${day === today ? 'text-ink' : ''}`}>
                  <span className="font-semibold">{label.weekday}</span> <span className="tabular-nums">{label.date}</span>
                  {day === today && <span className="ml-1">· Hôm nay</span>}
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {week.templates.map((shift) => (
            <tr key={shift.id} className="align-top">
              <th scope="row" className="px-3 py-3 text-left font-normal">
                <p className="font-semibold">{shift.name}</p>
                <p className="text-caption tabular-nums text-ink-muted">{shiftHours(shift.startTime, shift.endTime)}</p>
              </th>
              {days.map((day) => (
                <ShiftCell key={day} day={day} shift={shift} locked={day < today}
                  assignments={week.assignments.filter((a) => a.date === day && a.shiftTemplateId === shift.id)}
                  shortfalls={week.shortfalls.filter((s) => s.date === day && s.shiftTemplateId === shift.id)}
                  staff={week.staff} />
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** Hours per person this week in every branch; above the limit is allowed but flagged. */
function StaffHours({ week }: { week: ScheduleWeek }) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-display text-block font-semibold">Giờ làm tuần này</h2>
      <ul className="grid grid-cols-[repeat(auto-fill,minmax(16rem,1fr))] gap-2">
        {week.staff.map((s) => (
          <li key={s.userId} className="flex items-center justify-between gap-3 rounded-(--radius-control) bg-surface px-3 py-2">
            <span>
              <span className="font-medium">{s.fullName}</span>
              {s.department && <span className="ml-2 text-caption text-ink-muted">{DEPARTMENT_SHORT[s.department]}</span>}
            </span>
            {s.overLimit
              ? <StatusPill label={`${hoursLabel(s.weeklyHours)} · quá ${week.maxWeeklyHours}`} tone="lane" />
              : <span className="tabular-nums text-ink-muted">{hoursLabel(s.weeklyHours)}</span>}
          </li>
        ))}
      </ul>
    </section>
  )
}
