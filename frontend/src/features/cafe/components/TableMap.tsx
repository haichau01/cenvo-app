import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { OrderDto, TableDto } from '@/lib/api/types'
import { useFloorPlan } from '../api/setup'
import { useOpenOrders } from '../api/orders'
import { TableTile, TakeAwayTile } from './TableTile'

interface TableMapProps {
  selectedOrderId: string | null
  onTable: (table: TableDto) => void
  onOrder: (orderId: string) => void
  onNewTakeAway: () => void
  creating: boolean
}

/** Floor plan: tables grouped by area (occupied ones show total and waiting time) + open take-away orders. */
export function TableMap({ selectedOrderId, onTable, onOrder, onNewTakeAway, creating }: TableMapProps) {
  const floorPlan = useFloorPlan()
  const openOrders = useOpenOrders()
  const takeAways = (openOrders.data ?? []).filter((order) => order.type === 'TakeAway')

  if (floorPlan.isPending) return <TableMapSkeleton />
  if (floorPlan.isError) return <EmptyState title="Không tải được sơ đồ bàn" description={floorPlan.error.message}
    action={<Button variant="secondary" onClick={() => floorPlan.refetch()}>Tải lại</Button>} />

  return (
    <div className="flex flex-col gap-8 p-6">
      <TakeAwaySection orders={takeAways} selectedOrderId={selectedOrderId} onOrder={onOrder}
        onNew={onNewTakeAway} creating={creating} />
      {floorPlan.data.map((area) => (
        <section key={area.id} aria-labelledby={`area-${area.id}`}>
          <h2 id={`area-${area.id}`} className="mb-3 font-display text-block font-semibold">{area.name}</h2>
          <div className="grid grid-cols-[repeat(auto-fill,minmax(8.5rem,1fr))] gap-3">
            {area.tables.map((table) => (
              <TableTile key={table.id} table={table} selected={table.openOrderId != null && table.openOrderId === selectedOrderId}
                onSelect={onTable} />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

interface TakeAwaySectionProps {
  orders: OrderDto[]
  selectedOrderId: string | null
  onOrder: (orderId: string) => void
  onNew: () => void
  creating: boolean
}

function TakeAwaySection({ orders, selectedOrderId, onOrder, onNew, creating }: TakeAwaySectionProps) {
  return (
    <section aria-labelledby="take-away">
      <div className="mb-3 flex items-center justify-between gap-4">
        <h2 id="take-away" className="font-display text-block font-semibold">Mang đi</h2>
        <Button onClick={onNew} busy={creating}>Tạo hóa đơn mang đi</Button>
      </div>
      {orders.length > 0 && (
        <div className="grid grid-cols-[repeat(auto-fill,minmax(8.5rem,1fr))] gap-3">
          {orders.map((order) => (
            <TakeAwayTile key={order.id} order={order} selected={order.id === selectedOrderId} onSelect={onOrder} />
          ))}
        </div>
      )}
    </section>
  )
}

function TableMapSkeleton() {
  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(8.5rem,1fr))] gap-3 p-6" aria-busy>
      {Array.from({ length: 12 }, (_, i) => <div key={i} className="h-24 animate-pulse rounded-(--radius-panel) bg-surface" />)}
    </div>
  )
}
