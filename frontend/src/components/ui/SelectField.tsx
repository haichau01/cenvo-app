import { useId, type SelectHTMLAttributes } from 'react'
import { controlClass } from './fieldStyles'

export interface SelectOption {
  value: string
  label: string
}

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'value' | 'onChange'> {
  label: string
  value: string
  onChange: (value: string) => void
  options: SelectOption[]
  /** First option meaning "nothing selected / all", e.g. "Tất cả kho". */
  placeholder?: string
}

/** Native select: fast, accessible, works with the on-screen keyboard and screen readers. */
export function SelectField({ label, value, onChange, options, placeholder, className = '', ...rest }: SelectFieldProps) {
  const id = useId()
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label htmlFor={id} className="text-sm font-medium text-ink">{label}</label>
      <select {...rest} id={id} value={value} onChange={(event) => onChange(event.target.value)} className={controlClass}>
        {placeholder !== undefined && <option value="">{placeholder}</option>}
        {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    </div>
  )
}
