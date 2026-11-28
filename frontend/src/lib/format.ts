const VN_TIME_ZONE = 'Asia/Ho_Chi_Minh'

const vnd = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 })
const time = new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: VN_TIME_ZONE })
const quantity = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 3 })

const MINUS = '−'

/** "85.000" — the number part of an amount; the ₫ sign is rendered separately by <Money>. */
export function formatVndNumber(value: number): string {
  const text = vnd.format(Math.abs(Math.round(value)))
  return value < 0 ? `${MINUS}${text}` : text
}

/** "85.000 ₫" with a narrow no-break space — for plain-text places (toasts, aria labels). */
export function formatVnd(value: number): string {
  return `${formatVndNumber(value)} ₫`
}

/** "14:05" in Vietnam time. */
export function formatTime(iso: string): string {
  return time.format(new Date(iso))
}

export function formatQuantity(value: number): string {
  return quantity.format(value)
}

/** Whole minutes elapsed since `iso`, never negative (device clocks drift slightly). */
export function minutesSince(iso: string, now: number = Date.now()): number {
  return Math.max(0, Math.floor((now - new Date(iso).getTime()) / 60_000))
}

/** "12 phút", "1 giờ 05 phút". */
export function formatDuration(minutes: number): string {
  if (minutes < 60) return `${minutes} phút`
  const hours = Math.floor(minutes / 60)
  return `${hours} giờ ${String(minutes % 60).padStart(2, '0')} phút`
}

const vnDateFormat = new Intl.DateTimeFormat('en-CA', { timeZone: VN_TIME_ZONE }) // en-CA formats as yyyy-MM-dd

/** Calendar date in Vietnam as yyyy-MM-dd (the format of <input type="date"> and the API's DateOnly). */
export function vnDate(value: string | number | Date): string {
  return vnDateFormat.format(new Date(value))
}

const compact = new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 })

/** "1,3 Tr", "850 N" — short amounts for chart axes; full amounts go in tooltips and tables. */
export function formatCompact(value: number): string {
  return compact.format(value)
}

const dateParts = new Intl.DateTimeFormat('vi-VN', {
  timeZone: VN_TIME_ZONE, day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
})

/** Parts in Vietnam time; assembled by hand because Intl's vi-VN separators change with the chosen fields ("27-11" vs "27/11"). */
function vnParts(iso: string | number | Date) {
  const parts = Object.fromEntries(dateParts.formatToParts(new Date(iso)).map((p) => [p.type, p.value]))
  return { day: parts.day, month: parts.month, year: parts.year, time: `${parts.hour}:${parts.minute}` }
}

/** "26/11/2026" */
export function formatDate(iso: string | number | Date): string {
  const p = vnParts(iso)
  return `${p.day}/${p.month}/${p.year}`
}

/** "14:05 26/11/2026" */
export function formatDateTime(iso: string | number | Date): string {
  const p = vnParts(iso)
  return `${p.time} ${p.day}/${p.month}/${p.year}`
}

/** "14:05 26/11" — inside a period that already shows the year. */
export function formatShortDateTime(iso: string | number | Date): string {
  const p = vnParts(iso)
  return `${p.time} ${p.day}/${p.month}`
}
