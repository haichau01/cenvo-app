import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import type { OrderDto, TableDto } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import { cafeKeys } from '../api/keys'
import { useMoveOrder } from '../api/orders'
import { useFloorPlan } from '../api/setup'
import { moveSummary, type MoveDestination } from '../lib/move'

interface MoveDialogProps {
  order: OrderDto | null
  onClose: () => void
  /** The bill that received the items (the current one, a merged table or a new bill). */
  onMoved: (orderId: string) => void
}

/** Move the whole bill to another table, merge it into an occupied one, or split some items off. */
export function MoveDialog({ order, onClose, onMoved }: MoveDialogProps) {
  return (
    <Dialog open={order !== null} onClose={onClose} title={order ? `Chuyển / gộp ${order.code}` : ''}
      className="w-[min(46rem,calc(100vw-2rem))]">
      {order && <MoveForm key={order.id} order={order} onMoved={onMoved} />}
    </Dialog>
  )
}

function MoveForm({ order, onMoved }: { order: OrderDto; onMoved: (orderId: string) => void }) {
  const queryClient = useQueryClient()
  const areas = useFloorPlan().data ?? []
  const move = useMoveOrder()
  const [selected, setSelected] = useState<Set<string>>(() => new Set(order.lines.map((line) => line.id)))
  const [destination, setDestination] = useState<MoveDestination | null>(null)
  const allLines = selected.size === order.lines.length

  function toggleLine(lineId: string) {
    const next = new Set(selected)
    if (next.has(lineId)) next.delete(lineId)
    else next.add(lineId)
    setSelected(next)
  }

  function handleMove() {
    if (!destination) return
    const toTableId = destination.kind === 'table' ? destination.table.id : null
    move.mutate({ orderId: order.id, lineIds: allLines ? null : [...selected], toTableId }, {
      onSuccess: (target) => {
        toast.success(`Đã chuyển sang ${target.tableName ?? `hóa đơn mang đi ${target.code}`}`)
        void queryClient.invalidateQueries({ queryKey: cafeKeys.orders }) // the source bill changed too
        onMoved(target.id)
      },
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <fieldset className="flex flex-col gap-1">
        <legend className="mb-2 font-display text-lg font-semibold">Món chuyển đi</legend>
        {order.lines.map((line) => (
          <label key={line.id} className="flex h-12 items-center gap-3 rounded-(--radius-control) px-2 hover:bg-concrete">
            <input type="checkbox" className="size-5 accent-(--module)" checked={selected.has(line.id)} onChange={() => toggleLine(line.id)} />
            <span className="flex-1">{line.name} <span className="text-ink-muted tabular-nums">×{formatQuantity(line.quantity)}</span></span>
            <Money value={line.amount} />
          </label>
        ))}
      </fieldset>

      <fieldset className="flex flex-col gap-3">
        <legend className="mb-2 font-display text-lg font-semibold">Chuyển đến</legend>
        {areas.map((area) => (
          <div key={area.id} className="flex flex-col gap-2">
            <p className="text-caption text-ink-muted">{area.name}</p>
            <div className="grid grid-cols-4 gap-2">
              {area.tables.filter((table) => table.openOrderId !== order.id).map((table) => (
                <TableChoice key={table.id} table={table} chosen={destination?.kind === 'table' && destination.table.id === table.id}
                  onChoose={() => setDestination({ kind: 'table', table })} />
              ))}
            </div>
          </div>
        ))}
        <button type="button" aria-pressed={destination?.kind === 'takeAway'} onClick={() => setDestination({ kind: 'takeAway' })}
          className={`h-12 rounded-(--radius-control) border-2 font-semibold
            ${destination?.kind === 'takeAway' ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface'}`}>
          Hóa đơn mang đi mới
        </button>
      </fieldset>

      {destination && <p className="font-medium">{moveSummary(destination, allLines, selected.size)}</p>}
      <FormError message={move.error?.message} />
      <Button size="lg" onClick={handleMove} busy={move.isPending} disabled={!destination || selected.size === 0}>Chuyển</Button>
    </div>
  )
}

function TableChoice({ table, chosen, onChoose }: { table: TableDto; chosen: boolean; onChoose: () => void }) {
  const occupied = table.openOrderId != null
  return (
    <button type="button" aria-pressed={chosen} onClick={onChoose}
      className={`flex h-16 flex-col justify-center rounded-(--radius-control) border-2 px-3 text-left
        ${chosen ? 'border-(--module) bg-(--module)/10' : 'border-line bg-surface'}`}>
      <span className="font-display text-lg font-semibold">{table.name}</span>
      <span className="text-caption text-ink-muted">{occupied ? 'Có khách — gộp' : 'Trống'}</span>
    </button>
  )
}
