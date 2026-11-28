import { useCallback, useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { OrderDto, OrderLine } from '@/lib/api/types'
import { formatTime } from '@/lib/format'
import { useOrder } from '../api/orders'
import { usePrintReceipt } from '../api/receipts'
import type { DiscountTarget } from '../lib/discount'
import { orderTitle, totalQuantity } from '../lib/order'
import { CancelOrderDialog } from './CancelOrderDialog'
import { DiscountDialog } from './DiscountDialog'
import { MoveDialog } from './MoveDialog'
import { OrderFooter } from './OrderFooter'
import { OrderLineRow } from './OrderLineRow'
import { PayDialog } from './PayDialog'

interface OrderPanelProps {
  orderId: string | null
  shiftOpen: boolean
  onRequestShift: () => void
  /** Show another bill (after moving items to it). */
  onSelectOrder: (orderId: string) => void
  /** The order was paid or cancelled — leave it. */
  onFinished: () => void
}

/** Right-hand bill panel: always visible, total always on screen. */
export function OrderPanel({ orderId, shiftOpen, onRequestShift, onFinished, onSelectOrder }: OrderPanelProps) {
  const order = useOrder(orderId)

  if (orderId === null) {
    return <PanelShell><EmptyState className="flex-1" title="Chưa chọn hóa đơn"
      description="Chạm vào một bàn hoặc tạo hóa đơn mang đi để bắt đầu gọi món." /></PanelShell>
  }
  if (order.isPending) return <PanelShell><div className="m-4 h-32 animate-pulse rounded-(--radius-panel) bg-concrete" /></PanelShell>
  if (order.isError) {
    return <PanelShell><EmptyState className="flex-1" title="Không mở được hóa đơn" description={order.error.message}
      action={<Button variant="secondary" onClick={onFinished}>Quay lại sơ đồ bàn</Button>} /></PanelShell>
  }
  if (order.data.status !== 'Open') {
    // Paid or cancelled on another device (realtime refetch) — editing it would only produce errors.
    const closedBy = order.data.status === 'Paid' ? 'đã được thanh toán' : 'đã bị hủy'
    return <PanelShell><EmptyState className="flex-1" title={`Hóa đơn ${order.data.code} ${closedBy}`}
      description="Thao tác này vừa được thực hiện trên máy khác."
      action={<Button variant="secondary" onClick={onFinished}>Quay lại sơ đồ bàn</Button>} /></PanelShell>
  }
  return <OpenOrder order={order.data} shiftOpen={shiftOpen} onRequestShift={onRequestShift} onFinished={onFinished}
    onSelectOrder={onSelectOrder} />
}

function PanelShell({ children }: { children: React.ReactNode }) {
  return <aside aria-label="Hóa đơn" className="flex min-h-0 flex-col border-l border-line bg-surface">{children}</aside>
}

interface OpenOrderProps {
  order: OrderDto
  shiftOpen: boolean
  onRequestShift: () => void
  onFinished: () => void
  onSelectOrder: (orderId: string) => void
}

function OpenOrder({ order, shiftOpen, onRequestShift, onFinished, onSelectOrder }: OpenOrderProps) {
  const [dialog, setDialog] = useState<'pay' | 'cancel' | 'move' | null>(null)
  const [discountTarget, setDiscountTarget] = useState<DiscountTarget | null>(null)
  const printReceipt = usePrintReceipt()
  const closeDialog = () => setDialog(null)
  const empty = order.lines.length === 0
  const handleLineDiscount = useCallback((line: OrderLine) => setDiscountTarget({ kind: 'line', line }), [])

  function handlePay() {
    if (shiftOpen) setDialog('pay')
    else onRequestShift()
  }

  function handleFinished() {
    closeDialog()
    onFinished()
  }

  function handleMoved(orderId: string) {
    closeDialog()
    onSelectOrder(orderId)
  }

  return (
    <PanelShell>
      <header className="flex flex-col gap-2 border-b border-line px-5 py-3">
        <div>
          <h2 className="font-display text-screen font-semibold">{orderTitle(order)}</h2>
          <p className="text-caption text-ink-muted tabular-nums">
            {order.code} · {formatTime(order.createdAt)} · {totalQuantity(order)} món
          </p>
        </div>
        <div className="-ml-4 flex flex-wrap">
          <Button variant="ghost" onClick={() => setDialog('move')} disabled={empty}>Chuyển / gộp</Button>
          <Button variant="ghost" onClick={() => setDiscountTarget({ kind: 'order' })} disabled={empty}>Giảm giá</Button>
          <Button variant="ghost" onClick={() => printReceipt.mutate(order.id)} busy={printReceipt.isPending} disabled={empty}>
            In tạm tính
          </Button>
          <Button variant="ghost" onClick={() => setDialog('cancel')} className="ml-auto text-stop">Hủy</Button>
        </div>
      </header>

      <ul className="min-h-0 flex-1 overflow-y-auto">
        {order.lines.map((line) => <OrderLineRow key={line.id} orderId={order.id} line={line} onDiscount={handleLineDiscount} />)}
        {empty && (
          <li><EmptyState title="Chưa có món" description="Chạm vào món bên trái để thêm vào hóa đơn." /></li>
        )}
      </ul>

      <OrderFooter order={order} shiftOpen={shiftOpen} onPay={handlePay} />

      <PayDialog order={dialog === 'pay' ? order : null} onClose={closeDialog} onPaid={handleFinished} />
      <CancelOrderDialog order={dialog === 'cancel' ? order : null} onClose={closeDialog} onCancelled={handleFinished} />
      <MoveDialog order={dialog === 'move' ? order : null} onClose={closeDialog} onMoved={handleMoved} />
      <DiscountDialog order={order} target={discountTarget} onClose={() => setDiscountTarget(null)} />
    </PanelShell>
  )
}
