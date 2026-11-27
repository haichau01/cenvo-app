import { Money } from '@/components/cenvo/Money'
import { AmountInput } from '@/components/ui/AmountInput'
import { cellControlClass } from '@/components/ui/fieldStyles'
import type { ProductDto } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import type { DocumentTypeConfig } from '../lib/documentTypes'
import { lineAmount, type DraftLine } from '../lib/documentForm'

interface LinesEditorProps {
  lines: DraftLine[]
  config: DocumentTypeConfig
  products: Map<string, ProductDto>
  onChange: (key: string, patch: Partial<DraftLine>) => void
  onRemove: (key: string) => void
}

const th = 'px-3 py-2 text-left text-caption font-medium text-ink-muted'

export function LinesEditor({ lines, config, products, onChange, onRemove }: LinesEditorProps) {
  const total = lines.reduce((sum, line) => sum + lineAmount(line), 0) // display only — the server computes amounts
  return (
    <table className="w-full text-table">
      <thead className="border-b border-line">
        <tr>
          <th className={th}>Sản phẩm</th>
          <th className={`${th} w-44`}>Đơn vị</th>
          <th className={`${th} w-32 text-right`}>{config.quantityLabel}</th>
          {config.priceLabel && <th className={`${th} w-40 text-right`}>{config.priceLabel}</th>}
          {config.priceLabel && <th className={`${th} w-36 text-right`}>Thành tiền</th>}
          <th className={`${th} w-56`}>Ghi chú</th>
          <th className="w-12"><span className="sr-only">Xóa</span></th>
        </tr>
      </thead>
      <tbody>
        {lines.map((line) => (
          <LineRow key={line.key} line={line} config={config} product={products.get(line.productId)} onChange={onChange} onRemove={onRemove} />
        ))}
      </tbody>
      {config.priceLabel && lines.length > 0 && (
        <tfoot>
          <tr className="h-11 font-semibold">
            <td className="px-3" colSpan={4}>Tổng cộng</td>
            <td className="px-3 text-right"><Money value={total} /></td>
            <td colSpan={2} />
          </tr>
        </tfoot>
      )}
    </table>
  )
}

interface LineRowProps {
  line: DraftLine
  config: DocumentTypeConfig
  product: ProductDto | undefined
  onChange: (key: string, patch: Partial<DraftLine>) => void
  onRemove: (key: string) => void
}

function LineRow({ line, config, product, onChange, onRemove }: LineRowProps) {
  const update = (patch: Partial<DraftLine>) => onChange(line.key, patch)
  return (
    <tr className="border-b border-line">
      <td className="px-3 py-2"><span className="text-ink-muted">{line.productCode}</span> · <span className="font-medium">{line.productName}</span></td>
      <td className="px-3 py-2">
        <select aria-label={`Đơn vị của ${line.productName}`} value={line.unitId ?? ''} className={cellControlClass}
          onChange={(event) => update({ unitId: event.target.value || null })}>
          <option value="">{product?.baseUnitName ?? 'Đơn vị cơ sở'}</option>
          {product?.units.map((unit) => <option key={unit.unitId} value={unit.unitId}>{unit.unitName} ({formatQuantity(unit.factor)} {product.baseUnitName})</option>)}
        </select>
      </td>
      <td className="px-3 py-2">
        <input type="number" inputMode="decimal" min={0} step="any" aria-label={`${config.quantityLabel} của ${line.productName}`}
          value={line.quantity || ''} onChange={(event) => update({ quantity: Number(event.target.value) })}
          className={`${cellControlClass} text-right tabular-nums`} />
      </td>
      {config.priceLabel && (
        <td className="px-3 py-2">
          <AmountInput aria-label={`${config.priceLabel} của ${line.productName}`} value={line.unitPrice} onChange={(unitPrice) => update({ unitPrice })} />
        </td>
      )}
      {config.priceLabel && <td className="px-3 py-2 text-right"><Money value={lineAmount(line)} /></td>}
      <td className="px-3 py-2">
        <input aria-label={`Ghi chú của ${line.productName}`} value={line.note} maxLength={200}
          onChange={(event) => update({ note: event.target.value })} className={cellControlClass} />
      </td>
      <td className="px-1 py-2">
        <button type="button" onClick={() => onRemove(line.key)} aria-label={`Xóa ${line.productName}`}
          className="flex size-10 items-center justify-center rounded-(--radius-control) text-xl text-ink-muted hover:bg-stop/10 hover:text-stop">×</button>
      </td>
    </tr>
  )
}
