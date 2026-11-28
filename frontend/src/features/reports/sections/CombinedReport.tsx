import { Money } from '@/components/cenvo/Money'
import type { SalesSummary, WashSalesSummary } from '@/lib/api/types'
import { useSalesSummary, useWashSalesSummary } from '../api/reports'
import { Panel, ReportTable, th, thNum, tdNum } from '../components/ReportTable'
import { RevenueSection } from '../components/RevenueSection'
import { PaymentTable } from '../components/SalesTables'
import { Tile, TileRow } from '../components/StatTiles'
import { CAFE_SERIES, columnTotal, combinedChartDays, WASH_SERIES, type ChartDay } from '../lib/chart'
import { marginPercent, sumByMethod } from '../lib/metrics'
import { daysOf, type Period } from '../lib/period'
import { ReportState } from './ReportState'

/** Whole shop: both modules side by side. Totals are only added up for display; each module's report holds the detail. */
export function CombinedReport({ period }: { period: Period }) {
  const cafe = useSalesSummary(period)
  const wash = useWashSalesSummary(period)
  return (
    <ReportState query={cafe}>
      {(cafeData) => (
        <ReportState query={wash}>
          {(washData) => <CombinedBody cafe={cafeData} wash={washData} period={period} />}
        </ReportState>
      )}
    </ReportState>
  )
}

function CombinedBody({ cafe, wash, period }: { cafe: SalesSummary; wash: WashSalesSummary; period: Period }) {
  const revenue = cafe.revenue + wash.revenue
  const grossProfit = cafe.grossProfit + wash.grossProfit
  const margin = marginPercent(grossProfit, revenue)
  const days = combinedChartDays(cafe.daily, wash.daily, daysOf(period))
  return (
    <>
      <TileRow>
        <Tile label="Tổng doanh thu" accent="border-ink"><Money value={revenue} className="amount text-amount" /></Tile>
        <Tile label="Cà phê" note={`${cafe.orders} hóa đơn`}><Money value={cafe.revenue} /></Tile>
        <Tile label="Rửa xe" note={`${wash.tickets} xe · gồm bán gói`}><Money value={wash.revenue} /></Tile>
        <Tile label="Lãi gộp" note={margin === null ? undefined : `Biên lãi gộp ${margin}%`}><Money value={grossProfit} /></Tile>
      </TileRow>
      <RevenueSection days={days} series={[CAFE_SERIES, WASH_SERIES]} table={<CombinedDailyTable days={days} />} />
      <div className="grid grid-cols-2 gap-6">
        <Panel title="Theo phân hệ"><ModuleTable cafe={cafe} wash={wash} /></Panel>
        <Panel title="Hình thức thanh toán"><PaymentTable byMethod={sumByMethod(cafe.byMethod, wash.byMethod)} /></Panel>
      </div>
    </>
  )
}

function ModuleTable({ cafe, wash }: { cafe: SalesSummary; wash: WashSalesSummary }) {
  const rows = [
    { name: 'Cà phê', revenue: cafe.revenue, cost: cafe.cost, grossProfit: cafe.grossProfit },
    { name: 'Rửa xe', revenue: wash.revenue, cost: wash.supplyCost + wash.commission, grossProfit: wash.grossProfit },
  ]
  return (
    <ReportTable head={<>
      <th className={th}>Phân hệ</th><th className={thNum}>Doanh thu</th><th className={thNum}>Chi phí trực tiếp</th><th className={thNum}>Lãi gộp</th>
    </>}>
      {rows.map((row) => (
        <tr key={row.name}>
          <td className="px-4 font-medium">{row.name}</td>
          <td className={tdNum}><Money value={row.revenue} /></td>
          <td className={tdNum}><Money value={row.cost} /></td>
          <td className={tdNum}><Money value={row.grossProfit} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

const dayFormat = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', timeZone: 'UTC' })

/** Table view of the stacked chart. */
function CombinedDailyTable({ days }: { days: ChartDay[] }) {
  return (
    <ReportTable head={<><th className={th}>Ngày</th><th className={thNum}>Cà phê</th><th className={thNum}>Rửa xe</th><th className={thNum}>Tổng</th></>}>
      {days.map((day) => (
        <tr key={day.date}>
          <td className="px-4 tabular-nums">{dayFormat.format(new Date(`${day.date}T00:00:00Z`))}</td>
          <td className={tdNum}><Money value={day.values[0]} /></td>
          <td className={tdNum}><Money value={day.values[1]} /></td>
          <td className={`${tdNum} font-semibold`}><Money value={columnTotal(day)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}
