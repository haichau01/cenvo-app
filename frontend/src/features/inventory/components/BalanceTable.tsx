import { Money } from '@/components/cenvo/Money'
import { StatusPill } from '@/components/cenvo/StatusPill'
import type { BalanceDto } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'

interface BalanceTableProps {
  rows: BalanceDto[]
  onSelect: (row: BalanceDto) => void
}

const th = 'px-4 py-3 text-left text-caption font-medium text-ink-muted'
const num = 'px-4 text-right tabular-nums'

/** Dense desktop table (44px rows). Clicking a row opens its stock card. */
export function BalanceTable({ rows, onSelect }: BalanceTableProps) {
  const totalValue = rows.reduce((sum, row) => sum + row.value, 0) // display only

  return (
    <div className="overflow-x-auto rounded-(--radius-panel) bg-surface">
      <table className="w-full text-table">
        <thead className="border-b border-line">
          <tr>
            <th className={th}>Mã</th>
            <th className={th}>Sản phẩm</th>
            <th className={th}>Kho</th>
            <th className={`${th} text-right`}>Tồn</th>
            <th className={`${th} text-right`}>Giá vốn</th>
            <th className={`${th} text-right`}>Giá trị</th>
            <th className={`${th} text-right`}>Tối thiểu</th>
            <th className={th}><span className="sr-only">Trạng thái</span></th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={`${row.warehouseId}-${row.productId}`} onClick={() => onSelect(row)}
              className="h-11 cursor-pointer border-b border-line last:border-0 hover:bg-concrete">
              <td className="px-4 text-ink-muted">{row.productCode}</td>
              <td className="px-4 font-medium">
                <button type="button" className="text-left hover:underline" onClick={() => onSelect(row)}>{row.productName}</button>
              </td>
              <td className="px-4">{row.warehouseName}</td>
              <td className={num}><span className="font-semibold">{formatQuantity(row.quantity)}</span> <span className="text-ink-muted">{row.unitName}</span></td>
              <td className={num}><Money value={row.averageCost} /></td>
              <td className={num}><Money value={row.value} /></td>
              <td className={`${num} text-ink-muted`}>{row.minStock > 0 ? formatQuantity(row.minStock) : '—'}</td>
              <td className="px-4">{row.belowMin && <StatusPill label="Dưới mức" tone="lane" />}</td>
            </tr>
          ))}
        </tbody>
        <tfoot className="border-t border-line">
          <tr className="h-11">
            <td className="px-4 font-medium" colSpan={5}>Tổng giá trị ({rows.length} dòng)</td>
            <td className={`${num} font-semibold`}><Money value={totalValue} /></td>
            <td colSpan={2} />
          </tr>
        </tfoot>
      </table>
    </div>
  )
}
