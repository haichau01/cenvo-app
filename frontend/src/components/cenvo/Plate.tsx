import { formatPlate } from '@/lib/plate'

interface PlateProps {
  value: string
  size?: 'md' | 'lg'
  className?: string
}

/** Licence plate chip built like a real plate — Cenvo's signature element (queue cards, history, search, slips). */
export function Plate({ value, size = 'md', className = '' }: PlateProps) {
  const text = size === 'lg' ? 'px-3 py-1 text-[1.75rem]' : 'px-2 py-0.5 text-lg'
  return (
    <span className={`inline-flex shrink-0 items-center whitespace-nowrap rounded-(--radius-plate) border-2 border-plate-ink bg-plate font-display font-bold
      leading-none tracking-[0.02em] text-plate-ink tabular-nums ${text} ${className}`}>
      {formatPlate(value)}
    </span>
  )
}
