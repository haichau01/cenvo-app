import { memo } from 'react'
import { Money } from '@/components/cenvo/Money'
import { QuantityStepper } from '@/components/ui/QuantityStepper'
import { Button } from '@/components/ui/Button'
import type { OrderLine } from '@/lib/api/types'
import { PENDING_LINE_PREFIX, useRemoveLine, useUpdateLineQuantity } from '../api/orders'
import { kitchenStatusView } from '../lib/order'

interface OrderLineRowProps {
  orderId: string
  line: OrderLine
  onDiscount: (line: OrderLine) => void
}

export const OrderLineRow = memo(function OrderLineRow({ orderId, line, onDiscount }: OrderLineRowProps) {
  const update = useUpdateLineQuantity()
  const remove = useRemoveLine()
  const pending = line.id.startsWith(PENDING_LINE_PREFIX) || update.isPending || remove.isPending

  function handleQuantity(quantity: number) {
    if (quantity <= 0) remove.mutate({ orderId, lineId: line.id })
    else update.mutate({ orderId, lineId: line.id, quantity, note: line.note })
  }

  return (
    <li className="animate-line-in flex flex-col gap-2 border-b border-line px-5 py-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-semibold leading-snug">{line.name}</p>
          <LineDetails line={line} />
        </div>
        <Money value={line.amount} className="font-semibold" />
      </div>
      <div className="flex items-center justify-between gap-3">
        <KitchenBadge line={line} />
        <Button variant="ghost" className="ml-auto" disabled={pending} onClick={() => onDiscount(line)}
          aria-label={`Giảm giá ${line.name}`}>Giảm</Button>
        <QuantityStepper value={line.quantity} onChange={handleQuantity} label={line.name} disabled={pending} />
      </div>
    </li>
  )
})

function LineDetails({ line }: { line: OrderLine }) {
  const toppings = line.toppings.map((t) => t.name).join(', ')
  return (
    <>
      {toppings && <p className="text-caption text-ink-muted">+ {toppings}</p>}
      {line.note && <p className="text-caption text-ink-muted italic">“{line.note}”</p>}
      {line.discountAmount > 0 && (
        <p className="text-caption text-ink-muted">Giảm <Money value={line.discountAmount} /> · {line.discountReason}</p>
      )}
    </>
  )
}

function KitchenBadge({ line }: { line: OrderLine }) {
  const view = kitchenStatusView[line.kitchenStatus]
  return (
    <span className="inline-flex items-center gap-1.5 text-caption text-ink-muted">
      <span aria-hidden className={`size-2 rounded-full ${view.dot}`} />
      {view.label}
    </span>
  )
}
