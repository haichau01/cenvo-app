import type { QueueTicket } from '@/lib/api/types'
import { useNow } from '@/lib/useNow'
import { estimatedWait } from '../lib/queue'
import { groupByStatus, WASH_COLUMNS, type CardAction } from '../lib/status'
import { QueueCard } from './QueueCard'

/** Washing bays working in parallel — used only for the waiting-time hint. */
const WASH_BAYS = 2

interface QueueBoardProps {
  tickets: QueueTicket[]
  freshIds: ReadonlySet<string>
  canTakePayment: boolean
  onOpen: (ticket: QueueTicket) => void
  onAction: (ticket: QueueTicket, action: NonNullable<CardAction>) => void
}

/** Four lanes left → right; cards stacked in arrival order. */
export function QueueBoard({ tickets, freshIds, canTakePayment, onOpen, onAction }: QueueBoardProps) {
  const now = useNow()
  const groups = groupByStatus(tickets)

  return (
    <div className="grid min-h-0 flex-1 grid-cols-4 gap-4 p-4">
      {WASH_COLUMNS.map((column) => (
        <section key={column.status} aria-labelledby={`lane-${column.status}`} className="flex min-h-0 flex-col">
          <h2 id={`lane-${column.status}`} className="mb-3 flex items-center gap-2 font-display text-block font-semibold text-ink">
            <span aria-hidden className={`size-3 rounded-full ${column.stripe}`} />
            {column.title}
            <span className="tabular-nums text-ink-muted">{groups[column.status].length}</span>
          </h2>
          <ul className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto pb-4">
            {groups[column.status].map((ticket) => (
              <QueueCard key={ticket.id} ticket={ticket} column={column} now={now} canTakePayment={canTakePayment}
                waitEstimate={column.status === 'Waiting' ? estimatedWait(tickets, ticket.id, now, WASH_BAYS) : null}
                fresh={freshIds.has(ticket.id)} onOpen={onOpen} onAction={onAction} />
            ))}
            {groups[column.status].length === 0 && <li className="py-6 text-center text-ink-muted">Không có xe</li>}
          </ul>
        </section>
      ))}
    </div>
  )
}
