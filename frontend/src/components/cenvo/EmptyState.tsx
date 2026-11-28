import type { ReactNode } from 'react'

interface EmptyStateProps {
  title: string
  description?: string
  action?: ReactNode
  className?: string
}

/** Every empty list is an invitation to act: say what's missing and offer the next step. */
export function EmptyState({ title, description, action, className = '' }: EmptyStateProps) {
  return (
    <div className={`flex flex-col items-center justify-center gap-3 px-6 py-10 text-center ${className}`}>
      <p className="font-display text-block font-semibold text-ink">{title}</p>
      {description && <p className="max-w-sm text-ink-muted">{description}</p>}
      {action}
    </div>
  )
}
