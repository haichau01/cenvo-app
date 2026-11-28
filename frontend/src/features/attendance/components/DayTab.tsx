import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import type { UnpunchedShift } from '@/lib/api/types'
import { formatTime } from '@/lib/format'
import { useAttendanceDay } from '../api/attendance'
import { ManualDialog } from './ManualDialog'
import { RecordTable } from './RecordTable'

/** One day of the branch: who punched, and the planned shifts nobody punched for. */
export function DayTab({ today }: { today: string }) {
  const [date, setDate] = useState(today)
  const day = useAttendanceDay(date)
  const [entering, setEntering] = useState<UnpunchedShift | null>(null)

  return (
    <div className="flex flex-col gap-4">
      <TextField label="Ngày" type="date" max={today} value={date} onChange={(e) => setDate(e.target.value || today)} className="w-56" />
      <FormError message={day.error?.message} />
      {day.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {day.data && day.data.records.length > 0 && <RecordTable rows={day.data.records} />}
      {day.data && day.data.records.length === 0 && (
        <EmptyState title="Chưa có ai chấm công ngày này" description="Nhân viên quét mã trên máy chấm công ở quán để vào ca." />
      )}
      {day.data && day.data.unpunched.length > 0 && (
        <section className="flex flex-col gap-2">
          <h2 className="font-display text-block font-semibold">Ca đã xếp chưa chấm công</h2>
          <ul className="divide-y divide-line rounded-(--radius-panel) bg-surface">
            {day.data.unpunched.map((shift) => (
              <li key={shift.assignmentId} className="flex h-14 items-center gap-4 px-4">
                <span className="w-56 font-medium">{shift.employeeName}</span>
                <span className="tabular-nums">{shift.shiftName} · {formatTime(shift.startsAt)} – {formatTime(shift.endsAt)}</span>
                <StatusPill label={shift.isOver ? 'Vắng' : 'Chưa vào ca'} tone={shift.isOver ? 'stop' : 'muted'} />
                <Button variant="ghost" className="ml-auto" onClick={() => setEntering(shift)}>Nhập giờ</Button>
              </li>
            ))}
          </ul>
        </section>
      )}
      {entering && <ManualDialog shift={entering} date={date} onClose={() => setEntering(null)} />}
    </div>
  )
}
