import type { ReactNode } from 'react'
import type { BranchContact, PaymentMethod } from '@/lib/api/types'
import { formatDateTime, formatVnd } from '@/lib/format'

/**
 * Parts shared by every thermal slip (café receipt, car wash slip, membership receipt):
 * black on white only, Barlow, sized in pt so 80mm and 58mm paper both read well.
 */
export function Slip({ store, title, children }: { store: BranchContact; title: string; children: ReactNode }) {
  return (
    <article className="bg-white px-[3mm] py-[4mm] font-sans text-[9pt] leading-snug text-black">
      <header className="text-center">
        <p className="font-display text-[13pt] font-bold">{store.name}</p>
        {store.address && <p>{store.address}</p>}
        {store.phone && <p>ĐT: {store.phone}</p>}
        <p className="mt-[2mm] font-display text-[12pt] font-bold">{title}</p>
      </header>
      {children}
    </article>
  )
}

/** Document number (bold), then label / value pairs (time, cashier…). Empty values are skipped. */
export function SlipFacts({ code, facts }: { code: string; facts: [label: string, value: string | null | undefined][] }) {
  return (
    <dl className="mt-[2mm] grid grid-cols-[auto_1fr] gap-x-[2mm]">
      <dt>Số:</dt><dd className="text-right font-semibold">{code}</dd>
      {facts.filter(([, value]) => value).map(([label, value]) => (
        <div key={label} className="contents">
          <dt>{label}:</dt><dd className="text-right">{value}</dd>
        </div>
      ))}
    </dl>
  )
}

export function SlipRow({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex justify-between gap-[2mm] tabular-nums ${strong ? 'mt-[1mm] font-display text-[12pt] font-bold' : ''}`}>
      <span>{label}</span>
      <span className="shrink-0">{value}</span>
    </div>
  )
}

export function SlipDivider() {
  return <hr className="my-[2mm] border-0 border-t border-dashed border-black" />
}

const METHOD_LABELS: Record<PaymentMethod, string> = { Cash: 'Tiền mặt', Transfer: 'Chuyển khoản', Card: 'Thẻ' }

interface SlipPaymentsProps {
  payments: { method: PaymentMethod; amount: number }[]
  cashReceived: number
  changeAmount: number
}

/** Non-cash payments by method, then the cash handed over and the change. */
export function SlipPayments({ payments, cashReceived, changeAmount }: SlipPaymentsProps) {
  return (
    <div className="mt-[1mm]">
      {payments.filter((p) => p.method !== 'Cash').map((payment) => (
        <SlipRow key={payment.method} label={METHOD_LABELS[payment.method]} value={formatVnd(payment.amount)} />
      ))}
      {cashReceived > 0 && <SlipRow label="Khách đưa (tiền mặt)" value={formatVnd(cashReceived)} />}
      {changeAmount > 0 && <SlipRow label="Tiền thối" value={formatVnd(changeAmount)} />}
    </div>
  )
}

/** `printedAt` is passed in (not read from the clock) so slips stay pure components. */
export function SlipFooter({ printedAt, children }: { printedAt: string; children?: ReactNode }) {
  return (
    <>
      <SlipDivider />
      <footer className="text-center">
        {children}
        <p className="mt-[1mm] text-[7pt]">In lúc {formatDateTime(printedAt)}</p>
      </footer>
    </>
  )
}
