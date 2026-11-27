import type { KitchenStatus, KitchenTicket } from '@/lib/api/types'

/** Statuses that are still on the kitchen screen (Served leaves the queue). */
export type QueueStatus = 'Sent' | 'Preparing' | 'Done'

export interface KitchenColumn {
  status: QueueStatus
  title: string
  /** Label of the main button on a card in this column. */
  action: string
  /** Token class for the column stripe. */
  stripe: string
}

/** Left to right = the order a drink moves through the bar. */
export const KITCHEN_COLUMNS: KitchenColumn[] = [
  { status: 'Sent', title: 'Chờ pha', action: 'Bắt đầu pha', stripe: 'bg-lane' },
  { status: 'Preparing', title: 'Đang pha', action: 'Xong', stripe: 'bg-(--module)' },
  { status: 'Done', title: 'Chờ mang ra', action: 'Đã mang ra', stripe: 'bg-go' },
]

const NEXT: Record<QueueStatus, KitchenStatus> = { Sent: 'Preparing', Preparing: 'Done', Done: 'Served' }
const PREVIOUS: Record<QueueStatus, QueueStatus | null> = { Sent: null, Preparing: 'Sent', Done: 'Preparing' }

/** One step forward — mirrors the backend KitchenFlow rule. */
export function nextStatus(status: QueueStatus): KitchenStatus {
  return NEXT[status]
}

/** One step back to undo a mistaken tap; null when already at the start. */
export function previousStatus(status: QueueStatus): QueueStatus | null {
  return PREVIOUS[status]
}

export function isQueueStatus(status: KitchenStatus): status is QueueStatus {
  return status === 'Sent' || status === 'Preparing' || status === 'Done'
}

/** Tickets per column, oldest first (the server already sorts by send time). */
export function groupByStatus(tickets: KitchenTicket[]): Record<QueueStatus, KitchenTicket[]> {
  const groups: Record<QueueStatus, KitchenTicket[]> = { Sent: [], Preparing: [], Done: [] }
  for (const ticket of tickets) {
    if (isQueueStatus(ticket.status)) groups[ticket.status].push(ticket)
  }
  return groups
}

export function ticketPlace(ticket: Pick<KitchenTicket, 'type' | 'tableName'>): string {
  return ticket.type === 'TakeAway' ? 'Mang đi' : (ticket.tableName ?? 'Tại bàn')
}

/** A drink not started after this many minutes is flagged so the bar catches up. */
export const LATE_AFTER_MINUTES = 10

export function isLate(status: QueueStatus, waitedMinutes: number): boolean {
  return status !== 'Done' && waitedMinutes >= LATE_AFTER_MINUTES
}
