import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { AreaDto, MenuItem } from '@/lib/api/types'
import { cafeKeys } from './keys'

const TEN_MINUTES = 10 * 60_000

export const menuQuery = {
  queryKey: cafeKeys.menu,
  queryFn: () => apiFetch<MenuItem[]>('/api/cafe/menu'),
  staleTime: TEN_MINUTES,
}

/** Areas + tables with their open order — changes on every order, so it is refreshed by realtime signals. */
export const floorPlanQuery = {
  queryKey: cafeKeys.tables,
  queryFn: () => apiFetch<AreaDto[]>('/api/cafe/areas'),
  staleTime: 0,
}

export const useMenu = () => useQuery(menuQuery)
export const useFloorPlan = () => useQuery(floorPlanQuery)
