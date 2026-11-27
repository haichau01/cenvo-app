import { formatVndNumber } from '@/lib/format'

interface MoneyProps {
  value: number
  className?: string
}

/** VND amount: "85.000 ₫" — tabular digits, ₫ after the number at 0.7em; negatives in `stop`. */
export function Money({ value, className = '' }: MoneyProps) {
  const negative = value < 0
  return (
    <span className={`whitespace-nowrap tabular-nums ${negative ? 'text-stop' : ''} ${className}`}>
      {formatVndNumber(value)}
      <span className="ml-[0.15em] text-[0.7em]">₫</span>
    </span>
  )
}
