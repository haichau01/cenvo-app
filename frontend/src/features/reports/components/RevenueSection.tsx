import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import type { SalesSummary } from '@/lib/api/types'
import { fillDays } from '../lib/metrics'
import { daysOf, type Period } from '../lib/period'
import { Panel } from './ReportTable'
import { RevenueChart } from './RevenueChart'
import { DailyTable } from './SalesTables'

/** Chart for a range of days; a single day has nothing to compare, so the tiles above already say it all. */
export function RevenueSection({ summary, period }: { summary: SalesSummary; period: Period }) {
  const [asTable, setAsTable] = useState(false)
  const days = fillDays(summary.daily, daysOf(period))
  if (days.length < 2) return null

  return (
    <Panel title="Doanh thu theo ngày" description="Rê chuột hoặc dùng phím Tab trên cột để xem số chi tiết"
      actions={<Button variant="secondary" onClick={() => setAsTable(!asTable)} aria-pressed={asTable}>
        {asTable ? 'Xem biểu đồ' : 'Xem dạng bảng'}
      </Button>}>
      {asTable ? <DailyTable days={days} /> : <RevenueChart days={days} />}
    </Panel>
  )
}
