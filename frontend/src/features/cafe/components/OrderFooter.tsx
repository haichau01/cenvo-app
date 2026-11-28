import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/toastStore'
import type { OrderDto } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import { useSendToKitchen } from '../api/orders'
import { unsentLines } from '../lib/order'

interface OrderFooterProps {
  order: OrderDto
  shiftOpen: boolean
  onPay: () => void
}

export function OrderFooter({ order, shiftOpen, onPay }: OrderFooterProps) {
  const send = useSendToKitchen()
  const unsent = unsentLines(order)
  const unsentCount = unsent.reduce((sum, line) => sum + line.quantity, 0)
  const empty = order.lines.length === 0

  function handleSend() {
    send.mutate(order.id, { onSuccess: () => toast.success(`Đã gửi ${formatQuantity(unsentCount)} món`) })
  }

  return (
    <footer className="flex flex-col gap-3 border-t border-line px-5 py-4">
      {order.discountAmount > 0 && (
        <dl className="flex flex-col gap-1 text-table">
          <SummaryRow label="Tạm tính" value={order.subtotal} />
          <SummaryRow label={`Giảm giá · ${order.discountReason ?? ''}`} value={-order.discountAmount} />
        </dl>
      )}
      {order.discountApprovedByName && (
        <p className="text-caption text-ink-muted">Giảm giá vượt mức do {order.discountApprovedByName} duyệt</p>
      )}
      <div className="flex items-baseline justify-between">
        <span className="font-medium text-ink-muted">Tổng cộng</span>
        <Money value={order.total} className="amount text-amount" />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Button variant="secondary" size="lg" onClick={handleSend} busy={send.isPending} disabled={unsentCount === 0}>
          Gửi pha chế{unsentCount > 0 && ` (${formatQuantity(unsentCount)})`}
        </Button>
        <Button size="lg" onClick={onPay} disabled={empty}>
          {shiftOpen ? 'Thanh toán' : 'Mở ca để thu'}
        </Button>
      </div>
    </footer>
  )
}

function SummaryRow({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex justify-between">
      <dt className="text-ink-muted">{label}</dt>
      <dd><Money value={value} /></dd>
    </div>
  )
}
