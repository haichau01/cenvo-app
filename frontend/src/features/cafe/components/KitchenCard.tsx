import { memo } from 'react'
import { Button } from '@/components/ui/Button'
import type { KitchenTicket } from '@/lib/api/types'
import { formatDuration, formatQuantity, minutesSince } from '@/lib/format'
import { isLate, type KitchenColumn, previousStatus, ticketPlace } from '../lib/kitchen'

interface KitchenCardProps {
  ticket: KitchenTicket
  column: KitchenColumn
  now: number
  /** Arrived after the screen was opened → highlight once. */
  fresh: boolean
  onForward: (ticket: KitchenTicket) => void
  onBack: (ticket: KitchenTicket) => void
}

/** One drink: big name and quantity readable from arm's length, toppings and note, where it goes, how long it waited. */
export const KitchenCard = memo(function KitchenCard({ ticket, column, now, fresh, onForward, onBack }: KitchenCardProps) {
  const waited = ticket.sentAt ? minutesSince(ticket.sentAt, now) : 0
  const canGoBack = previousStatus(column.status) !== null

  return (
    <li className={`relative flex shrink-0 flex-col gap-3 overflow-hidden rounded-(--radius-panel) bg-surface p-4 pl-5
      ${fresh ? 'animate-ticket-in' : ''}`}>
      <span aria-hidden className={`absolute inset-y-0 left-0 w-1 ${column.stripe}`} />
      <div className="flex items-start justify-between gap-3">
        <p className="font-display text-xl font-semibold leading-tight text-ink">{ticket.name}</p>
        <p className="font-display text-2xl font-bold tabular-nums text-ink">×{formatQuantity(ticket.quantity)}</p>
      </div>
      <TicketDetails ticket={ticket} />
      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="font-semibold text-ink">{ticketPlace(ticket)} <span className="font-normal text-ink-muted">· {ticket.orderCode}</span></span>
        <WaitTime minutes={waited} late={isLate(column.status, waited)} />
      </div>
      <div className="flex gap-2">
        {canGoBack && (
          <Button variant="secondary" onClick={() => onBack(ticket)} aria-label={`Lùi lại: ${ticket.name}`}>Lùi</Button>
        )}
        <Button size="lg" className="flex-1" onClick={() => onForward(ticket)}>{column.action}</Button>
      </div>
    </li>
  )
})

function TicketDetails({ ticket }: { ticket: KitchenTicket }) {
  if (ticket.toppings.length === 0 && !ticket.note) return null
  return (
    <div className="flex flex-col gap-1">
      {ticket.toppings.length > 0 && <p className="text-ink">+ {ticket.toppings.join(', ')}</p>}
      {ticket.note && <p className="font-semibold text-ink">Ghi chú: {ticket.note}</p>}
    </div>
  )
}

function WaitTime({ minutes, late }: { minutes: number; late: boolean }) {
  if (late) return <span className="font-semibold text-stop tabular-nums">Chờ lâu · {formatDuration(minutes)}</span>
  return <span className="text-ink-muted tabular-nums">{formatDuration(minutes)}</span>
}
