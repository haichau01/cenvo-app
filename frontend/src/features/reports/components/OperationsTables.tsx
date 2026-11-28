import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import type { ConsumptionDto, ShiftDto } from '@/lib/api/types'
import { formatQuantity, formatShortDateTime } from '@/lib/format'
import { shiftDifference } from '../lib/metrics'
import { ReportTable, th, thNum, tdNum } from './ReportTable'

const COLLAPSED_ROWS = 10

/** Cash reconciliation per shift. Missing cash is the number a manager must see first. */
export function ShiftsTable({ shifts }: { shifts: ShiftDto[] }) {
  if (shifts.length === 0) return <p className="py-6 text-center text-ink-muted">Không có ca nào mở trong khoảng này.</p>
  return (
    <ReportTable head={<>
      <th className={th}>Ca</th><th className={th}>Thu ngân</th><th className={th}>Mở – đóng</th>
      <th className={thNum}>Đầu ca</th><th className={thNum}>Phải có</th><th className={thNum}>Đếm được</th><th className={thNum}>Chênh lệch</th>
    </>}>
      {shifts.map((shift) => (
        <tr key={shift.id}>
          <td className="px-4 font-semibold tabular-nums">{shift.code}</td>
          <td className="px-4">{shift.cashierName}</td>
          <td className="px-4 tabular-nums">{formatShortDateTime(shift.openedAt)} – {shift.closedAt ? formatShortDateTime(shift.closedAt) : <StatusPill label="Đang mở" tone="lane" />}</td>
          <td className={tdNum}><Money value={shift.openingCash} /></td>
          <td className={tdNum}>{shift.expectedCash == null ? '—' : <Money value={shift.expectedCash} />}</td>
          <td className={tdNum}>{shift.countedCash == null ? '—' : <Money value={shift.countedCash} />}</td>
          <td className={tdNum}><Difference value={shiftDifference(shift)} /></td>
        </tr>
      ))}
    </ReportTable>
  )
}

function Difference({ value }: { value: number | null }) {
  if (value === null) return <>—</>
  if (value === 0) return <span className="text-go">Khớp</span>
  if (value > 0) return <span className="font-semibold">Dư <Money value={value} /></span>
  return <span className="font-semibold text-stop">Thiếu <Money value={-value} /></span>
}

/** Ingredients used by sales (net of cancelled orders), most expensive first. */
export function ConsumptionTable({ rows }: { rows: ConsumptionDto[] }) {
  const [expanded, setExpanded] = useState(false)
  if (rows.length === 0) return <p className="py-6 text-center text-ink-muted">Chưa tiêu hao nguyên liệu nào trong khoảng này.</p>
  const visible = expanded ? rows : rows.slice(0, COLLAPSED_ROWS)
  return (
    <>
      <ReportTable head={<><th className={th}>Nguyên liệu</th><th className={thNum}>Đã dùng</th><th className={thNum}>Giá vốn</th></>}>
        {visible.map((row) => (
          <tr key={row.productId}>
            <td className="px-4"><span className="text-ink-muted">{row.productCode}</span> · <span className="font-medium">{row.productName}</span></td>
            <td className={tdNum}>{formatQuantity(row.quantity)} <span className="text-ink-muted">{row.unitName}</span></td>
            <td className={tdNum}><Money value={row.cost} /></td>
          </tr>
        ))}
      </ReportTable>
      {rows.length > COLLAPSED_ROWS && (
        <Button variant="ghost" className="mt-2" onClick={() => setExpanded(!expanded)}>
          {expanded ? 'Thu gọn' : `Xem tất cả ${rows.length} nguyên liệu`}
        </Button>
      )}
    </>
  )
}
