import { Button } from '@/components/ui/Button'
import { cellControlClass } from '@/components/ui/fieldStyles'
import type { ProductDto } from '@/lib/api/types'
import { useSupplyProducts } from '../api/setup'
import type { SupplyRow } from '../lib/offeringForm'

/** Supplies used per vehicle — deducted from the branch stock when the wash is completed. */
export function SupplyRows({ rows, onChange }: { rows: SupplyRow[]; onChange: (rows: SupplyRow[]) => void }) {
  const products = useSupplyProducts().data ?? []
  const set = (index: number, changes: Partial<SupplyRow>) => onChange(rows.map((row, i) => (i === index ? { ...row, ...changes } : row)))

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="mb-2 font-display text-lg font-semibold">Hóa chất mỗi xe</legend>
      {rows.length === 0 && <p className="text-ink-muted">Dịch vụ không dùng hóa chất (vd. hút bụi).</p>}
      {rows.map((row, index) => {
        const product = products.find((item) => item.id === row.productId)
        return (
          <div key={index} className="grid grid-cols-[1fr_7rem_7rem_auto] items-center gap-2">
            <select aria-label="Hóa chất" className={cellControlClass} value={row.productId}
              onChange={(event) => set(index, { productId: event.target.value, unitId: null })}>
              <option value="">Chọn hóa chất</option>
              {products.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <input aria-label="Định mức" type="number" min={0} step="any" className={`${cellControlClass} text-right`} value={row.quantity}
              onChange={(event) => set(index, { quantity: Number(event.target.value) })} />
            <select aria-label="Đơn vị" className={cellControlClass} value={row.unitId ?? ''}
              onChange={(event) => set(index, { unitId: event.target.value || null })}>
              {unitOptions(product).map((unit) => <option key={unit.value} value={unit.value}>{unit.label}</option>)}
            </select>
            <Button variant="ghost" aria-label="Bỏ dòng" onClick={() => onChange(rows.filter((_, i) => i !== index))}>×</Button>
          </div>
        )
      })}
      <Button variant="secondary" className="self-start" onClick={() => onChange([...rows, { productId: '', unitId: null, quantity: 0 }])}>
        Thêm hóa chất
      </Button>
    </fieldset>
  )
}

/** Base unit first (value "" = base unit), then the product's conversion units. */
function unitOptions(product: ProductDto | undefined) {
  if (!product) return [{ value: '', label: '—' }]
  return [{ value: '', label: product.baseUnitName }, ...product.units.map((unit) => ({ value: unit.unitId, label: unit.unitName }))]
}
