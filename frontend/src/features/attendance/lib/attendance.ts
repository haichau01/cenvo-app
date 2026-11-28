import type { PillTone } from '@/components/cenvo/StatusPill'
import type { AttendanceRow, PunchMethod } from '@/lib/api/types'

/** Vietnam has no daylight saving: local time is always UTC+7. */
const VN_OFFSET_MS = 7 * 60 * 60 * 1000

/** ISO instant → value of <input type="datetime-local"> in Vietnam time ("2026-11-28T08:05"). */
export function toVnLocalInput(iso: string): string {
  return new Date(new Date(iso).getTime() + VN_OFFSET_MS).toISOString().slice(0, 16)
}

/** <input type="datetime-local"> value typed in Vietnam time → ISO instant (UTC). */
export function fromVnLocalInput(value: string): string {
  return new Date(Date.parse(`${value}:00Z`) - VN_OFFSET_MS).toISOString()
}

/** "8 giờ 05" / "45 phút" — hours worked in tables and tiles. */
export function formatMinutes(minutes: number): string {
  if (minutes < 60) return `${minutes} phút`
  const rest = minutes % 60
  return rest === 0 ? `${minutes / 60} giờ` : `${Math.floor(minutes / 60)} giờ ${String(rest).padStart(2, '0')}`
}

/** The link the QR code on the shop POS opens on a phone (its own camera app is enough). */
export function punchUrl(origin: string, code: string): string {
  return `${origin}/cham-cong/quet?ma=${encodeURIComponent(code)}`
}

/** Milliseconds until the kiosk should fetch a new code: a few seconds before the shown one expires, at least 1 s. */
export function refreshDelay(expiresAt: string, now: number): number {
  return Math.max(1_000, new Date(expiresAt).getTime() - now - 5_000)
}

/** "Tháng 11/2026" */
export function monthTitle(month: string): string {
  const [year, m] = month.split('-')
  return `Tháng ${Number(m)}/${year}`
}

/** First day of the month `offset` months away from the month of `date` (yyyy-MM-dd). */
export function shiftMonth(date: string, offset: number): string {
  const [year, month] = date.split('-').map(Number)
  const d = new Date(Date.UTC(year, month - 1 + offset, 1))
  return d.toISOString().slice(0, 10)
}

export interface RowStatus { label: string; tone: PillTone }

/** One status per punch, the most urgent first: what the manager has to do about it. */
export function rowStatus(row: Pick<AttendanceRow, 'approval' | 'forgotten' | 'checkOutAt'>): RowStatus {
  if (row.approval === 'Rejected') return { label: 'Không duyệt', tone: 'stop' }
  if (row.forgotten) return { label: 'Quên chấm ra', tone: 'stop' }
  if (row.approval === 'Pending') return { label: 'Chờ duyệt', tone: 'lane' }
  if (!row.checkOutAt) return { label: 'Đang làm', tone: 'muted' }
  return { label: 'Đã tính công', tone: 'go' }
}

/** Overtime waiting for a decision. */
export const needsOvertimeReview = (row: Pick<AttendanceRow, 'proposedOvertimeMinutes' | 'overtimeApprovedMinutes'>) =>
  row.proposedOvertimeMinutes > 0 && row.overtimeApprovedMinutes == null

const METHOD_LABELS: Record<PunchMethod, string> = { Qr: 'Quét mã', Pin: 'PIN ở quán', Manual: 'Quản lý nhập' }
export const methodLabel = (method: PunchMethod) => METHOD_LABELS[method]

/** PINs are 4–6 digits (the backend checks the same). */
export const isValidPin = (pin: string) => /^\d{4,6}$/.test(pin)
