import type { ReactNode } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Plate } from '@/components/cenvo/Plate'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import type { WashTicket } from '@/lib/api/types'
import { formatTime } from '@/lib/format'
import { useMoveTicket, useTicket } from '../api/tickets'
import { washStatusLabel } from '../lib/status'
import { vehicleLabel } from '../lib/vehicle'
import { PhotoStrip } from './PhotoStrip'
import { StaffPicker } from './StaffPicker'
import { TicketServices } from './TicketServices'

interface TicketSheetProps {
  ticketId: string | null
  canTakePayment: boolean
  onClose: () => void
  onPay: (ticket: WashTicket) => void
  onCancel: (ticket: WashTicket) => void
}

/** Everything about one car: services, washers, photos, money — and the actions the queue card has no room for. */
export function TicketSheet({ ticketId, canTakePayment, onClose, onPay, onCancel }: TicketSheetProps) {
  const ticket = useTicket(ticketId).data
  return (
    <Dialog open={ticketId !== null} onClose={onClose} title={ticket ? `Phiếu rửa ${ticket.code}` : 'Phiếu rửa'}
      className="w-[min(46rem,calc(100vw-2rem))]"
      footer={ticket && <SheetActions ticket={ticket} canTakePayment={canTakePayment} onPay={onPay} onCancel={onCancel} />}>
      {ticket ? <SheetBody ticket={ticket} /> : <div className="h-64 animate-pulse rounded-(--radius-panel) bg-concrete" aria-busy />}
    </Dialog>
  )
}

function SheetBody({ ticket }: { ticket: WashTicket }) {
  const status = washStatusLabel(ticket.status)
  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center gap-3">
        <Plate value={ticket.plate} size="lg" />
        <StatusPill label={status.label} tone={status.tone} />
        {ticket.isPaid && <StatusPill label="Đã thu tiền" tone="go" />}
        <span className="ml-auto text-sm text-ink-muted">Nhận lúc {formatTime(ticket.createdAt)}</span>
      </div>
      <p className="text-ink">
        {vehicleLabel(ticket.vehicleType)}{ticket.vehicleDescription && ` · ${ticket.vehicleDescription}`}
        {ticket.customerName && ` · ${ticket.customerName}`}{ticket.customerPhone && ` · ${ticket.customerPhone}`}
      </p>
      {ticket.note && <p className="font-semibold">Ghi chú: {ticket.note}</p>}
      {ticket.cancelReason && <p className="font-semibold text-stop">Đã hủy: {ticket.cancelReason}</p>}

      <TicketServices ticket={ticket} />
      <Totals ticket={ticket} />
      <Block title="Người rửa"><StaffPicker ticket={ticket} /></Block>
      <Block title="Ảnh hiện trạng"><PhotoStrip ticket={ticket} /></Block>
    </div>
  )
}

function Totals({ ticket }: { ticket: WashTicket }) {
  return (
    <dl className="flex flex-col gap-1 text-table">
      {ticket.membershipCovered > 0 && (
        <div className="flex justify-between"><dt className="text-ink-muted">Gói thành viên trả</dt><dd><Money value={-ticket.membershipCovered} /></dd></div>
      )}
      <div className="flex items-baseline justify-between">
        <dt className="font-semibold">Khách trả</dt>
        <dd><Money value={ticket.total} className="font-display text-2xl font-bold" /></dd>
      </div>
    </dl>
  )
}

function Block({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h3 className="font-display text-lg font-semibold">{title}</h3>
      {children}
    </section>
  )
}

interface SheetActionsProps {
  ticket: WashTicket
  canTakePayment: boolean
  onPay: (ticket: WashTicket) => void
  onCancel: (ticket: WashTicket) => void
}

function SheetActions({ ticket, canTakePayment, onPay, onCancel }: SheetActionsProps) {
  const move = useMoveTicket()
  if (ticket.status === 'Cancelled') return null
  const canPay = canTakePayment && !ticket.isPaid
  return (
    <div className="flex w-full flex-wrap gap-3">
      <Button variant="ghost" className="text-stop" onClick={() => onCancel(ticket)}>Hủy phiếu</Button>
      {ticket.status === 'Washing' && (
        <Button variant="secondary" busy={move.isPending} onClick={() => move.mutate({ id: ticket.id, step: 'return' })}>
          Đưa lại hàng chờ
        </Button>
      )}
      {canPay && <Button size="lg" className="ml-auto" onClick={() => onPay(ticket)}>Thu tiền</Button>}
    </div>
  )
}
