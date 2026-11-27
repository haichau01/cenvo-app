import type { KitchenTicket } from '@/lib/api/types'
import { useNow } from '@/lib/useNow'
import { groupByStatus, KITCHEN_COLUMNS } from '../lib/kitchen'
import { KitchenCard } from './KitchenCard'

interface KitchenBoardProps {
  tickets: KitchenTicket[]
  freshIds: ReadonlySet<string>
  onForward: (ticket: KitchenTicket) => void
  onBack: (ticket: KitchenTicket) => void
}

/** Three columns left → right: waiting, being made, ready to serve. */
export function KitchenBoard({ tickets, freshIds, onForward, onBack }: KitchenBoardProps) {
  const now = useNow()
  const groups = groupByStatus(tickets)

  return (
    <div className="grid min-h-0 flex-1 grid-cols-3 gap-4 p-4">
      {KITCHEN_COLUMNS.map((column) => (
        <section key={column.status} aria-labelledby={`col-${column.status}`} className="flex min-h-0 flex-col">
          <h2 id={`col-${column.status}`} className="mb-3 flex items-center gap-2 font-display text-block font-semibold text-ink">
            <span aria-hidden className={`size-3 rounded-full ${column.stripe}`} />
            {column.title}
            <span className="tabular-nums text-ink-muted">{groups[column.status].length}</span>
          </h2>
          <ul className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto pb-4">
            {groups[column.status].map((ticket) => (
              <KitchenCard key={ticket.lineId} ticket={ticket} column={column} now={now}
                fresh={freshIds.has(ticket.lineId)} onForward={onForward} onBack={onBack} />
            ))}
            {groups[column.status].length === 0 && <li className="py-6 text-center text-ink-muted">Không có món</li>}
          </ul>
        </section>
      ))}
    </div>
  )
}
