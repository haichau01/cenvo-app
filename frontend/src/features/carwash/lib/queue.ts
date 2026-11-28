import type { QueueTicket } from '@/lib/api/types'
import { minutesSince } from '@/lib/format'

/** What the card's clock shows, depending on where the vehicle is. */
export interface CardClock {
  label: string
  minutes: number
  /** Washing longer than planned → show in `stop`. */
  late: boolean
}

export function cardClock(ticket: QueueTicket, now: number): CardClock {
  if (ticket.status === 'Washing' && ticket.startedAt) {
    const minutes = minutesSince(ticket.startedAt, now)
    return { label: 'Đang rửa', minutes, late: minutes > ticket.estimatedMinutes }
  }
  if (ticket.status === 'Done' && ticket.completedAt) {
    return { label: 'Chờ giao', minutes: minutesSince(ticket.completedAt, now), late: false }
  }
  if (ticket.status === 'Delivered' && ticket.deliveredAt && ticket.completedAt) {
    return { label: 'Giao sau', minutes: minutesSince(ticket.completedAt, Date.parse(ticket.deliveredAt)), late: false }
  }
  return { label: 'Đã chờ', minutes: minutesSince(ticket.createdAt, now), late: false }
}

/**
 * Rough wait before a waiting vehicle starts: the remaining work of vehicles being washed plus the work of
 * those ahead in the queue, shared between `bays` washing bays. Display hint only.
 */
export function estimatedWait(tickets: QueueTicket[], ticketId: string, now: number, bays: number): number {
  const remainingWashing = tickets
    .filter((ticket) => ticket.status === 'Washing' && ticket.startedAt)
    .reduce((sum, ticket) => sum + Math.max(0, ticket.estimatedMinutes - minutesSince(ticket.startedAt!, now)), 0)
  const waiting = tickets.filter((ticket) => ticket.status === 'Waiting')
  const ahead = waiting.slice(0, Math.max(0, waiting.findIndex((ticket) => ticket.id === ticketId)))
  const aheadMinutes = ahead.reduce((sum, ticket) => sum + ticket.estimatedMinutes, 0)
  return Math.ceil((remainingWashing + aheadMinutes) / Math.max(1, bays))
}
