import { SelectField } from '@/components/ui/SelectField'
import { controlClass } from '@/components/ui/fieldStyles'
import { useSuppliers, useWarehouses } from '../api/references'
import type { DraftDocument } from '../lib/documentForm'
import type { DocumentTypeConfig } from '../lib/documentTypes'

interface DocumentHeaderFieldsProps {
  draft: DraftDocument
  config: DocumentTypeConfig
  today: string
  onChange: (patch: Partial<DraftDocument>) => void
}

/** Warehouse, destination (transfers), supplier (receipts), date and note — only the fields this type uses. */
export function DocumentHeaderFields({ draft, config, today, onChange }: DocumentHeaderFieldsProps) {
  const ownWarehouses = useWarehouses()
  const allWarehouses = useWarehouses(config.destination)
  const suppliers = useSuppliers()
  const destinations = (allWarehouses.data ?? []).filter((w) => w.id !== draft.warehouseId)

  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(14rem,1fr))] gap-4">
      <SelectField label={config.destination ? 'Kho đi' : 'Kho'} value={draft.warehouseId}
        onChange={(warehouseId) => onChange({ warehouseId })}
        options={(ownWarehouses.data ?? []).map((w) => ({ value: w.id, label: w.name }))} />
      {config.destination && (
        <SelectField label="Kho đến" value={draft.toWarehouseId ?? ''} placeholder="Chọn kho đến"
          onChange={(id) => onChange({ toWarehouseId: id || null })}
          options={destinations.map((w) => ({ value: w.id, label: w.name }))} />
      )}
      {config.supplier && (
        <SelectField label="Nhà cung cấp" value={draft.supplierId ?? ''} placeholder="Không ghi nhà cung cấp"
          onChange={(id) => onChange({ supplierId: id || null })}
          options={(suppliers.data ?? []).map((s) => ({ value: s.id, label: s.name }))} />
      )}
      <label className="flex flex-col gap-1.5 text-sm font-medium">Ngày chứng từ
        <input type="date" value={draft.date} max={today} onChange={(event) => onChange({ date: event.target.value || today })} className={controlClass} />
      </label>
      <label className="col-span-full flex flex-col gap-1.5 text-sm font-medium">Ghi chú
        <input value={draft.note} maxLength={500} onChange={(event) => onChange({ note: event.target.value })}
          placeholder="Số hóa đơn nhà cung cấp, người giao hàng…" className={controlClass} />
      </label>
    </div>
  )
}
