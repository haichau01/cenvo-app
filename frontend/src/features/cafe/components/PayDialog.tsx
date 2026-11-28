import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { Dialog } from '@/components/ui/Dialog'
import { MoneyField } from '@/components/ui/MoneyField'
import { toast } from '@/components/ui/toastStore'
import type { OrderDto, PaymentMethod } from '@/lib/api/types'
import { formatVnd } from '@/lib/format'
import { usePayOrder } from '../api/orders'
import { usePrintReceipt } from '../api/receipts'
import { useStoredFlag } from '@/lib/useStoredFlag'
import { previewChange, suggestCash } from '@/lib/payment'

const PRINT_AFTER_PAY_KEY = 'cenvo.pos.printAfterPay'

const METHODS: { value: PaymentMethod; label: string }[] = [
  { value: 'Cash', label: 'Tiền mặt' },
  { value: 'Transfer', label: 'Chuyển khoản' },
  { value: 'Card', label: 'Thẻ' },
]

interface PayDialogProps {
  order: OrderDto | null
  onClose: () => void
  onPaid: () => void
}

export function PayDialog({ order, onClose, onPaid }: PayDialogProps) {
  return (
    <Dialog open={order !== null} onClose={onClose} title={order ? `Thanh toán ${order.code}` : ''}>
      {order && <PayForm key={order.id} order={order} onPaid={onPaid} />}
    </Dialog>
  )
}

/** Exact cash is preselected, so a typical sale is: Thanh toán → Xác nhận (2 taps). */
function PayForm({ order, onPaid }: { order: OrderDto; onPaid: () => void }) {
  const pay = usePayOrder()
  const printReceipt = usePrintReceipt()
  const [printAfterPay, setPrintAfterPay] = useStoredFlag(PRINT_AFTER_PAY_KEY, true)
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [given, setGiven] = useState(order.total)
  const isCash = method === 'Cash'
  const change = previewChange(order.total, given)
  const short = isCash && change < 0

  function handleConfirm() {
    const amount = isCash ? given : order.total
    pay.mutate({ orderId: order.id, payments: [{ method, amount }] }, {
      onSuccess: (paid) => {
        const changeText = paid.changeAmount > 0 ? ` · Tiền thối ${formatVnd(paid.changeAmount)}` : ''
        toast.success(`Đã thanh toán ${paid.code}${changeText}`)
        onPaid()
        if (printAfterPay) printReceipt.mutate(paid.id)
      },
    })
  }

  return (
    <div className="flex flex-col gap-6">
      <p className="text-center"><Money value={order.total} className="amount text-amount" /></p>

      <div role="radiogroup" aria-label="Hình thức thanh toán" className="grid grid-cols-3 gap-2">
        {METHODS.map((option) => (
          <button key={option.value} type="button" role="radio" aria-checked={method === option.value}
            onClick={() => setMethod(option.value)}
            className={`h-12 rounded-(--radius-control) border-2 font-semibold
              ${method === option.value ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
            {option.label}
          </button>
        ))}
      </div>

      {isCash && <CashInput total={order.total} given={given} onGiven={setGiven} change={change} />}

      <FormError message={pay.error?.message} />
      <label className="flex h-12 items-center gap-3 font-medium">
        <input type="checkbox" checked={printAfterPay} onChange={(event) => setPrintAfterPay(event.target.checked)}
          className="size-5 accent-(--module)" />
        In hóa đơn sau khi thanh toán
      </label>
      <Button size="lg" onClick={handleConfirm} busy={pay.isPending} disabled={short}>
        Xác nhận thanh toán
      </Button>
    </div>
  )
}

interface CashInputProps {
  total: number
  given: number
  onGiven: (value: number) => void
  change: number
}

function CashInput({ total, given, onGiven, change }: CashInputProps) {
  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-4 gap-2">
        {suggestCash(total).map((amount) => (
          <button key={amount} type="button" onClick={() => onGiven(amount)} aria-pressed={given === amount}
            className={`h-12 rounded-(--radius-control) border font-semibold tabular-nums
              ${given === amount ? 'border-ink bg-ink text-white' : 'border-line bg-surface'}`}>
            <Money value={amount} />
          </button>
        ))}
      </div>
      <MoneyField label="Khách đưa" value={given} onChange={onGiven} />
      <div className="flex items-baseline justify-between text-lg" aria-live="polite">
        <span className="text-ink-muted">{change < 0 ? 'Khách còn thiếu' : 'Tiền thối'}</span>
        <Money value={Math.abs(change)} className={`font-display text-2xl font-bold ${change < 0 ? 'text-stop' : ''}`} />
      </div>
    </div>
  )
}
