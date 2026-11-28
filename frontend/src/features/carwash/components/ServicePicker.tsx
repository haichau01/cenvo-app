import { Money } from '@/components/cenvo/Money'
import type { VehicleType, WashMembership, WashOffering } from '@/lib/api/types'
import { formatDuration } from '@/lib/format'
import { membershipFor, priceFor, type ChosenService } from '../lib/intake'

interface ServicePickerProps {
  offerings: WashOffering[]
  vehicleType: VehicleType
  chosen: ChosenService[]
  memberships: WashMembership[]
  onToggle: (offeringId: string) => void
  onMembership: (offeringId: string, membershipId: string | null) => void
}

/** Service tiles priced for the chosen vehicle type; a chosen service the customer's package covers offers "Trừ gói". */
export function ServicePicker({ offerings, vehicleType, chosen, memberships, onToggle, onMembership }: ServicePickerProps) {
  return (
    <ul className="grid grid-cols-2 gap-3">
      {offerings.map((offering) => {
        const price = priceFor(offering, vehicleType)
        const selection = chosen.find((service) => service.offeringId === offering.id)
        const membership = membershipFor(memberships, offering.id, vehicleType)
        return (
          <li key={offering.id} className="flex flex-col gap-2">
            <button type="button" aria-pressed={selection !== undefined} disabled={price === null} onClick={() => onToggle(offering.id)}
              className={`flex min-h-24 flex-col justify-between rounded-(--radius-panel) border-2 p-3 text-left
                disabled:cursor-not-allowed disabled:opacity-50
                ${selection ? 'border-(--module) bg-(--module)/10' : 'border-line bg-surface active:bg-concrete'}`}>
              <span className="font-display text-lg font-semibold leading-tight">{offering.name}</span>
              <span className="flex items-baseline justify-between gap-2 text-sm text-ink-muted">
                {formatDuration(offering.durationMinutes)}
                {price === null ? <span>Chưa có giá</span> : <Money value={price} className="text-base font-semibold text-ink" />}
              </span>
            </button>
            {selection && membership && (
              <label className="flex h-12 items-center gap-3 rounded-(--radius-control) bg-go/10 px-3 font-medium">
                <input type="checkbox" className="size-5 accent-(--module)" checked={selection.membershipId === membership.id}
                  onChange={(event) => onMembership(offering.id, event.target.checked ? membership.id : null)} />
                Trừ gói (còn {membership.remainingUses} lượt)
              </label>
            )}
          </li>
        )
      })}
    </ul>
  )
}
