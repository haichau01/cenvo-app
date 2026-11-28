import type { CommissionType, VehicleType, WashOffering, WashOfferingInput } from '@/lib/api/types'
import { VEHICLE_TYPES } from './vehicle'

export interface SupplyRow {
  productId: string
  unitId: string | null
  quantity: number
}

export interface OfferingForm {
  code: string
  name: string
  durationMinutes: number
  commissionType: CommissionType
  commissionValue: number
  sortOrder: number
  isActive: boolean
  /** 0 = not offered for that vehicle type. */
  prices: Record<VehicleType, number>
  supplies: SupplyRow[]
}

const DEFAULT_DURATION = 30
const DEFAULT_COMMISSION_PERCENT = 20

export function toOfferingForm(offering: WashOffering | null): OfferingForm {
  const prices = Object.fromEntries(VEHICLE_TYPES.map((type) => [type.value, 0])) as Record<VehicleType, number>
  for (const price of offering?.prices ?? []) prices[price.vehicleType] = price.price
  return {
    code: offering?.code ?? '',
    name: offering?.name ?? '',
    durationMinutes: offering?.durationMinutes ?? DEFAULT_DURATION,
    commissionType: offering?.commissionType ?? 'Percent',
    commissionValue: offering?.commissionValue ?? DEFAULT_COMMISSION_PERCENT,
    sortOrder: offering?.sortOrder ?? 0,
    isActive: offering?.isActive ?? true,
    prices,
    supplies: offering?.supplies.map((supply) => ({ productId: supply.productId, unitId: supply.unitId ?? null, quantity: supply.quantity })) ?? [],
  }
}

/** Vehicle types left at 0 are not offered; empty supply rows are dropped. The server validates the rest. */
export function toOfferingInput(form: OfferingForm): WashOfferingInput {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    durationMinutes: form.durationMinutes,
    commissionType: form.commissionType,
    commissionValue: form.commissionValue,
    sortOrder: form.sortOrder,
    isActive: form.isActive,
    prices: VEHICLE_TYPES.filter((type) => form.prices[type.value] > 0)
      .map((type) => ({ vehicleType: type.value, price: form.prices[type.value] })),
    supplies: form.supplies.filter((supply) => supply.productId && supply.quantity > 0),
  }
}
