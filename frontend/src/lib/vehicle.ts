import type { VehicleType } from '@/lib/api/types'

/** Cars only (the shop does not take motorbikes). Order = order shown in pickers and price tables. */
export const VEHICLE_TYPES: { value: VehicleType; label: string }[] = [
  { value: 'Sedan', label: '4–5 chỗ' },
  { value: 'SevenSeater', label: '7 chỗ' },
  { value: 'Pickup', label: 'Bán tải' },
  { value: 'Suv', label: 'SUV' },
]

export function vehicleLabel(type: VehicleType): string {
  return VEHICLE_TYPES.find((option) => option.value === type)?.label ?? type
}
