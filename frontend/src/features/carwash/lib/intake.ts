import type { VehicleType, WashMembership, WashOffering } from '@/lib/api/types'

/** Price of a service for the vehicle type, or null when the manager has not priced it. */
export function priceFor(offering: WashOffering, vehicleType: VehicleType): number | null {
  return offering.prices.find((price) => price.vehicleType === vehicleType)?.price ?? null
}

/** A usable membership of the customer that covers this service for this vehicle type. */
export function membershipFor(memberships: WashMembership[], offeringId: string, vehicleType: VehicleType): WashMembership | null {
  return memberships.find((membership) => membership.isUsable
    && membership.offeringId === offeringId
    && (membership.vehicleType == null || membership.vehicleType === vehicleType)) ?? null
}

export interface ChosenService {
  offeringId: string
  /** Pay with one use of this membership instead of the price. */
  membershipId: string | null
}

/** Preview only — the backend computes the real total. */
export function previewTotal(chosen: ChosenService[], offerings: WashOffering[], vehicleType: VehicleType): number {
  return chosen.reduce((sum, service) => {
    if (service.membershipId) return sum
    const offering = offerings.find((item) => item.id === service.offeringId)
    return sum + (offering ? priceFor(offering, vehicleType) ?? 0 : 0)
  }, 0)
}
