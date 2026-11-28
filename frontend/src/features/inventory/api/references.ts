import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { ProductDto, SupplierDto, WarehouseDto } from '@/lib/api/types'
import { inventoryKeys } from './keys'

const TEN_MINUTES = 10 * 60_000

/** Warehouses of the current branch; `allBranches` adds other branches' warehouses (transfer destination). */
export const useWarehouses = (allBranches = false) => useQuery({
  queryKey: inventoryKeys.warehouses(allBranches),
  queryFn: () => apiFetch<WarehouseDto[]>(`/api/inventory/warehouses${allBranches ? '?all=true' : ''}`),
  staleTime: TEN_MINUTES,
})

export const useSuppliers = () => useQuery({
  queryKey: inventoryKeys.suppliers,
  queryFn: () => apiFetch<SupplierDto[]>('/api/inventory/suppliers'),
  staleTime: TEN_MINUTES,
})

/** Stock-tracked products (≤ 500, the backend list limit) — loaded once, searched on the client. */
export const useStockProducts = () => useQuery({
  queryKey: inventoryKeys.products,
  queryFn: async () => (await apiFetch<ProductDto[]>('/api/catalog/products')).filter((p) => p.trackStock),
  staleTime: TEN_MINUTES,
})
