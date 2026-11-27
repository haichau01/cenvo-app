import { useId, type InputHTMLAttributes, type ReactNode, type Ref } from 'react'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  /** Secondary button inside the field (e.g. show/hide password). */
  trailing?: ReactNode
  ref?: Ref<HTMLInputElement>
}

export function TextField({ label, error, trailing, className = '', ref, ...rest }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label htmlFor={id} className="text-sm font-medium text-ink">
        {label}
      </label>
      <div
        className={`flex h-12 items-center rounded-(--radius-control) border bg-surface
          focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-foam
          ${error ? 'border-stop' : 'border-line'}`}
      >
        <input
          {...rest}
          ref={ref}
          id={id}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className="h-full min-w-0 flex-1 bg-transparent px-3 text-base text-ink outline-none placeholder:text-ink-muted/70"
        />
        {trailing}
      </div>
      {error && (
        <p id={errorId} className="text-caption font-medium text-stop">
          {error}
        </p>
      )}
    </div>
  )
}
