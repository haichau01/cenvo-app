import { useState } from 'react'
import { formatCompact, formatVnd } from '@/lib/format'
import { useElementWidth } from '@/lib/useElementWidth'
import { columnTotal, type ChartDay, type ChartSeries } from '../lib/chart'
import { niceTicks } from '../lib/metrics'
import { ChartTooltip } from './ChartTooltip'

const HEIGHT = 260
const MARGIN = { top: 12, right: 8, bottom: 28, left: 56 }
const MAX_BAR = 28
const MIN_LABEL_SPACING = 48
const RADIUS = 4
const SEGMENT_GAP = 2

/**
 * Revenue per day. One series needs no legend (the section title names it); two are stacked bottom-up in series order
 * with a 2px surface gap and a legend, so identity never rests on colour alone.
 */
export function RevenueChart({ days, series }: { days: ChartDay[]; series: ChartSeries[] }) {
  const [containerRef, width] = useElementWidth<HTMLDivElement>()
  const [active, setActive] = useState<number | null>(null)
  const plotWidth = Math.max(0, width - MARGIN.left - MARGIN.right)
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const ticks = niceTicks(Math.max(...days.map(columnTotal), 0))
  const top = ticks[ticks.length - 1]
  const band = days.length > 0 ? plotWidth / days.length : 0
  const barWidth = Math.max(2, Math.min(MAX_BAR, band - 2)) // ≥ 2px surface gap between neighbours
  const labelEvery = Math.max(1, Math.ceil(MIN_LABEL_SPACING / Math.max(band, 1)))
  const y = (value: number) => MARGIN.top + plotHeight - (value / top) * plotHeight

  return (
    <div>
      {series.length > 1 && <Legend series={series} />}
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
                  <Column day={day} series={series} x={x + (band - barWidth) / 2} width={barWidth} y={y} />
                  {index % labelEvery === 0 && (
                    <text x={x + band / 2} y={HEIGHT - 8} textAnchor="middle" className="fill-ink-muted text-caption tabular-nums">
                      {day.date.slice(8, 10)}/{day.date.slice(5, 7)}
                    </text>
                  )}
                  {/* Hit target: the whole column, bigger than the bar; focusable so keyboard users get the tooltip too. */}
                  <rect x={x} y={MARGIN.top} width={band} height={plotHeight} fill="transparent" tabIndex={0}
                    aria-label={`${day.date}: ${day.details.map((detail) => `${detail.label} ${detailText(detail)}`).join(', ')}`}
                    onMouseEnter={() => setActive(index)} onFocus={() => setActive(index)} onBlur={() => setActive(null)}
                    className="outline-none focus-visible:stroke-foam focus-visible:stroke-2" />
                </g>
              )
            })}
            <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(0)} y2={y(0)} className="stroke-ink-muted" strokeWidth={1} />
          </svg>
        )}
        {active !== null && days[active] && (
          <ChartTooltip day={days[active]} x={MARGIN.left + (active + 0.5) * band} y={y(columnTotal(days[active]))} containerWidth={width} />
        )}
      </div>
    </div>
  )
}

const detailText = (detail: ChartDay['details'][number]) => (detail.kind === 'money' ? formatVnd(detail.value) : String(detail.value))

interface ColumnProps {
  day: ChartDay
  series: ChartSeries[]
  x: number
  width: number
  y: (value: number) => number
}

/** Stacked segments; only the top one gets the rounded data end, lower ones are separated by a surface gap. */
function Column({ day, series, x, width, y }: ColumnProps) {
  const topIndex = day.values.findLastIndex((value) => value > 0)
  let base = 0
  return day.values.map((value, index) => {
    const bottom = base
    base += value
    if (value <= 0) return null
    const gap = index > 0 && bottom > 0 ? SEGMENT_GAP : 0
    const height = y(bottom) - y(base) - gap
    return <path key={series[index].label} d={barPath(x, y(base), width, height, index === topIndex)} className={series[index].fill} />
  })
}

function Legend({ series }: { series: ChartSeries[] }) {
  return (
    <ul className="mb-2 flex gap-4 text-caption text-ink-muted" aria-label="Chú giải">
      {series.map((item) => (
        <li key={item.label} className="flex items-center gap-1.5">
          <svg width={10} height={10} aria-hidden><rect width={10} height={10} rx={2} className={item.fill} /></svg>
          {item.label}
        </li>
      ))}
    </ul>
  )
}

/** Bar segment on its base; the top segment has 4px rounded data end. */
function barPath(x: number, y: number, width: number, height: number, rounded: boolean): string {
  if (height <= 0) return ''
  const r = rounded ? Math.min(RADIUS, width / 2, height) : 0
  return `M${x},${y + height} V${y + r} Q${x},${y} ${x + r},${y} H${x + width - r} Q${x + width},${y} ${x + width},${y + r} V${y + height} Z`
}
