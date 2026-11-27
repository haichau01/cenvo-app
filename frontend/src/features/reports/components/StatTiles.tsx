import type { ReactNode } from 'react'
import { Money } from '@/components/cenvo/Money'
import type { SalesSummary } from '@/lib/api/types'
import { marginPercent } from '../lib/metrics'

/** Headline numbers: a single value per job, so stat tiles — not charts. Revenue is the hero. */
export function StatTiles({ summary }: { summary: SalesSummary }) {
  const margin = marginPercent(summary.grossProfit, summary.revenue)
  return (
    <section aria-label="Tổng quan" className="mb-6 grid grid-cols-[2fr_repeat(3,1fr)] gap-4">
      <Tile label="Doanh thu" hero>
        <Money value={summary.revenue} className="amount text-amount" />
      </Tile>
      <Tile label="Lãi gộp" note={margin === null ? undefined : `Biên lãi gộp ${margin}%`}>
        <Money value={summary.grossProfit} />
      </Tile>
      <Tile label="Giá vốn" note="Theo phiếu xuất bán còn hiệu lực"><Money value={summary.cost} /></Tile>
      <Tile label="Hóa đơn" note={`Trung bình ${formatAverage(summary.averageTicket)} · Hủy ${summary.cancelled}`}>
        <span className="tabular-nums">{summary.orders}</span>
      </Tile>
    </section>
  )
}

function formatAverage(value: number): string {
  return `${new Intl.NumberFormat('vi-VN').format(value)} ₫`
}

function Tile({ label, note, hero, children }: { label: string; note?: string; hero?: boolean; children: ReactNode }) {
  return (
    <div className={`flex flex-col justify-between gap-2 rounded-(--radius-panel) bg-surface p-5 ${hero ? 'border-l-4 border-robusta' : ''}`}>
      <p className="text-caption text-ink-muted">{label}</p>
      <div className={hero ? '' : 'font-display text-2xl font-bold'}>{children}</div>
      {note && <p className="text-caption text-ink-muted">{note}</p>}
    </div>
  )
}
