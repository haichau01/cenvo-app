import type { ButtonHTMLAttributes } from 'react'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  /** md = 48px (minimum tablet touch target), lg = 56px (primary action). */
  size?: Size
  busy?: boolean
}

const variants: Record<Variant, string> = {
  // --module: primary color of the current module (ink / robusta / foam).
  primary: 'bg-(--module) text-(--primary-foreground) hover:brightness-110 active:brightness-95',
  secondary: 'bg-surface text-ink border border-line hover:bg-concrete active:bg-line/60',
  ghost: 'text-ink hover:bg-ink/5 active:bg-ink/10',
  danger: 'bg-stop text-white hover:brightness-110 active:brightness-95',
}

const sizes: Record<Size, string> = {
  md: 'h-12 px-4 text-base',
  lg: 'h-14 px-6 text-lg',
}

export function Button({ variant = 'primary', size = 'md', busy = false, disabled, className = '', children, ...rest }: ButtonProps) {
  return (
    <button
      {...rest}
      disabled={disabled || busy}
      aria-busy={busy || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-(--radius-control) font-semibold
        transition-[filter,background-color] duration-100 disabled:cursor-not-allowed disabled:opacity-60
        ${variants[variant]} ${sizes[size]} ${className}`}
    >
      {busy && <Spinner />}
      {children}
    </button>
  )
}

function Spinner() {
  return <span aria-hidden className="size-4 animate-spin rounded-full border-2 border-current border-r-transparent" />
}
