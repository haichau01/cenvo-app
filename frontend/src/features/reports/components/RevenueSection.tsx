import { useState, type ReactNode } from 'react'
import { Button } from '@/components/ui/Button'
import type { ChartDay, ChartSeries } from '../lib/chart'
import { Panel } from './ReportTable'
import { RevenueChart } from './RevenueChart'

interface RevenueSectionProps {
  days: ChartDay[]
  series: ChartSeries[]
  /** Exact-number table of the same days — the accessible alternative to the chart. */
  table: ReactNode
}

/** Chart for a range of days; a single day has nothing to compare, so the tiles above already say it all. */
export function RevenueSection({ days, series, table }: RevenueSectionProps) {
  const [asTable, setAsTable] = useState(false)
  if (days.length < 2) return null

  return (
    <Panel title="Doanh thu theo ngày" description="Rê chuột hoặc dùng phím Tab trên cột để xem số chi tiết"
      actions={<Button variant="secondary" onClick={() => setAsTable(!asTable)} aria-pressed={asTable}>
        {asTable ? 'Xem biểu đồ' : 'Xem dạng bảng'}
      </Button>}>
      {asTable ? table : <RevenueChart days={days} series={series} />}
    </Panel>
  )
}
