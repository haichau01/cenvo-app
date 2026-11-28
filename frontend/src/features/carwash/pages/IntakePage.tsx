import { useNavigate } from 'react-router'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import { isPlausiblePlate, normalizePlate } from '@/lib/plate'
import { useDebounced } from '@/lib/useDebounced'
import { usePlateLookup } from '../api/customers'
import { useOfferings, useWashers } from '../api/setup'
import { useCreateTicket } from '../api/tickets'
import { LookupSummary } from '../components/LookupSummary'
import { PlateField } from '../components/PlateField'
import { RequireBranch } from '../components/RequireBranch'
import { ServicePicker } from '../components/ServicePicker'
import { WasherChips } from '../components/WasherChips'
import { useIntakeForm, type IntakeDraft } from '../hooks/useIntakeForm'
import { previewTotal } from '../lib/intake'
import { VEHICLE_TYPES } from '../lib/vehicle'

export function IntakePage() {
  return <RequireBranch from="/rua-xe/tiep-nhan"><IntakeScreen /></RequireBranch>
}

/** Taking a car in at the gate: plate → (known car fills itself) → services → washers → create. Photos come next on the ticket. */
function IntakeScreen() {
  const navigate = useNavigate()
  const form = useIntakeForm()
  const { draft } = form
  const plate = useDebounced(normalizePlate(draft.plate))
  const lookup = usePlateLookup(plate, isPlausiblePlate(plate))
  const offerings = useOfferings().data ?? []
  const washers = useWashers().data ?? []
  const create = useCreateTicket()
  form.prefill(lookup.data)
  const memberships = ownerStillMatches(lookup.data?.customer?.phone, draft.phone) ? lookup.data!.memberships : []

  function handleCreate() {
    create.mutate(toInput(draft), {
      onSuccess: (ticket) => {
        toast.success(`Đã tạo phiếu ${ticket.code}`)
        navigate(`/rua-xe?phieu=${ticket.id}`) // open the ticket to take condition photos
      },
    })
  }

  return (
    <div className="grid min-h-0 flex-1 grid-cols-[minmax(0,5fr)_minmax(0,6fr)] gap-6 overflow-y-auto p-6">
      <section aria-label="Xe và khách" className="flex flex-col gap-4">
        <h1 className="font-display text-screen font-semibold">Tiếp nhận xe</h1>
        <PlateField value={draft.plate} onChange={(value) => form.update({ plate: value })} />
        <LookupSummary lookup={isPlausiblePlate(plate) ? lookup.data : undefined} />
        <VehicleTypePicker value={draft.vehicleType} onChange={(vehicleType) => form.update({ vehicleType })} />
        <TextField label="Hiệu xe, màu (không bắt buộc)" value={draft.description} placeholder="Vios trắng"
          onChange={(event) => form.update({ description: event.target.value })} maxLength={100} />
        <div className="grid grid-cols-2 gap-3">
          <TextField label="Số điện thoại khách" inputMode="tel" value={draft.phone}
            onChange={(event) => form.update({ phone: event.target.value })} maxLength={15} />
          <TextField label="Tên khách" value={draft.name} onChange={(event) => form.update({ name: event.target.value })} maxLength={100} />
        </div>
        <TextField label="Ghi chú" value={draft.note} onChange={(event) => form.update({ note: event.target.value })} maxLength={300}
          placeholder="Vết xước cửa trái, đồ trong cốp…" />
      </section>

      <section aria-label="Dịch vụ" className="flex flex-col gap-4">
        <h2 className="font-display text-block font-semibold">Dịch vụ</h2>
        <ServicePicker offerings={offerings} vehicleType={draft.vehicleType} chosen={draft.services} memberships={memberships}
          onToggle={form.toggleService} onMembership={form.setMembership} />
        <h2 className="font-display text-block font-semibold">Người rửa</h2>
        <WasherChips washers={washers} chosen={draft.staffIds} onToggle={form.toggleStaff} />
        <div className="mt-auto flex flex-col gap-3 border-t border-line pt-4">
          <div className="flex items-baseline justify-between">
            <span className="text-ink-muted">Tạm tính</span>
            <Money value={previewTotal(draft.services, offerings, draft.vehicleType)} className="amount text-amount" />
          </div>
          <FormError message={create.error?.message} />
          <Button size="lg" onClick={handleCreate} busy={create.isPending}
            disabled={!isPlausiblePlate(draft.plate) || draft.services.length === 0}>
            Tạo phiếu rửa
          </Button>
        </div>
      </section>
    </div>
  )
}

function VehicleTypePicker({ value, onChange }: { value: IntakeDraft['vehicleType']; onChange: (value: IntakeDraft['vehicleType']) => void }) {
  return (
    <div role="radiogroup" aria-label="Loại xe" className="grid grid-cols-4 gap-2">
      {VEHICLE_TYPES.map((option) => (
        <button key={option.value} type="button" role="radio" aria-checked={value === option.value} onClick={() => onChange(option.value)}
          className={`h-12 rounded-(--radius-control) border-2 font-semibold
            ${value === option.value ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
          {option.label}
        </button>
      ))}
    </div>
  )
}

function toInput(draft: IntakeDraft) {
  const text = (value: string) => value.trim() || null
  return {
    plate: draft.plate, vehicleType: draft.vehicleType, vehicleDescription: text(draft.description),
    customerName: text(draft.name), customerPhone: text(draft.phone), services: draft.services, staffIds: draft.staffIds,
    note: text(draft.note),
  }
}

/** Packages belong to the car's known owner: offer them unless staff typed someone else's phone number. */
function ownerStillMatches(ownerPhone: string | undefined, typedPhone: string): boolean {
  if (!ownerPhone) return false
  const digits = typedPhone.replace(/\D/g, '')
  return digits === '' || digits === ownerPhone
}
