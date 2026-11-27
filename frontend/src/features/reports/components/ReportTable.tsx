import type { ReactNode } from 'react'

export const th = 'px-4 py-2.5 text-left text-caption font-medium text-ink-muted'
export const thNum = `${th} text-right`
export const tdNum = 'px-4 text-right tabular-nums'

/** Dense report table (44px rows) inside a surface panel. */
export function ReportTable({ head, children }: { head: ReactNode; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-table">
        <thead className="border-b border-line"><tr>{head}</tr></thead>
        <tbody className="[&>tr]:h-11 [&>tr]:border-b [&>tr]:border-line [&>tr:last-child]:border-0">{children}</tbody>
      </table>
    </div>
  )
}

/** Inline magnitude bar (one hue) with the percentage as text — never color alone. */
export function ShareBar({ value }: { value: number }) {
  const percent = Math.round(value * 100)
  return (
    <span className="flex items-center justify-end gap-2">
      <span aria-hidden className="h-2 w-24 overflow-hidden rounded-full bg-concrete">
        <span className="block h-full rounded-full bg-ink-muted" style={{ width: `${percent}%` }} />
      </span>
      <span className="w-10 text-right tabular-nums text-ink-muted">{percent}%</span>
    </span>
  )
}

export function Panel({ title, description, actions, children }: { title: string; description?: string; actions?: ReactNode; children: ReactNode }) {
  return (
    <section aria-label={title} className="rounded-(--radius-panel) bg-surface p-5">
      <div className="mb-3 flex items-start justify-between gap-4">
        <div>
          <h2 className="font-display text-block font-semibold">{title}</h2>
          {description && <p className="text-caption text-ink-muted">{description}</p>}
        </div>
        {actions}
      </div>
      {children}
    </section>
  )
}
