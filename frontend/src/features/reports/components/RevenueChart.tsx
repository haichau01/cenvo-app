import { useState } from 'react'
import type { DailyRevenue } from '@/lib/api/types'
import { formatCompact } from '@/lib/format'
import { useElementWidth } from '@/lib/useElementWidth'
import { niceTicks } from '../lib/metrics'
import { ChartTooltip } from './ChartTooltip'

const HEIGHT = 260
const MARGIN = { top: 12, right: 8, bottom: 28, left: 56 }
const MAX_BAR = 28
const MIN_LABEL_SPACING = 48
const RADIUS = 4

/** Daily revenue — one series (so no legend: the section title names it), robusta = the Cafe module. */
export function RevenueChart({ days }: { days: DailyRevenue[] }) {
  const [containerRef, width] = useElementWidth<HTMLDivElement>()
  const [active, setActive] = useState<number | null>(null)
  const plotWidth = Math.max(0, width - MARGIN.left - MARGIN.right)
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const ticks = niceTicks(Math.max(...days.map((d) => d.revenue), 0))
  const top = ticks[ticks.length - 1]
  const band = days.length > 0 ? plotWidth / days.length : 0
  const barWidth = Math.max(2, Math.min(MAX_BAR, band - 2)) // ≥ 2px surface gap between neighbours
  const labelEvery = Math.max(1, Math.ceil(MIN_LABEL_SPACING / Math.max(band, 1)))
  const y = (value: number) => MARGIN.top + plotHeight - (value / top) * plotHeight

  return (
    <div ref={containerRef} className="relative" onMouseLeave={() => setActive(null)}>
      {width > 0 && (
        <svg width={width} height={HEIGHT} role="img" aria-label="Biểu đồ doanh thu theo ngày">
          {ticks.map((tick) => (
            <g key={tick}>
              <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(tick)} y2={y(tick)} className="stroke-line" strokeWidth={1} />
              <text x={MARGIN.left - 8} y={y(tick)} dy="0.32em" textAnchor="end" className="fill-ink-muted text-caption tabular-nums">
                {formatCompact(tick)}
              </text>
            </g>
          ))}
          {days.map((day, index) => {
            const x = MARGIN.left + index * band
            return (
              <g key={day.date}>
                {active === index && <rect x={x} y={MARGIN.top} width={band} height={plotHeight} className="fill-concrete" />}
                <path d={barPath(x + (band - barWidth) / 2, y(day.revenue), barWidth, y(0) - y(day.revenue))} className="fill-robusta" />
                {index % labelEvery === 0 && (
                  <text x={x + band / 2} y={HEIGHT - 8} textAnchor="middle" className="fill-ink-muted text-caption tabular-nums">
                    {day.date.slice(8, 10)}/{day.date.slice(5, 7)}
                  </text>
                )}
                {/* Hit target: the whole column, bigger than the bar; focusable so keyboard users get the tooltip too. */}
                <rect x={x} y={MARGIN.top} width={band} height={plotHeight} fill="transparent" tabIndex={0}
                  aria-label={`${day.date}: doanh thu ${day.revenue} đồng, ${day.orders} hóa đơn`}
                  onMouseEnter={() => setActive(index)} onFocus={() => setActive(index)} onBlur={() => setActive(null)}
                  className="outline-none focus-visible:stroke-foam focus-visible:stroke-2" />
              </g>
            )
          })}
          <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(0)} y2={y(0)} className="stroke-ink-muted" strokeWidth={1} />
        </svg>
      )}
      {active !== null && days[active] && (
        <ChartTooltip day={days[active]} x={MARGIN.left + (active + 0.5) * band} y={y(days[active].revenue)} containerWidth={width} />
      )}
    </div>
  )
}

/** Bar with 4px rounded data end (top) and a square base sitting on the baseline. */
function barPath(x: number, y: number, width: number, height: number): string {
  if (height <= 0) return ''
  const r = Math.min(RADIUS, width / 2, height)
  return `M${x},${y + height} V${y + r} Q${x},${y} ${x + r},${y} H${x + width - r} Q${x + width},${y} ${x + width},${y + r} V${y + height} Z`
}
