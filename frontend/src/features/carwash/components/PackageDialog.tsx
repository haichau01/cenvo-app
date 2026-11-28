import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { MoneyField } from '@/components/ui/MoneyField'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { VehicleType, WashOffering, WashPackage, WashPackageInput } from '@/lib/api/types'
import { useSavePackage } from '../api/setup'
import { VEHICLE_TYPES } from '@/lib/vehicle'

const DEFAULT_USES = 12
const DEFAULT_VALID_DAYS = 180

interface PackageDialogProps {
  item: WashPackage | 'new' | null
  offerings: WashOffering[]
  onClose: () => void
}

/** Prepaid package, e.g. "mua 10 tặng 2" = 12 uses of one service for the price of 10. */
export function PackageDialog({ item, offerings, onClose }: PackageDialogProps) {
  const editing = item === 'new' ? null : item
  return (
    <Dialog open={item !== null} onClose={onClose} title={editing ? `Sửa gói ${editing.name}` : 'Thêm gói thành viên'}>
      {item !== null && <PackageEditor key={editing?.id ?? 'new'} item={editing} offerings={offerings} onSaved={onClose} />}
    </Dialog>
  )
}

function PackageEditor({ item, offerings, onSaved }: { item: WashPackage | null; offerings: WashOffering[]; onSaved: () => void }) {
  const save = useSavePackage()
  const [form, setForm] = useState<WashPackageInput>(() => ({
    code: item?.code ?? '', name: item?.name ?? '', offeringId: item?.offeringId ?? '', vehicleType: item?.vehicleType ?? undefined,
    price: item?.price ?? 0, uses: item?.uses ?? DEFAULT_USES, validDays: item?.validDays ?? DEFAULT_VALID_DAYS, isActive: item?.isActive ?? true,
  }))
  const update = (changes: Partial<WashPackageInput>) => setForm((current) => ({ ...current, ...changes }))

  function handleSave() {
    save.mutate({ id: item?.id ?? null, input: form }, { onSuccess: () => { toast.success(`Đã lưu gói ${form.name}`); onSaved() } })
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-[8rem_1fr] gap-3">
        <TextField label="Mã" value={form.code} onChange={(event) => update({ code: event.target.value })} maxLength={30} />
        <TextField label="Tên gói" value={form.name} onChange={(event) => update({ name: event.target.value })} maxLength={100} />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <SelectField label="Dịch vụ mỗi lượt" value={form.offeringId} placeholder="Chọn dịch vụ" onChange={(offeringId) => update({ offeringId })}
          options={offerings.map((offering) => ({ value: offering.id, label: offering.name }))} />
        <SelectField label="Loại xe" value={form.vehicleType ?? ''} placeholder="Mọi loại xe"
          onChange={(value) => update({ vehicleType: (value || undefined) as VehicleType | undefined })}
          options={VEHICLE_TYPES.map((type) => ({ value: type.value, label: type.label }))} />
      </div>
      <div className="grid grid-cols-3 gap-3">
        <MoneyField label="Giá gói" value={form.price} onChange={(price) => update({ price })} />
        <TextField label="Số lượt (kể cả tặng)" type="number" min={1} value={form.uses} onChange={(event) => update({ uses: Number(event.target.value) })} />
        <TextField label="Hạn dùng (ngày)" type="number" min={1} value={form.validDays}
          onChange={(event) => update({ validDays: Number(event.target.value) })} />
      </div>
      <label className="flex h-12 items-center gap-3 font-medium">
        <input type="checkbox" className="size-5 accent-(--module)" checked={form.isActive} onChange={(event) => update({ isActive: event.target.checked })} />
        Đang bán
      </label>
      <FormError message={save.error?.message} />
      <Button size="lg" onClick={handleSave} busy={save.isPending} disabled={!form.offeringId}>Lưu gói</Button>
    </div>
  )
}
