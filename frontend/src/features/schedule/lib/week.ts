import type { Department } from '@/lib/api/types'

const WEEKDAYS = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']

const parse = (iso: string) => {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, d))
}
const format = (date: Date) => date.toISOString().slice(0, 10)

/** Calendar arithmetic on yyyy-MM-dd strings (no time zone involved). */
export const addDays = (iso: string, days: number) => format(new Date(parse(iso).getTime() + days * 86_400_000))

/** Monday of the week containing the day. */
export function weekStartOf(iso: string): string {
  return addDays(iso, -((parse(iso).getUTCDay() + 6) % 7))
}

export const weekDays = (monday: string) => Array.from({ length: 7 }, (_, i) => addDays(monday, i))

/** "2026-12-07" → { weekday: "T2", date: "07/12" } */
export function dayLabel(iso: string): { weekday: string; date: string } {
  return { weekday: WEEKDAYS[parse(iso).getUTCDay()], date: `${iso.slice(8, 10)}/${iso.slice(5, 7)}` }
}

/** "06:00:00" → "06:00" */
export const timeLabel = (time: string) => time.slice(0, 5)

/** "Sáng 06:00–12:00", overnight shifts marked so nobody misreads the end. */
export function shiftHours(start: string, end: string): string {
  const overnight = end <= start
  return `${timeLabel(start)}–${timeLabel(end)}${overnight ? ' (hôm sau)' : ''}`
}

/** Hours with at most one decimal, Vietnamese comma: 7,5 giờ. */
export const hoursLabel = (hours: number) => `${Number.isInteger(hours) ? hours : hours.toFixed(1).replace('.', ',')} giờ`

export const DEPARTMENT_SHORT: Record<Department, string> = { Cafe: 'Cà phê', CarWash: 'Rửa xe', General: 'Chung' }
