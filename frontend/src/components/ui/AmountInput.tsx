import type { InputHTMLAttributes } from 'react'
import { formatVndNumber } from '@/lib/format'
import { cellControlClass } from './fieldStyles'

interface AmountInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange'> {
  value: number
  onChange: (value: number) => void
}

/** Compact VND input for table cells: digits only, thousand separators while typing. */
export function AmountInput({ value, onChange, className = '', ...rest }: AmountInputProps) {
  return (
    <input {...rest} inputMode="numeric" autoComplete="off" value={value === 0 ? '' : formatVndNumber(value)} placeholder="0"
      onChange={(event) => onChange(Number(event.target.value.replace(/\D/g, '')) || 0)}
      className={`${cellControlClass} text-right tabular-nums ${className}`} />
  )
}
