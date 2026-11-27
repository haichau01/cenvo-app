import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { OrderDto } from '@/lib/api/types'
import { formatTime } from '@/lib/format'
import { useOrder } from '../api/orders'
import { usePrintReceipt } from '../api/receipts'
import { orderTitle, totalQuantity } from '../lib/order'
import { CancelOrderDialog } from './CancelOrderDialog'
import { OrderFooter } from './OrderFooter'
import { OrderLineRow } from './OrderLineRow'
import { PayDialog } from './PayDialog'

interface OrderPanelProps {
  orderId: string | null
  shiftOpen: boolean
  onRequestShift: () => void
  /** The order was paid or cancelled — leave it. */
  onFinished: () => void
}

/** Right-hand bill panel: always visible, total always on screen. */
export function OrderPanel({ orderId, shiftOpen, onRequestShift, onFinished }: OrderPanelProps) {
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
  return <OpenOrder order={order.data} shiftOpen={shiftOpen} onRequestShift={onRequestShift} onFinished={onFinished} />
}

function PanelShell({ children }: { children: React.ReactNode }) {
  return <aside aria-label="Hóa đơn" className="flex min-h-0 flex-col border-l border-line bg-surface">{children}</aside>
}

interface OpenOrderProps {
  order: OrderDto
  shiftOpen: boolean
  onRequestShift: () => void
  onFinished: () => void
}

function OpenOrder({ order, shiftOpen, onRequestShift, onFinished }: OpenOrderProps) {
  const [dialog, setDialog] = useState<'pay' | 'cancel' | null>(null)
  const printReceipt = usePrintReceipt()
  const closeDialog = () => setDialog(null)

  function handlePay() {
    if (shiftOpen) setDialog('pay')
    else onRequestShift()
  }

  function handleFinished() {
    closeDialog()
    onFinished()
  }

  return (
    <PanelShell>
      <header className="flex items-start justify-between gap-3 border-b border-line px-5 py-4">
        <div>
          <h2 className="font-display text-screen font-semibold">{orderTitle(order)}</h2>
          <p className="text-caption text-ink-muted tabular-nums">
            {order.code} · {formatTime(order.createdAt)} · {totalQuantity(order)} món
          </p>
        </div>
        <div className="flex">
          <Button variant="ghost" onClick={() => printReceipt.mutate(order.id)} busy={printReceipt.isPending}
            disabled={order.lines.length === 0}>In tạm tính</Button>
          <Button variant="ghost" onClick={() => setDialog('cancel')} className="text-stop">Hủy</Button>
        </div>
      </header>

      <ul className="min-h-0 flex-1 overflow-y-auto">
        {order.lines.map((line) => <OrderLineRow key={line.id} orderId={order.id} line={line} />)}
        {order.lines.length === 0 && (
          <li><EmptyState title="Chưa có món" description="Chạm vào món bên trái để thêm vào hóa đơn." /></li>
        )}
      </ul>

      <OrderFooter order={order} shiftOpen={shiftOpen} onPay={handlePay} />

      <PayDialog order={dialog === 'pay' ? order : null} onClose={closeDialog} onPaid={handleFinished} />
      <CancelOrderDialog order={dialog === 'cancel' ? order : null} onClose={closeDialog} onCancelled={handleFinished} />
    </PanelShell>
  )
}
