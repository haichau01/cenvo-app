import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { Button } from '@/components/ui/Button'
import { useConsumption, useSalesSummary, useShifts } from '../api/reports'
import { ConsumptionTable, ShiftsTable } from '../components/OperationsTables'
import { PeriodPicker } from '../components/PeriodPicker'
import { Panel } from '../components/ReportTable'
import { RevenueSection } from '../components/RevenueSection'
import { PaymentTable, TopItemsTable } from '../components/SalesTables'
import { StatTiles } from '../components/StatTiles'
import { todayInVietnam, type Period } from '../lib/period'

/** Daily/period sales report for managers: headline numbers, revenue by day, best sellers, shifts, ingredient use. */
export function ReportsPage() {
  const [today] = useState(() => todayInVietnam())
  const [period, setPeriod] = useState<Period>({ from: today, to: today })
  const summary = useSalesSummary(period)
  const shifts = useShifts(period)
  const consumption = useConsumption(period)

  return (
    <>
      <PageHeader title="Báo cáo bán hàng" description="Cà phê · tính theo hóa đơn đã thanh toán, giờ Việt Nam." />
      <PeriodPicker value={period} today={today} onChange={setPeriod} />
      {summary.isPending && <div className="h-40 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {summary.isError && <EmptyState title="Không tải được báo cáo" description={summary.error.message}
        action={<Button variant="secondary" onClick={() => summary.refetch()}>Tải lại</Button>} />}
      {summary.data && (
        <div className={`flex flex-col gap-6 transition-opacity ${summary.isPlaceholderData ? 'opacity-60' : ''}`}>
          <StatTiles summary={summary.data} />
          <RevenueSection summary={summary.data} period={period} />
          <div className="grid grid-cols-[3fr_2fr] gap-6">
            <Panel title="Món bán chạy" description="10 món bán nhiều nhất theo số lượng">
              <TopItemsTable items={summary.data.topItems} revenue={summary.data.revenue} />
            </Panel>
            <Panel title="Hình thức thanh toán"><PaymentTable summary={summary.data} /></Panel>
          </div>
          <Panel title="Đối soát ca" description="Tiền mặt phải có = tiền đầu ca + tiền mặt thu trong ca">
            {shifts.data ? <ShiftsTable shifts={shifts.data} /> : <p className="text-ink-muted">Đang tải…</p>}
          </Panel>
          <Panel title="Nguyên liệu đã dùng" description="Trừ kho theo công thức khi bán, đã cộng lại phần hóa đơn bị hủy">
            {consumption.data ? <ConsumptionTable rows={consumption.data} /> : <p className="text-ink-muted">Đang tải…</p>}
          </Panel>
        </div>
      )}
    </>
  )
}
