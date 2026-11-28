import { Money } from '@/components/cenvo/Money'
import type { DailyRevenue } from '@/lib/api/types'

const WIDTH = 220
const dateFormat = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })

interface ChartTooltipProps {
  day: DailyRevenue
  /** Anchor in chart pixels (center top of the bar). */
  x: number
  y: number
  containerWidth: number
}

/** Exact numbers for one day; the axis only shows rounded amounts. Clamped so it never leaves the chart. */
export function ChartTooltip({ day, x, y, containerWidth }: ChartTooltipProps) {
  const left = Math.min(Math.max(x - WIDTH / 2, 0), containerWidth - WIDTH)
  return (
    <div role="status" style={{ left, top: Math.max(y - 128, 0), width: WIDTH }}
      className="pointer-events-none absolute rounded-(--radius-control) bg-surface p-3 text-table shadow-overlay">
      <p className="mb-1 font-semibold">{dateFormat.format(new Date(`${day.date}T00:00:00Z`))}</p>
      <Row label="Doanh thu"><Money value={day.revenue} className="font-semibold" /></Row>
      <Row label="Giá vốn"><Money value={day.cost} /></Row>
      <Row label="Lãi gộp"><Money value={day.grossProfit} /></Row>
      <Row label="Hóa đơn"><span className="tabular-nums">{day.orders}</span></Row>
    </div>
  )
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-3">
      <span className="text-ink-muted">{label}</span>
      {children}
    </div>
  )
}
