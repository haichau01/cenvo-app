import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { MoneyField } from '@/components/ui/MoneyField'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { WashOffering } from '@/lib/api/types'
import { useSaveOffering } from '../api/setup'
import { toOfferingForm, toOfferingInput, type OfferingForm } from '../lib/offeringForm'
import { VEHICLE_TYPES } from '@/lib/vehicle'
import { SupplyRows } from './SupplyRows'

/** `offering`: null = closed, 'new' = create. */
export function OfferingDialog({ offering, onClose }: { offering: WashOffering | 'new' | null; onClose: () => void }) {
  const editing = offering === 'new' ? null : offering
  return (
    <Dialog open={offering !== null} onClose={onClose} title={editing ? `Sửa dịch vụ ${editing.name}` : 'Thêm dịch vụ'}
      className="w-[min(44rem,calc(100vw-2rem))]">
      {offering !== null && <OfferingEditor key={editing?.id ?? 'new'} offering={editing} onSaved={onClose} />}
    </Dialog>
  )
}

function OfferingEditor({ offering, onSaved }: { offering: WashOffering | null; onSaved: () => void }) {
  const save = useSaveOffering()
  const [form, setForm] = useState<OfferingForm>(() => toOfferingForm(offering))
  const update = (changes: Partial<OfferingForm>) => setForm((current) => ({ ...current, ...changes }))
  const isPercent = form.commissionType === 'Percent'

  function handleSave() {
    save.mutate({ id: offering?.id ?? null, input: toOfferingInput(form) }, {
      onSuccess: () => { toast.success(`Đã lưu dịch vụ ${form.name}`); onSaved() },
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="grid grid-cols-[8rem_1fr_8rem] gap-3">
        <TextField label="Mã" value={form.code} onChange={(event) => update({ code: event.target.value })} maxLength={30} />
        <TextField label="Tên dịch vụ" value={form.name} onChange={(event) => update({ name: event.target.value })} maxLength={100} />
        <TextField label="Thời gian (phút)" type="number" min={1} value={form.durationMinutes}
          onChange={(event) => update({ durationMinutes: Number(event.target.value) })} />
      </div>
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 font-display text-lg font-semibold">Giá theo loại xe (để 0 nếu không nhận)</legend>
        <div className="grid grid-cols-4 gap-3">
          {VEHICLE_TYPES.map((type) => (
            <MoneyField key={type.value} label={type.label} value={form.prices[type.value]}
              onChange={(price) => update({ prices: { ...form.prices, [type.value]: price } })} />
          ))}
        </div>
      </fieldset>
      <div className="grid grid-cols-2 gap-3">
        <SelectField label="Hoa hồng" value={form.commissionType} onChange={(value) => update({ commissionType: value as OfferingForm['commissionType'] })}
          options={[{ value: 'Percent', label: 'Phần trăm giá' }, { value: 'FixedAmount', label: 'Số tiền mỗi xe' }]} />
        {isPercent
          ? <TextField label="Phần trăm (chia đều cho người rửa)" type="number" min={0} max={100} value={form.commissionValue}
              onChange={(event) => update({ commissionValue: Number(event.target.value) })} />
          : <MoneyField label="Số tiền mỗi xe (chia đều)" value={form.commissionValue} onChange={(value) => update({ commissionValue: value })} />}
      </div>
      <SupplyRows rows={form.supplies} onChange={(supplies) => update({ supplies })} />
      <label className="flex h-12 items-center gap-3 font-medium">
        <input type="checkbox" className="size-5 accent-(--module)" checked={form.isActive} onChange={(event) => update({ isActive: event.target.checked })} />
        Đang cung cấp (hiện ở màn tiếp nhận)
      </label>
      <FormError message={save.error?.message} />
      <Button size="lg" onClick={handleSave} busy={save.isPending}>Lưu dịch vụ</Button>
    </div>
  )
}
