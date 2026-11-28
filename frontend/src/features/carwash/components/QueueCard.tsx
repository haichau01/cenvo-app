import { memo } from 'react'
import { Plate } from '@/components/cenvo/Plate'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import type { QueueTicket } from '@/lib/api/types'
import { formatDuration } from '@/lib/format'
import { cardClock } from '../lib/queue'
import { cardAction, type CardAction, type WashColumn } from '../lib/status'
import { vehicleLabel } from '../lib/vehicle'

interface QueueCardProps {
  ticket: QueueTicket
  column: WashColumn
  now: number
  canTakePayment: boolean
  /** Minutes until a waiting car is likely to start (null for other lanes). */
  waitEstimate: number | null
  fresh: boolean
  onOpen: (ticket: QueueTicket) => void
  onAction: (ticket: QueueTicket, action: NonNullable<CardAction>) => void
}

/** One car: plate readable from across the bay, what to do to it, who does it, how long it has been there. */
export const QueueCard = memo(function QueueCard(props: QueueCardProps) {
  const { ticket, column, now, canTakePayment, waitEstimate, fresh, onOpen, onAction } = props
  const action = cardAction(ticket, canTakePayment)
  const clock = cardClock(ticket, now)

  return (
    <li className={`relative flex shrink-0 flex-col overflow-hidden rounded-(--radius-panel) bg-surface ${fresh ? 'animate-ticket-in' : ''}`}>
      <span aria-hidden className={`absolute inset-y-0 left-0 w-1 ${column.stripe}`} />
      <button type="button" onClick={() => onOpen(ticket)} aria-label={`Mở phiếu ${ticket.code}`}
        className="flex flex-col gap-2 p-4 pl-5 text-left active:bg-ink/5">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <Plate value={ticket.plate} size="lg" />
          {ticket.isPaid && <StatusPill label="Đã thu tiền" tone="go" />}
        </div>
        <p className="text-sm text-ink-muted">
          {vehicleLabel(ticket.vehicleType)}{ticket.vehicleDescription && ` · ${ticket.vehicleDescription}`}
          {ticket.customerName && ` · ${ticket.customerName}`}
        </p>
        <p className="font-semibold text-ink">{ticket.services.join(' + ')}</p>
        <div className="flex items-center justify-between gap-2 text-sm">
          <span className="text-ink">{ticket.staff.length > 0 ? ticket.staff.join(', ') : 'Chưa gán người rửa'}</span>
          <span className={`tabular-nums ${clock.late ? 'font-semibold text-stop' : 'text-ink-muted'}`}>
            {clock.label} {formatDuration(clock.minutes)}
          </span>
        </div>
        {waitEstimate !== null && (
          <p className="text-sm text-ink-muted tabular-nums">Dự kiến bắt đầu sau ~{formatDuration(waitEstimate)}</p>
        )}
      </button>
      {action && (
        <div className="px-4 pb-4 pl-5">
          <Button size="lg" className="w-full" variant={action.kind === 'open' ? 'secondary' : 'primary'}
            onClick={() => onAction(ticket, action)}>
            {action.label}
          </Button>
        </div>
      )}
    </li>
  )
})
