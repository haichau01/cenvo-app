import { Money } from '@/components/cenvo/Money'
import type { WashDailyRevenue, WashSalesSummary, WashServiceSales, WashVehicleSales } from '@/lib/api/types'
import { formatVnd } from '@/lib/format'
import { vehicleLabel } from '@/lib/vehicle'
import { marginPercent, share } from '../lib/metrics'
import { ReportTable, ShareBar, th, thNum, tdNum } from './ReportTable'
import { Tile, TileRow } from './StatTiles'

const dayFormat = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', timeZone: 'UTC' })

export function WashStatTiles({ summary }: { summary: WashSalesSummary }) {
  const margin = marginPercent(summary.grossProfit, summary.revenue)
  return (
    <TileRow>
      <Tile label="Doanh thu" accent="border-foam" note={`Rửa xe ${formatVnd(summary.washRevenue)} · Bán gói ${formatVnd(summary.membershipRevenue)}`}>
        <Money value={summary.revenue} className="amount text-amount" />
      </Tile>
      <Tile label="Lãi gộp" note={margin === null ? 'Doanh thu − hóa chất − hoa hồng' : `Biên lãi gộp ${margin}% · sau hóa chất và hoa hồng`}>
        <Money value={summary.grossProfit} />
      </Tile>
      <Tile label="Hóa chất + hoa hồng" note={`Hóa chất ${formatVnd(summary.supplyCost)} · Hoa hồng ${formatVnd(summary.commission)}`}>
        <Money value={summary.supplyCost + summary.commission} />
      </Tile>
      <Tile label="Xe đã thu tiền" note={`Trung bình ${formatVnd(summary.averageTicket)}/xe · Hủy ${summary.cancelled}`}>
        <span className="tabular-nums">{summary.tickets}</span>
      </Tile>
    </TileRow>
  )
}

/** Packages: money taken now vs. washes paid by packages sold earlier (not money of this period). */
export function MembershipTable({ summary }: { summary: WashSalesSummary }) {
  return (
    <ReportTable head={<><th className={th}>Gói thành viên</th><th className={thNum}>Số lượng</th><th className={thNum}>Giá trị</th></>}>
      <tr>
        <td className="px-4">Gói bán ra (tiền thu trong kỳ)</td>
        <td className={tdNum}>{summary.membershipsSold}</td>
        <td className={tdNum}><Money value={summary.membershipRevenue} /></td>
      </tr>
      <tr>
        <td className="px-4">Lượt rửa trừ vào gói (theo giá niêm yết)</td>
        <td className={tdNum}>{summary.membershipUses}</td>
        <td className={tdNum}><Money value={summary.membershipCovered} /></td>
      </tr>
    </ReportTable>
  )
}

export function ServiceTable({ services, revenue }: { services: WashServiceSales[]; revenue: number }) {
  if (services.length === 0) return <p className="py-6 text-center text-ink-muted">Chưa có xe nào thu tiền trong khoảng này.</p>
  return (
    <ReportTable head={<>
      <th className={th}>Dịch vụ</th><th className={thNum}>Số lượt</th><th className={thNum}>Trừ gói</th>
      <th className={thNum}>Doanh thu</th><th className={thNum}>Tỷ trọng doanh thu</th>
    </>}>
      {services.map((service) => (
        <tr key={service.offeringId + service.name}>
          <td className="px-4 font-medium">{service.name}</td>
          <td className={tdNum}>{service.count}</td>
          <td className={tdNum}>{service.membershipUses}</td>
          <td className={tdNum}><Money value={service.revenue} /></td>
          <td className="px-4"><ShareBar value={share(service.revenue, revenue)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

export function VehicleTable({ vehicles }: { vehicles: WashVehicleSales[] }) {
  const total = vehicles.reduce((sum, vehicle) => sum + vehicle.tickets, 0)
  return (
    <ReportTable head={<><th className={th}>Loại xe</th><th className={thNum}>Số xe</th><th className={thNum}>Doanh thu</th><th className={thNum}>Tỷ trọng xe</th></>}>
      {vehicles.map((vehicle) => (
        <tr key={vehicle.vehicleType}>
          <td className="px-4">{vehicleLabel(vehicle.vehicleType)}</td>
          <td className={tdNum}>{vehicle.tickets}</td>
          <td className={tdNum}><Money value={vehicle.revenue} /></td>
          <td className="px-4"><ShareBar value={share(vehicle.tickets, total)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

export function WashDailyTable({ days }: { days: WashDailyRevenue[] }) {
  return (
    <ReportTable head={<>
      <th className={th}>Ngày</th><th className={thNum}>Xe</th><th className={thNum}>Doanh thu</th><th className={thNum}>Bán gói</th>
      <th className={thNum}>Hóa chất</th><th className={thNum}>Hoa hồng</th><th className={thNum}>Lãi gộp</th>
    </>}>
      {days.map((day) => (
        <tr key={day.date}>
          <td className="px-4 tabular-nums">{dayFormat.format(new Date(`${day.date}T00:00:00Z`))}</td>
          <td className={tdNum}>{day.tickets}</td>
          <td className={`${tdNum} font-semibold`}><Money value={day.revenue} /></td>
          <td className={tdNum}><Money value={day.membershipRevenue} /></td>
          <td className={tdNum}><Money value={day.supplyCost} /></td>
          <td className={tdNum}><Money value={day.commission} /></td>
          <td className={tdNum}><Money value={day.grossProfit} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}
