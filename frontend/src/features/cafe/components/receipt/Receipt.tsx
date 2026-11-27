import type { OrderLine, ReceiptDto } from '@/lib/api/types'
import { formatDateTime, formatQuantity, formatVnd } from '@/lib/format'
import { orderTitle } from '../../lib/order'
import { receiptTitle } from '../../lib/receipt'

/**
 * Thermal receipt: black on white only, Barlow, sized in pt so 80mm and 58mm paper both read well.
 * `printedAt` is passed in (not read from the clock) so the component stays pure.
 */
export function Receipt({ receipt, printedAt }: { receipt: ReceiptDto; printedAt: string }) {
  const { order, store } = receipt
  return (
    <article className="bg-white px-[3mm] py-[4mm] font-sans text-[9pt] leading-snug text-black">
      <header className="text-center">
        <p className="font-display text-[13pt] font-bold">{store.name}</p>
        {store.address && <p>{store.address}</p>}
        {store.phone && <p>ĐT: {store.phone}</p>}
        <p className="mt-[2mm] font-display text-[12pt] font-bold">{receiptTitle(order.status)}</p>
      </header>

      <dl className="mt-[2mm] grid grid-cols-[auto_1fr] gap-x-[2mm]">
        <dt>Số:</dt><dd className="text-right font-semibold">{order.code}</dd>
        <dt>Giờ:</dt><dd className="text-right">{formatDateTime(order.paidAt ?? order.createdAt)}</dd>
        <dt>Chỗ:</dt><dd className="text-right">{orderTitle(order)}</dd>
        {receipt.cashierName && <><dt>Thu ngân:</dt><dd className="text-right">{receipt.cashierName}</dd></>}
      </dl>

      <Divider />
      <ul className="flex flex-col gap-[1.5mm]">
        {order.lines.map((line) => <ReceiptLine key={line.id} line={line} />)}
      </ul>
      <Divider />

      <Row label="Tạm tính" value={formatVnd(order.subtotal)} />
      {order.discountAmount > 0 && <Row label="Giảm giá" value={`−${formatVnd(order.discountAmount)}`} />}
      <Row label="Tổng cộng" value={formatVnd(order.total)} strong />
      {order.status === 'Paid' && <Payments receipt={receipt} />}

      <Divider />
      <footer className="text-center">
        {order.status === 'Open' && <p className="font-semibold">Phiếu chưa thanh toán</p>}
        {order.status === 'Cancelled' && <p className="font-semibold">Lý do hủy: {order.cancelReason}</p>}
        <p>Cảm ơn quý khách. Hẹn gặp lại!</p>
        <p className="mt-[1mm] text-[7pt]">In lúc {formatDateTime(printedAt)}</p>
      </footer>
    </article>
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

const METHOD_LABELS = { Cash: 'Tiền mặt', Transfer: 'Chuyển khoản', Card: 'Thẻ' } as const

function Payments({ receipt }: { receipt: ReceiptDto }) {
  const { order } = receipt
  return (
    <div className="mt-[1mm]">
      {order.payments.filter((p) => p.method !== 'Cash').map((payment) => (
        <Row key={payment.method} label={METHOD_LABELS[payment.method]} value={formatVnd(payment.amount)} />
      ))}
      {order.cashReceived > 0 && <Row label="Khách đưa (tiền mặt)" value={formatVnd(order.cashReceived)} />}
      {order.changeAmount > 0 && <Row label="Tiền thối" value={formatVnd(order.changeAmount)} />}
    </div>
  )
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex justify-between tabular-nums ${strong ? 'mt-[1mm] font-display text-[12pt] font-bold' : ''}`}>
      <span>{label}</span>
      <span>{value}</span>
    </div>
  )
}

function Divider() {
  return <hr className="my-[2mm] border-0 border-t border-dashed border-black" />
}
