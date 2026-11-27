import type { ReactNode } from 'react'

/** Screen title (left) + main actions (right), same on every back-office page. */
export function PageHeader({ title, description, actions }: { title: string; description?: string; actions?: ReactNode }) {
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="font-display text-screen font-semibold text-ink">{title}</h1>
        {description && <p className="mt-1 text-ink-muted">{description}</p>}
      </div>
      {actions && <div className="flex gap-3">{actions}</div>}
    </header>
  )
}
