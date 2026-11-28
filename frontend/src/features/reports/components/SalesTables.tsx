import { Money } from '@/components/cenvo/Money'
import type { DailyRevenue, SalesSummary, TopItem } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import { share } from '../lib/metrics'
import { ReportTable, ShareBar, th, thNum, tdNum } from './ReportTable'

const METHOD_LABELS: Record<string, string> = { Cash: 'Tiền mặt', Transfer: 'Chuyển khoản', Card: 'Thẻ' }
const dayFormat = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', timeZone: 'UTC' })

/** Table view of the revenue chart — the accessible / exact-number alternative. */
export function DailyTable({ days }: { days: DailyRevenue[] }) {
  return (
    <ReportTable head={<>
      <th className={th}>Ngày</th><th className={thNum}>Hóa đơn</th><th className={thNum}>Doanh thu</th>
      <th className={thNum}>Giảm giá</th><th className={thNum}>Giá vốn</th><th className={thNum}>Lãi gộp</th>
    </>}>
      {days.map((day) => (
        <tr key={day.date}>
          <td className="px-4 tabular-nums">{dayFormat.format(new Date(`${day.date}T00:00:00Z`))}</td>
          <td className={tdNum}>{day.orders}</td>
          <td className={`${tdNum} font-semibold`}><Money value={day.revenue} /></td>
          <td className={tdNum}><Money value={day.discount} /></td>
          <td className={tdNum}><Money value={day.cost} /></td>
          <td className={tdNum}><Money value={day.grossProfit} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

export function PaymentTable({ summary }: { summary: SalesSummary }) {
  const methods = Object.entries(summary.byMethod) as [string, number][]
  const total = methods.reduce((sum, [, amount]) => sum + amount, 0)
  return (
    <ReportTable head={<><th className={th}>Hình thức</th><th className={thNum}>Số tiền</th><th className={thNum}>Tỷ trọng</th></>}>
      {methods.map(([method, amount]) => (
        <tr key={method}>
          <td className="px-4">{METHOD_LABELS[method] ?? method}</td>
          <td className={tdNum}><Money value={amount} /></td>
          <td className="px-4"><ShareBar value={share(amount, total)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

export function TopItemsTable({ items, revenue }: { items: TopItem[]; revenue: number }) {
  if (items.length === 0) return <p className="py-6 text-center text-ink-muted">Chưa bán món nào trong khoảng này.</p>
  return (
    <ReportTable head={<>
      <th className={`${th} w-10`}>#</th><th className={th}>Món</th><th className={thNum}>Số lượng</th>
      <th className={thNum}>Doanh thu</th><th className={thNum}>Tỷ trọng doanh thu</th>
    </>}>
      {items.map((item, index) => (
        <tr key={item.productId + item.name}>
          <td className="px-4 tabular-nums text-ink-muted">{index + 1}</td>
          <td className="px-4 font-medium">{item.name}</td>
          <td className={tdNum}>{formatQuantity(item.quantity)}</td>
          <td className={tdNum}><Money value={item.revenue} /></td>
          <td className="px-4"><ShareBar value={share(item.revenue, revenue)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}
