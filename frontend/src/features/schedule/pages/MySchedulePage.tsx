import { useState } from 'react'
import { Link } from 'react-router'
import { Wordmark } from '@/components/cenvo/Wordmark'
import { FormError } from '@/components/ui/FormError'
import { useSession } from '@/lib/auth/session'
import { vnDate } from '@/lib/format'
import { useMySchedule } from '../api/schedule'
import { addDays, dayLabel, shiftHours, weekDays, weekStartOf } from '../lib/week'

/** Every staff member, on their phone: their shifts this week and next, in every branch. One column, day by day. */
export function MySchedulePage() {
  const user = useSession()!.user
  const [today] = useState(() => vnDate(Date.now()))
  const monday = weekStartOf(today)
  const days = [...weekDays(monday), ...weekDays(addDays(monday, 7))]
  const shifts = useMySchedule(days[0], days[13])

  return (
    <div className="min-h-dvh bg-concrete">
      <header className="flex h-14 items-center gap-3 border-b border-line bg-surface px-4">
        <Wordmark />
        <Link to="/" className="ml-auto flex h-11 items-center rounded-(--radius-control) px-3 font-medium hover:bg-ink/5">Trang chủ</Link>
      </header>
      <main className="mx-auto flex max-w-md flex-col gap-4 px-4 py-6">
        <div>
          <h1 className="font-display text-screen font-semibold">Lịch của tôi</h1>
          <p className="text-ink-muted">{user.fullName} · tuần này và tuần sau</p>
        </div>
        <FormError message={shifts.error?.message} />
        {shifts.isPending && <div className="h-96 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
        {shifts.data && (
          <ol className="flex flex-col gap-2">
            {days.map((day) => {
              const label = dayLabel(day)
              const mine = shifts.data.filter((s) => s.date === day)
              const isToday = day === today
              return (
                <li key={day} className={`flex gap-4 rounded-(--radius-panel) bg-surface px-4 py-3 ${day < today ? 'opacity-60' : ''}
                  ${isToday ? 'border-l-4 border-foam' : ''}`}>
                  <div className="w-14 shrink-0">
                    <p className="font-display text-block font-semibold">{label.weekday}</p>
                    <p className="text-caption tabular-nums text-ink-muted">{isToday ? 'Hôm nay' : label.date}</p>
                  </div>
                  {mine.length === 0
                    ? <p className="self-center text-ink-muted">Nghỉ</p>
                    : (
                      <ul className="flex flex-col gap-1">
                        {mine.map((s) => (
                          <li key={s.id}>
                            <p className="font-semibold">{s.shiftName} <span className="tabular-nums">{shiftHours(s.startTime, s.endTime)}</span></p>
                            <p className="text-caption text-ink-muted">{s.branchName}</p>
                          </li>
                        ))}
                      </ul>
                    )}
                </li>
              )
            })}
          </ol>
        )}
      </main>
    </div>
  )
}
