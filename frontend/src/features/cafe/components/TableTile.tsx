import { memo } from 'react'
import { Money } from '@/components/cenvo/Money'
import type { OrderDto, TableDto } from '@/lib/api/types'
import { formatDuration, minutesSince } from '@/lib/format'

const tileBase = `flex h-24 flex-col justify-between rounded-(--radius-panel) border-2 p-3 text-left
  transition-colors duration-100 active:brightness-95`

function tileState(occupied: boolean, selected: boolean): string {
  if (selected) return 'border-(--module) bg-surface'
  if (occupied) return 'border-transparent bg-surface border-l-(--module) border-l-4'
  return 'border-dashed border-line bg-concrete text-ink-muted'
}

interface TableTileProps {
  table: TableDto
  selected: boolean
  onSelect: (table: TableDto) => void
}

/** Free table = dashed outline; occupied = white card with the module stripe, order total and elapsed time. */
export const TableTile = memo(function TableTile({ table, selected, onSelect }: TableTileProps) {
  const occupied = table.openOrderId != null
  return (
    <button type="button" onClick={() => onSelect(table)} aria-pressed={selected}
      className={`${tileBase} ${tileState(occupied, selected)}`}>
      <span className="font-display text-xl font-semibold text-ink">{table.name}</span>
      {occupied ? <OccupiedInfo total={table.openOrderTotal ?? 0} openedAt={table.openedAt} /> : <span className="text-caption">Trống</span>}
    </button>
  )
})

function OccupiedInfo({ total, openedAt }: { total: number; openedAt?: string | null }) {
  return (
    <span className="flex items-baseline justify-between gap-2 text-sm">
      <Money value={total} className="font-semibold text-ink" />
      {openedAt && <span className="text-caption text-ink-muted tabular-nums">{formatDuration(minutesSince(openedAt))}</span>}
    </span>
  )
}

interface TakeAwayTileProps {
  order: OrderDto
  selected: boolean
  onSelect: (orderId: string) => void
}

export const TakeAwayTile = memo(function TakeAwayTile({ order, selected, onSelect }: TakeAwayTileProps) {
  return (
    <button type="button" onClick={() => onSelect(order.id)} aria-pressed={selected}
      className={`${tileBase} ${tileState(true, selected)}`}>
      <span className="font-display text-lg font-semibold text-ink">{order.customerName || order.code}</span>
      <OccupiedInfo total={order.total} openedAt={order.createdAt} />
    </button>
  )
})
