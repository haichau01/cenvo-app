import { Money } from '@/components/cenvo/Money'
import { MoneyField } from '@/components/ui/MoneyField'
import type { PaymentMethod } from '@/lib/api/types'
import { suggestCash } from '@/lib/payment'

const METHODS: { value: PaymentMethod; label: string }[] = [
  { value: 'Cash', label: 'Tiền mặt' },
  { value: 'Transfer', label: 'Chuyển khoản' },
  { value: 'Card', label: 'Thẻ' },
]

export function MethodPicker({ method, onMethod }: { method: PaymentMethod; onMethod: (method: PaymentMethod) => void }) {
  return (
    <div role="radiogroup" aria-label="Hình thức thanh toán" className="grid grid-cols-3 gap-2">
      {METHODS.map((option) => (
        <button key={option.value} type="button" role="radio" aria-checked={method === option.value}
          onClick={() => onMethod(option.value)}
          className={`h-12 rounded-(--radius-control) border-2 font-semibold
            ${method === option.value ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
          {option.label}
        </button>
      ))}
    </div>
  )
}

export function CashInput({ total, given, onGiven, change }: { total: number; given: number; onGiven: (v: number) => void; change: number }) {
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
