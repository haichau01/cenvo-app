import { Money } from '@/components/cenvo/Money'
import type { ChartDay } from '../lib/chart'

const WIDTH = 220
const LINE_HEIGHT = 22
const dateFormat = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })

interface ChartTooltipProps {
  day: ChartDay
  /** Anchor in chart pixels (center top of the bar). */
  x: number
  y: number
  containerWidth: number
}

/** Exact numbers for one day; the axis only shows rounded amounts. Clamped so it never leaves the chart. */
export function ChartTooltip({ day, x, y, containerWidth }: ChartTooltipProps) {
  const left = Math.min(Math.max(x - WIDTH / 2, 0), containerWidth - WIDTH)
  const height = (day.details.length + 1) * LINE_HEIGHT + 24
  return (
    <div role="status" style={{ left, top: Math.max(y - height, 0), width: WIDTH }}
      className="pointer-events-none absolute rounded-(--radius-control) bg-surface p-3 text-table shadow-overlay">
      <p className="mb-1 font-semibold">{dateFormat.format(new Date(`${day.date}T00:00:00Z`))}</p>
      {day.details.map((detail, index) => (
        <div key={detail.label} className="flex justify-between gap-3">
          <span className="text-ink-muted">{detail.label}</span>
          {detail.kind === 'money'
            ? <Money value={detail.value} className={index === 0 ? 'font-semibold' : ''} />
            : <span className="tabular-nums">{detail.value}</span>}
        </div>
      ))}
    </div>
  )
}
