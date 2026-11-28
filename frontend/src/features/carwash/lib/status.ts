import type { PillTone } from '@/components/cenvo/StatusPill'
import type { QueueTicket, WashStatus } from '@/lib/api/types'

export type BoardStatus = Exclude<WashStatus, 'Cancelled'>

export interface WashColumn {
  status: BoardStatus
  title: string
  /** Lane colour on the column title and card stripe (never the whole card). */
  stripe: string
}

/** Kanban lanes left → right. */
export const WASH_COLUMNS: WashColumn[] = [
  { status: 'Waiting', title: 'Chờ rửa', stripe: 'bg-lane' },
  { status: 'Washing', title: 'Đang rửa', stripe: 'bg-foam' },
  { status: 'Done', title: 'Xong — chờ giao', stripe: 'bg-go' },
  { status: 'Delivered', title: 'Đã giao hôm nay', stripe: 'bg-ink-muted' },
]

const STATUS: Record<WashStatus, { label: string; tone: PillTone }> = {
  Waiting: { label: 'Đang chờ', tone: 'lane' },
  Washing: { label: 'Đang rửa', tone: 'muted' },
  Done: { label: 'Đã rửa xong', tone: 'go' },
  Delivered: { label: 'Đã giao xe', tone: 'muted' },
  Cancelled: { label: 'Đã hủy', tone: 'stop' },
}

export function washStatusLabel(status: WashStatus) {
  return STATUS[status]
}

export function groupByStatus(tickets: QueueTicket[]): Record<BoardStatus, QueueTicket[]> {
  const groups: Record<BoardStatus, QueueTicket[]> = { Waiting: [], Washing: [], Done: [], Delivered: [] }
  for (const ticket of tickets) if (ticket.status !== 'Cancelled') groups[ticket.status].push(ticket)
  return groups
}

/** The one big button on a queue card. `open` = the ticket needs something only the detail sheet offers. */
export type CardAction =
  | { kind: 'step'; step: 'start' | 'complete' | 'deliver'; label: string }
  | { kind: 'pay'; label: string }
  | { kind: 'open'; label: string }
  | null

export function cardAction(ticket: QueueTicket, canTakePayment: boolean): CardAction {
  switch (ticket.status) {
    case 'Waiting':
      return ticket.staff.length > 0 ? { kind: 'step', step: 'start', label: 'Bắt đầu rửa' } : { kind: 'open', label: 'Gán người rửa' }
    case 'Washing':
      return { kind: 'step', step: 'complete', label: 'Rửa xong' }
    case 'Done':
      if (ticket.isPaid) return { kind: 'step', step: 'deliver', label: 'Giao xe' }
      return canTakePayment ? { kind: 'pay', label: 'Thu tiền' } : { kind: 'open', label: 'Chờ thu tiền' }
    default:
      return null
  }
}
