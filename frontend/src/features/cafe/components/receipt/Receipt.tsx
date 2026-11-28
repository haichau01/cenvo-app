import { Slip, SlipDivider, SlipFacts, SlipFooter, SlipPayments, SlipRow } from '@/components/cenvo/Slip'
import type { OrderLine, ReceiptDto } from '@/lib/api/types'
import { formatDateTime, formatQuantity, formatVnd } from '@/lib/format'
import { orderTitle } from '../../lib/order'
import { receiptTitle } from '../../lib/receipt'

/** Café receipt (or pre-bill while the order is open). `printedAt` is passed in so the component stays pure. */
export function Receipt({ receipt, printedAt }: { receipt: ReceiptDto; printedAt: string }) {
  const { order, store } = receipt
  return (
    <Slip store={store} title={receiptTitle(order.status)}>
      <SlipFacts code={order.code} facts={[
        ['Giờ', formatDateTime(order.paidAt ?? order.createdAt)],
        ['Chỗ', orderTitle(order)],
        ['Thu ngân', receipt.cashierName],
      ]} />

      <SlipDivider />
      <ul className="flex flex-col gap-[1.5mm]">
        {order.lines.map((line) => <ReceiptLine key={line.id} line={line} />)}
      </ul>
      <SlipDivider />

      <SlipRow label="Tạm tính" value={formatVnd(order.subtotal)} />
      {order.discountAmount > 0 && <SlipRow label="Giảm giá" value={`−${formatVnd(order.discountAmount)}`} />}
      <SlipRow label="Tổng cộng" value={formatVnd(order.total)} strong />
      {order.status === 'Paid' && (
        <SlipPayments payments={order.payments} cashReceived={order.cashReceived} changeAmount={order.changeAmount} />
      )}

      <SlipFooter printedAt={printedAt}>
        {order.status === 'Open' && <p className="font-semibold">Phiếu chưa thanh toán</p>}
        {order.status === 'Cancelled' && <p className="font-semibold">Lý do hủy: {order.cancelReason}</p>}
        <p>Cảm ơn quý khách. Hẹn gặp lại!</p>
      </SlipFooter>
    </Slip>
  )
}

function ReceiptLine({ line }: { line: OrderLine }) {
  return (
    <li>
      <p className="font-semibold">{line.name}</p>
      <div className="flex justify-between tabular-nums">
        <span>{formatQuantity(line.quantity)} × {formatVnd(line.unitPrice)}</span>
        <span>{formatVnd(line.amount)}</span>
      </div>
      {line.toppings.map((topping) => (
        <p key={topping.productId} className="pl-[2mm]">+ {topping.name} ({formatVnd(topping.unitPrice)})</p>
      ))}
      {line.discountAmount > 0 && <p className="pl-[2mm]">Giảm −{formatVnd(line.discountAmount)}</p>}
      {line.note && <p className="pl-[2mm] italic">{line.note}</p>}
    </li>
  )
}
