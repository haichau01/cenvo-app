import type { ReactNode } from 'react'
import { Money } from '@/components/cenvo/Money'
import type { SalesSummary } from '@/lib/api/types'
import { formatVnd } from '@/lib/format'
import { marginPercent } from '../lib/metrics'

/** Café headline numbers: a single value per job, so stat tiles — not charts. Revenue is the hero. */
export function StatTiles({ summary }: { summary: SalesSummary }) {
  const margin = marginPercent(summary.grossProfit, summary.revenue)
  return (
    <TileRow>
      <Tile label="Doanh thu" accent="border-robusta">
        <Money value={summary.revenue} className="amount text-amount" />
      </Tile>
      <Tile label="Lãi gộp" note={margin === null ? undefined : `Biên lãi gộp ${margin}%`}>
        <Money value={summary.grossProfit} />
      </Tile>
      <Tile label="Giá vốn" note="Theo phiếu xuất bán còn hiệu lực"><Money value={summary.cost} /></Tile>
      <Tile label="Hóa đơn" note={`Trung bình ${formatVnd(summary.averageTicket)} · Hủy ${summary.cancelled}`}>
        <span className="tabular-nums">{summary.orders}</span>
      </Tile>
    </TileRow>
  )
}

/** Hero tile first (twice as wide), then three secondary tiles. */
export function TileRow({ children }: { children: ReactNode }) {
  return <section aria-label="Tổng quan" className="mb-6 grid grid-cols-[2fr_repeat(3,1fr)] gap-4">{children}</section>
}

interface TileProps {
  label: string
  note?: string
  /** Border class of the hero tile — the module stripe (robusta, foam, or ink for the whole shop). */
  accent?: string
  children: ReactNode
}

export function Tile({ label, note, accent, children }: TileProps) {
  return (
    <div className={`flex flex-col justify-between gap-2 rounded-(--radius-panel) bg-surface p-5 ${accent ? `border-l-4 ${accent}` : ''}`}>
      <p className="text-caption text-ink-muted">{label}</p>
      <div className={accent ? '' : 'font-display text-2xl font-bold'}>{children}</div>
      {note && <p className="text-caption text-ink-muted">{note}</p>}
    </div>
  )
}
