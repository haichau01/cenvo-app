import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { BalanceDto, StockCardDto } from '@/lib/api/types'
import { inventoryKeys } from './keys'

/** Server-side filters. Text search happens in the browser (see StockPage) so it ignores Vietnamese diacritics. */
export interface BalanceFilter {
  warehouseId: string | null
  belowMinOnly: boolean
}

function query(params: Record<string, string | null | undefined | false>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) if (value) search.set(key, value)
  const text = search.toString()
  return text ? `?${text}` : ''
}

export const useBalances = (filter: BalanceFilter) => useQuery({
  queryKey: inventoryKeys.balances(filter),
  queryFn: () => apiFetch<BalanceDto[]>(`/api/inventory/balances${query({
    warehouseId: filter.warehouseId, belowMin: filter.belowMinOnly && 'true',
  })}`),
  placeholderData: keepPreviousData, // keep the table on screen while switching filters
})

export interface StockCardParams {
  productId: string
  warehouseId: string
  from: string
  to: string
}

export const useStockCard = (params: StockCardParams | null) => useQuery({
  queryKey: inventoryKeys.stockCard(params ?? {}),
  queryFn: () => apiFetch<StockCardDto>(`/api/inventory/stock-card${query({ ...params })}`),
  enabled: params !== null,
})
