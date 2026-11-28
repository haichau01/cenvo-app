import { StatusPill } from '@/components/cenvo/StatusPill'
import { toast } from '@/components/ui/toastStore'
import type { ScheduleWeek } from '@/lib/api/types'
import { useAssign, useUnassign } from '../api/schedule'
import { DEPARTMENT_SHORT, hoursLabel } from '../lib/week'

interface ShiftCellProps {
  day: string
  shift: ScheduleWeek['templates'][number]
  assignments: ScheduleWeek['assignments']
  shortfalls: ScheduleWeek['shortfalls']
  staff: ScheduleWeek['staff']
  /** Days already gone are read-only (attendance compares against them). */
  locked: boolean
}

/** One shift on one day: who is on it, who can still be added, and which department is short (upcoming days only — `lane` is for what can still be fixed). */
export function ShiftCell({ day, shift, assignments, shortfalls, staff, locked }: ShiftCellProps) {
  const assign = useAssign()
  const unassign = useUnassign()
  const available = staff.filter((s) => assignments.every((a) => a.employeeId !== s.userId))

  function handleAdd(employeeId: string) {
    if (!employeeId) return
    const name = staff.find((s) => s.userId === employeeId)?.fullName ?? ''
    assign.mutate({ date: day, shiftTemplateId: shift.id, employeeId }, {
      onSuccess: (result) => toast.success(result.overLimit
        ? `Đã xếp ${name} — tuần này ${hoursLabel(result.weeklyHours)}, quá giới hạn`
        : `Đã xếp ${name} vào ca ${shift.name}`),
    })
  }

  return (
    <td className={`px-2 py-2 ${locked ? 'bg-concrete/50' : ''}`}>
      <ul className="flex flex-col gap-1">
        {assignments.map((a) => (
          <li key={a.id} className="flex items-center justify-between gap-1 rounded-(--radius-control) bg-concrete px-2 py-1">
            <span className="truncate" title={a.employeeName}>{a.employeeName}</span>
            {!locked && (
              <button type="button" aria-label={`Bỏ ${a.employeeName} khỏi ca ${shift.name}`} disabled={unassign.isPending}
                onClick={() => unassign.mutate(a.id)}
                className="flex size-8 shrink-0 items-center justify-center rounded-(--radius-control) text-ink-muted hover:bg-ink/10">
                ×
              </button>
            )}
          </li>
        ))}
      </ul>
      {!locked && shortfalls.map((s) => (
        <div key={s.department} className="mt-1">
          <StatusPill label={`Thiếu ${s.required - s.assigned} ${DEPARTMENT_SHORT[s.department]}`} tone="lane" />
        </div>
      ))}
      {!locked && available.length > 0 && (
        <select aria-label={`Thêm người vào ca ${shift.name} ngày ${day}`} value="" disabled={assign.isPending}
          onChange={(event) => handleAdd(event.target.value)}
          className="mt-1 h-9 w-full rounded-(--radius-control) border border-dashed border-line bg-transparent px-2 text-ink-muted
            focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam">
          <option value="">+ Thêm người</option>
          {available.map((s) => <option key={s.userId} value={s.userId}>{s.fullName}</option>)}
        </select>
      )}
      {(assign.error || unassign.error) && (
        <p role="alert" className="mt-1 text-caption text-stop">{assign.error?.message ?? unassign.error?.message}</p>
      )}
    </td>
  )
}
