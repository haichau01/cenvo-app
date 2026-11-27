import { formatQuantity } from '@/lib/format'

interface QuantityStepperProps {
  value: number
  onChange: (value: number) => void
  /** Name of the item, for screen readers ("Giảm Bạc xỉu"). */
  label: string
  min?: number
  disabled?: boolean
}

/** − n + with 48px touch targets. Going below `min` is left to the caller (e.g. remove the line). */
export function QuantityStepper({ value, onChange, label, min = 0, disabled }: QuantityStepperProps) {
  return (
    <div className="inline-flex items-center rounded-(--radius-control) border border-line bg-surface">
      <StepButton label={`Giảm ${label}`} disabled={disabled || value <= min} onClick={() => onChange(value - 1)}>−</StepButton>
      <span className="w-8 text-center font-semibold tabular-nums" aria-live="polite">{formatQuantity(value)}</span>
      <StepButton label={`Tăng ${label}`} disabled={disabled} onClick={() => onChange(value + 1)}>+</StepButton>
    </div>
  )
}

interface StepButtonProps {
  label: string
  disabled?: boolean
  onClick: () => void
  children: string
}

function StepButton({ label, disabled, onClick, children }: StepButtonProps) {
  return (
    <button type="button" aria-label={label} disabled={disabled} onClick={onClick}
      className="flex size-12 items-center justify-center text-xl text-ink active:bg-ink/10 disabled:opacity-40">
      {children}
    </button>
  )
}
