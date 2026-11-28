import { useState } from 'react'
import type { VehicleLookup, VehicleType } from '@/lib/api/types'
import type { ChosenService } from '../lib/intake'

export interface IntakeDraft {
  plate: string
  vehicleType: VehicleType
  description: string
  phone: string
  name: string
  note: string
  services: ChosenService[]
  staffIds: string[]
}

const EMPTY: IntakeDraft = {
  plate: '', vehicleType: 'Sedan', description: '', phone: '', name: '', note: '', services: [], staffIds: [],
}

/** Intake form state. A known plate fills the car and owner once (staff can still change them). */
export function useIntakeForm() {
  const [draft, setDraft] = useState<IntakeDraft>(EMPTY)
  const [prefilledFrom, setPrefilledFrom] = useState<VehicleLookup | null>(null)

  const update = (changes: Partial<IntakeDraft>) => setDraft((current) => ({ ...current, ...changes }))

  /** Called while rendering with the latest lookup (React's "adjust state on input change" pattern, no effect). */
  function prefill(lookup: VehicleLookup | null | undefined) {
    if (!lookup || lookup === prefilledFrom) return
    setPrefilledFrom(lookup)
    setDraft((current) => ({
      ...current,
      vehicleType: lookup.vehicle.vehicleType,
      description: current.description || (lookup.vehicle.description ?? ''),
      phone: current.phone || (lookup.customer?.phone ?? ''),
      name: current.name || (lookup.customer?.name ?? ''),
    }))
  }

  function toggleService(offeringId: string) {
    setDraft((current) => {
      const chosen = current.services.some((service) => service.offeringId === offeringId)
      const services = chosen
        ? current.services.filter((service) => service.offeringId !== offeringId)
        : [...current.services, { offeringId, membershipId: null }]
      return { ...current, services }
    })
  }

  function setMembership(offeringId: string, membershipId: string | null) {
    setDraft((current) => ({
      ...current,
      services: current.services.map((service) => (service.offeringId === offeringId ? { ...service, membershipId } : service)),
    }))
  }

  function toggleStaff(userId: string) {
    setDraft((current) => ({
      ...current,
      staffIds: current.staffIds.includes(userId) ? current.staffIds.filter((id) => id !== userId) : [...current.staffIds, userId],
    }))
  }

  return { draft, update, prefill, toggleService, setMembership, toggleStaff }
}
