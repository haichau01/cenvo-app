import { useWashSalesSummary } from '../api/reports'
import { Panel } from '../components/ReportTable'
import { RevenueSection } from '../components/RevenueSection'
import { PaymentTable } from '../components/SalesTables'
import { MembershipTable, ServiceTable, VehicleTable, WashDailyTable, WashStatTiles } from '../components/WashTables'
import { fillWashDays, WASH_SERIES, washChartDays } from '../lib/chart'
import { daysOf, type Period } from '../lib/period'
import { ReportState } from './ReportState'

/** Car wash: money taken (washes + packages), supplies and commission, by service and vehicle type. */
export function WashReport({ period }: { period: Period }) {
  const summary = useWashSalesSummary(period)
  const days = daysOf(period)

  return (
    <ReportState query={summary}>
      {(data) => (
        <>
          <WashStatTiles summary={data} />
          <RevenueSection days={washChartDays(data.daily, days)} series={[WASH_SERIES]}
            table={<WashDailyTable days={fillWashDays(data.daily, days)} />} />
          <div className="grid grid-cols-[3fr_2fr] gap-6">
            <Panel title="Theo dịch vụ" description="Lượt trừ gói được tính số lượt, doanh thu bằng 0">
              <ServiceTable services={data.byService} revenue={data.washRevenue} />
            </Panel>
            <Panel title="Hình thức thanh toán" description="Gồm tiền rửa xe và tiền bán gói"><PaymentTable byMethod={data.byMethod} /></Panel>
          </div>
          <div className="grid grid-cols-2 gap-6">
            <Panel title="Gói thành viên"><MembershipTable summary={data} /></Panel>
            <Panel title="Theo loại xe"><VehicleTable vehicles={data.byVehicle} /></Panel>
          </div>
        </>
      )}
    </ReportState>
  )
}
