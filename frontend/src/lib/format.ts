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
