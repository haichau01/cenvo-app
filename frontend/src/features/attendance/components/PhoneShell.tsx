import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Wordmark } from '@/components/cenvo/Wordmark'

/** One-column phone layout shared by the staff screens (schedule, attendance). */
export function PhoneShell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-dvh bg-concrete">
      <header className="flex h-14 items-center gap-3 border-b border-line bg-surface px-4">
        <Wordmark />
        <Link to="/" className="ml-auto flex h-11 items-center rounded-(--radius-control) px-3 font-medium hover:bg-ink/5">Trang chủ</Link>
      </header>
      <main className="mx-auto flex max-w-md flex-col gap-4 px-4 py-6">{children}</main>
    </div>
  )
}
