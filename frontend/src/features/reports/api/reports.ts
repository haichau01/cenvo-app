import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { ConsumptionDto, SalesSummary, ShiftDto } from '@/lib/api/types'
import type { Period } from '../lib/period'

const range = (period: Period) => `from=${period.from}&to=${period.to}`

// Reports only read; keepPreviousData keeps numbers on screen while switching periods instead of flashing skeletons.
export const useSalesSummary = (period: Period) => useQuery({
  queryKey: ['reports', 'cafe-sales', period],
  queryFn: () => apiFetch<SalesSummary>(`/api/reports/cafe-sales?${range(period)}`),
  placeholderData: keepPreviousData,
})

export const useShifts = (period: Period) => useQuery({
  queryKey: ['reports', 'shifts', period],
  queryFn: () => apiFetch<ShiftDto[]>(`/api/cafe/shifts?${range(period)}`),
  placeholderData: keepPreviousData,
})

export const useConsumption = (period: Period) => useQuery({
  queryKey: ['reports', 'consumption', period],
  queryFn: () => apiFetch<ConsumptionDto[]>(`/api/inventory/consumption?${range(period)}`),
  placeholderData: keepPreviousData,
})
