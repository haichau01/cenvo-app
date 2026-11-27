import { Money } from '@/components/cenvo/Money'
import type { StockDocumentDto, StockDocumentLineDto } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import type { DocumentTypeConfig } from '../lib/documentTypes'

const th = 'px-3 py-2 text-left text-caption font-medium text-ink-muted'
const num = 'px-3 text-right tabular-nums'

/** Read-only lines. A posted stocktake also shows book quantity and the difference it adjusted. */
export function DocumentLinesTable({ doc, config }: { doc: StockDocumentDto; config: DocumentTypeConfig }) {
  const isStocktake = doc.type === 'Stocktake'
  const showMoney = !isStocktake // a stocktake adjusts quantities; its value lives in the adjustment movements
  return (
    <table className="w-full text-table">
      <thead className="border-b border-line">
        <tr>
          <th className={th}>Sản phẩm</th>
          <th className={th}>Đơn vị</th>
          {isStocktake && <th className={`${th} text-right`}>Sổ sách</th>}
          <th className={`${th} text-right`}>{config.quantityLabel}</th>
          {isStocktake && <th className={`${th} text-right`}>Chênh lệch</th>}
          {showMoney && <th className={`${th} text-right`}>Đơn giá</th>}
          {showMoney && <th className={`${th} text-right`}>Thành tiền</th>}
          <th className={th}>Ghi chú</th>
        </tr>
      </thead>
      <tbody>
        {(doc.lines ?? []).map((line) => (
          <tr key={line.id} className="h-11 border-b border-line last:border-0">
            <td className="px-3"><span className="text-ink-muted">{line.productCode}</span> · <span className="font-medium">{line.productName}</span></td>
            <td className="px-3">{line.unitName}</td>
            {isStocktake && <td className={num}>{line.systemQuantity == null ? '—' : formatQuantity(line.systemQuantity)}</td>}
            <td className={`${num} font-semibold`}>{formatQuantity(line.quantity)}</td>
            {isStocktake && <td className={num}><Difference line={line} /></td>}
            {showMoney && <td className={num}><Money value={line.unitPrice} /></td>}
            {showMoney && <td className={num}><Money value={line.amount} /></td>}
            <td className="px-3 text-ink-muted">{line.note}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

/** Counted − book, in base units. Shortages are shown with a real minus sign in `stop`. */
function Difference({ line }: { line: StockDocumentLineDto }) {
  if (line.systemQuantity == null) return <>—</>
  const difference = line.baseQuantity - line.systemQuantity
  if (difference === 0) return <span className="text-ink-muted">Khớp</span>
  const text = formatQuantity(Math.abs(difference))
  return difference > 0 ? <span className="text-go">+{text}</span> : <span className="text-stop">−{text}</span>
}
