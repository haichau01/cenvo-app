import { vnDate } from '@/lib/format'

export interface Period {
  /** yyyy-MM-dd, Vietnam calendar, both ends included. */
  from: string
  to: string
}

export type PresetId = 'today' | 'yesterday' | 'last7' | 'thisMonth' | 'lastMonth'

export interface Preset {
  id: PresetId
  label: string
  period: (today: string) => Period
}

/** Shift a yyyy-MM-dd date by whole days (UTC arithmetic on a date-only value — no time zone drift). */
export function addDays(date: string, days: number): string {
  const value = new Date(`${date}T00:00:00Z`)
  value.setUTCDate(value.getUTCDate() + days)
  return value.toISOString().slice(0, 10)
}

function startOfMonth(date: string): string {
  return `${date.slice(0, 7)}-01`
}

export const PRESETS: Preset[] = [
  { id: 'today', label: 'Hôm nay', period: (today) => ({ from: today, to: today }) },
  { id: 'yesterday', label: 'Hôm qua', period: (today) => ({ from: addDays(today, -1), to: addDays(today, -1) }) },
  { id: 'last7', label: '7 ngày', period: (today) => ({ from: addDays(today, -6), to: today }) },
  { id: 'thisMonth', label: 'Tháng này', period: (today) => ({ from: startOfMonth(today), to: today }) },
  {
    id: 'lastMonth', label: 'Tháng trước',
    period: (today) => {
      const lastDay = addDays(startOfMonth(today), -1)
      return { from: startOfMonth(lastDay), to: lastDay }
    },
  },
]

export function todayInVietnam(now: number = Date.now()): string {
  return vnDate(now)
}

/** The preset matching a period, so the right button shows as selected after manual date edits. */
export function matchingPreset(period: Period, today: string): PresetId | null {
  return PRESETS.find((preset) => {
    const candidate = preset.period(today)
    return candidate.from === period.from && candidate.to === period.to
  })?.id ?? null
}

/** Every date from `from` to `to`, both included. */
export function daysOf(period: Period): string[] {
  const days: string[] = []
  for (let day = period.from; day <= period.to; day = addDays(day, 1)) days.push(day)
  return days
}
