import type { ReactNode } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'

interface ReportStateProps<T> {
  query: { data: T | undefined; isPending: boolean; isError: boolean; error: Error | null; isPlaceholderData: boolean; refetch: () => unknown }
  children: (data: T) => ReactNode
}

/** Skeleton while loading, a retry on error, and the previous period dimmed while the next one loads. */
export function ReportState<T>({ query, children }: ReportStateProps<T>) {
  if (query.isPending) return <div className="h-40 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (query.isError || query.data === undefined) {
    return <EmptyState title="Không tải được báo cáo" description={query.error?.message}
      action={<Button variant="secondary" onClick={() => query.refetch()}>Tải lại</Button>} />
  }
  return (
    <div className={`flex flex-col gap-6 transition-opacity ${query.isPlaceholderData ? 'opacity-60' : ''}`}>
      {children(query.data)}
    </div>
  )
}
