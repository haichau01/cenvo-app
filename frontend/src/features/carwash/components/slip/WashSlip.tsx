import { Slip, SlipDivider, SlipFacts, SlipFooter, SlipPayments, SlipRow } from '@/components/cenvo/Slip'
import type { WashTicket, WashTicketLine, WashTicketReceipt } from '@/lib/api/types'
import { formatDateTime, formatVnd } from '@/lib/format'
import { formatPlate } from '@/lib/plate'
import { washSlipTitle, washSlipTotalLabel } from '../../lib/slip'
import { vehicleLabel } from '@/lib/vehicle'

/** Intake slip (customer keeps it to collect the car) or paid receipt of a car wash ticket. */
export function WashSlip({ receipt, printedAt }: { receipt: WashTicketReceipt; printedAt: string }) {
  const { ticket, store } = receipt
  return (
    <Slip store={store} title={washSlipTitle(ticket)}>
      {/* The plate is what staff match the car by — printed large and framed like the real plate. */}
      <p className="mx-auto mt-[2mm] w-fit rounded-[1mm] border-[0.6mm] border-black px-[3mm] py-[1mm] font-display text-[16pt] font-bold tabular-nums tracking-[0.02em]">
        {formatPlate(ticket.plate)}
      </p>
      <SlipFacts code={ticket.code} facts={[
        ['Xe', [vehicleLabel(ticket.vehicleType), ticket.vehicleDescription].filter(Boolean).join(' · ')],
        ['Khách', [ticket.customerName, ticket.customerPhone].filter(Boolean).join(' · ')],
        ['Nhận lúc', formatDateTime(ticket.createdAt)],
        ['Thu tiền lúc', ticket.paidAt && formatDateTime(ticket.paidAt)],
        ['Thu ngân', receipt.cashierName],
      ]} />

      <SlipDivider />
      <ul className="flex flex-col gap-[1mm]">
        {ticket.lines.map((line) => <ServiceLine key={line.id} line={line} />)}
      </ul>
      <SlipDivider />

      <SlipRow label="Tạm tính" value={formatVnd(ticket.subtotal)} />
      {ticket.membershipCovered > 0 && <SlipRow label="Gói thành viên trả" value={`−${formatVnd(ticket.membershipCovered)}`} />}
      <SlipRow label={washSlipTotalLabel(ticket)} value={formatVnd(ticket.total)} strong />
      {ticket.isPaid && <SlipPayments payments={ticket.payments} cashReceived={ticket.cashReceived} changeAmount={ticket.changeAmount} />}
      {ticket.note && <p className="mt-[2mm]">Ghi chú: {ticket.note}</p>}

      <SlipFooter printedAt={printedAt}><FooterNote ticket={ticket} /></SlipFooter>
    </Slip>
  )
}

function ServiceLine({ line }: { line: WashTicketLine }) {
  return (
    <li>
      <SlipRow label={line.name} value={formatVnd(line.price)} />
      {line.membershipId && <p className="pl-[2mm]">Trừ 1 lượt gói thành viên</p>}
    </li>
  )
}

function FooterNote({ ticket }: { ticket: WashTicket }) {
  if (ticket.status === 'Cancelled') return <p className="font-semibold">Lý do hủy: {ticket.cancelReason}</p>
  if (!ticket.isPaid) return <p className="font-semibold">Vui lòng giữ phiếu này để nhận xe.</p>
  return <p>Cảm ơn quý khách. Hẹn gặp lại!</p>
}
