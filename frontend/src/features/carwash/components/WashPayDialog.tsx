import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Plate } from '@/components/cenvo/Plate'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import type { PaymentMethod } from '@/lib/api/types'
import { formatVnd } from '@/lib/format'
import { previewChange } from '@/lib/payment'
import { useStoredFlag } from '@/lib/useStoredFlag'
import { PRINT_AFTER_PAY_KEY, usePrintWashTicket } from '../api/printing'
import { usePayTicket } from '../api/tickets'
import { CashInput, MethodPicker } from './PaymentInputs'
import { PrintToggle } from './PrintToggle'

/** What the dialog needs — works from a queue card or the full ticket. */
export interface PayableTicket {
  id: string
  code: string
  plate: string
  total: number
}

export function WashPayDialog({ ticket, onClose }: { ticket: PayableTicket | null; onClose: () => void }) {
  return (
    <Dialog open={ticket !== null} onClose={onClose} title={ticket ? `Thu tiền ${ticket.code}` : ''}>
      {ticket && <PayForm key={ticket.id} ticket={ticket} onPaid={onClose} />}
    </Dialog>
  )
}

/** Exact cash is preselected: Thu tiền → Xác nhận is two taps. The money goes into the cashier's open shift. */
function PayForm({ ticket, onPaid }: { ticket: PayableTicket; onPaid: () => void }) {
  const pay = usePayTicket()
  const printTicket = usePrintWashTicket()
  const [printAfterPay, setPrintAfterPay] = useStoredFlag(PRINT_AFTER_PAY_KEY, true)
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [given, setGiven] = useState(ticket.total)
  const coveredByMembership = ticket.total === 0
  const isCash = method === 'Cash'
  const change = previewChange(ticket.total, given)

  function handleConfirm() {
    const payments = coveredByMembership ? [] : [{ method, amount: isCash ? given : ticket.total }]
    pay.mutate({ id: ticket.id, payments }, {
      onSuccess: (paid) => {
        const changeText = paid.changeAmount > 0 ? ` · Tiền thối ${formatVnd(paid.changeAmount)}` : ''
        toast.success(`Đã thu tiền ${paid.code}${changeText}`)
        onPaid()
        if (printAfterPay) printTicket.mutate(paid.id)
      },
    })
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-3">
        <Plate value={ticket.plate} size="lg" />
        <Money value={ticket.total} className="amount text-amount" />
        {coveredByMembership && <p className="text-ink-muted">Gói thành viên đã trả cho toàn bộ dịch vụ.</p>}
      </div>
      {!coveredByMembership && (
        <MethodPicker method={method} onMethod={setMethod} />
      )}
      {!coveredByMembership && isCash && <CashInput total={ticket.total} given={given} onGiven={setGiven} change={change} />}
      <FormError message={pay.error?.message} />
      <PrintToggle label="In hóa đơn sau khi thu tiền" checked={printAfterPay} onChange={setPrintAfterPay} />
      <Button size="lg" onClick={handleConfirm} busy={pay.isPending} disabled={isCash && !coveredByMembership && change < 0}>
        Xác nhận thu tiền
      </Button>
    </div>
  )
}
