import type { PillTone } from '@/components/cenvo/StatusPill'
import type { DueReminder, ReminderKind, ReminderStatus } from '@/lib/api/types'
import { formatDate } from '@/lib/format'

type DueReason = DueReminder['reason']

interface KindConfig {
  kind: ReminderKind
  /** Tab title. */
  label: string
  empty: string
}

/** Tab order on the reminders screen — same order the API returns. */
export const REMINDER_KINDS: KindConfig[] = [
  { kind: 'ReturnVisit', label: 'Lâu chưa quay lại', empty: 'Không có khách nào quá hạn quay lại.' },
  { kind: 'MembershipExpiring', label: 'Gói sắp hết hạn', empty: 'Không có gói nào sắp hết hạn mà còn lượt.' },
  { kind: 'MembershipLowUses', label: 'Gói sắp hết lượt', empty: 'Không có gói nào sắp hết lượt.' },
]

export const reminderKindLabel = (kind: ReminderKind): string =>
  REMINDER_KINDS.find((k) => k.kind === kind)?.label ?? kind

const DAY_MS = 24 * 60 * 60 * 1000

/** Why this customer is on the list, in one short line. */
export function reminderReason(reason: DueReason, now: number = Date.now()): string {
  if (reason.kind === 'ReturnVisit' && reason.lastWashAt) {
    const days = Math.floor((now - new Date(reason.lastWashAt).getTime()) / DAY_MS)
    return `Rửa lần cuối ${formatDate(reason.lastWashAt)} · ${days} ngày trước`
  }
  const uses = reason.remainingUses ?? 0
  const left = uses === 0 ? 'đã hết lượt' : `còn ${uses} lượt`
  return reason.expiresAt ? `${left} · hết hạn ${formatDate(reason.expiresAt)}` : left
}

export function reminderStatusLabel(status: ReminderStatus): { label: string; tone: PillTone } {
  if (status === 'Sent') return { label: 'Đã gửi', tone: 'go' }
  if (status === 'Failed') return { label: 'Gửi lỗi', tone: 'stop' }
  return { label: 'Đang gửi', tone: 'lane' }
}
