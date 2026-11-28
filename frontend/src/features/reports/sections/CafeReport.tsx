import { useConsumption, useSalesSummary, useShifts } from '../api/reports'
import { ConsumptionTable, ShiftsTable } from '../components/OperationsTables'
import { Panel } from '../components/ReportTable'
import { RevenueSection } from '../components/RevenueSection'
import { DailyTable, PaymentTable, TopItemsTable } from '../components/SalesTables'
import { StatTiles } from '../components/StatTiles'
import { CAFE_SERIES, cafeChartDays, fillCafeDays } from '../lib/chart'
import { daysOf, type Period } from '../lib/period'
import { ReportState } from './ReportState'

/** Café: headline numbers, revenue by day, best sellers, payment methods, shifts, ingredient use. */
export function CafeReport({ period }: { period: Period }) {
  const summary = useSalesSummary(period)
  const shifts = useShifts(period)
  const consumption = useConsumption(period)
  const days = daysOf(period)

  return (
    <ReportState query={summary}>
      {(data) => (
        <>
          <StatTiles summary={data} />
          <RevenueSection days={cafeChartDays(data.daily, days)} series={[CAFE_SERIES]}
            table={<DailyTable days={fillCafeDays(data.daily, days)} />} />
          <div className="grid grid-cols-[3fr_2fr] gap-6">
            <Panel title="Món bán chạy" description="10 món bán nhiều nhất theo số lượng">
              <TopItemsTable items={data.topItems} revenue={data.revenue} />
            </Panel>
            <Panel title="Hình thức thanh toán"><PaymentTable byMethod={data.byMethod} /></Panel>
          </div>
          <Panel title="Đối soát ca" description="Tiền mặt phải có = tiền đầu ca + tiền mặt thu trong ca (gồm cả tiền rửa xe và bán gói)">
            {shifts.data ? <ShiftsTable shifts={shifts.data} /> : <p className="text-ink-muted">Đang tải…</p>}
          </Panel>
          <Panel title="Nguyên liệu đã dùng" description="Trừ kho theo công thức khi bán, đã cộng lại phần hóa đơn bị hủy">
            {consumption.data ? <ConsumptionTable rows={consumption.data} /> : <p className="text-ink-muted">Đang tải…</p>}
          </Panel>
        </>
      )}
    </ReportState>
  )
}
