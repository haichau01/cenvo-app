import { useId } from 'react'
import { formatVndNumber } from '@/lib/format'

interface MoneyFieldProps {
  label: string
  value: number
  onChange: (value: number) => void
  autoFocus?: boolean
}

/** Amount input: digits only, shown with thousand separators while typing, numeric keypad on tablets. */
export function MoneyField({ label, value, onChange, autoFocus }: MoneyFieldProps) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-ink">{label}</label>
      <div className="flex h-14 items-center rounded-(--radius-control) border border-line bg-surface
        focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-foam">
        <input
          id={id}
          inputMode="numeric"
          autoComplete="off"
          autoFocus={autoFocus}
          value={value === 0 ? '' : formatVndNumber(value)}
          placeholder="0"
          onChange={(event) => onChange(Number(event.target.value.replace(/\D/g, '')) || 0)}
          className="h-full min-w-0 flex-1 bg-transparent px-3 text-right font-display text-2xl font-semibold tabular-nums outline-none"
        />
        <span className="pr-3 text-lg text-ink-muted">₫</span>
      </div>
    </div>
  )
}
