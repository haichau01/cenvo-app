import { sessionStore } from '@/lib/auth/session'
import { queryClient } from '@/lib/queryClient'
import { openOrdersQuery } from './api/orders'
import { floorPlanQuery, menuQuery } from './api/setup'

/** Route loader for /pos: warm the caches without blocking navigation. Kept tiny so the main bundle stays small. */
export function prefetchPos() {
  if (sessionStore.get()?.user.branchId == null) return null
  void queryClient.prefetchQuery(menuQuery)
  void queryClient.prefetchQuery(floorPlanQuery)
  void queryClient.prefetchQuery(openOrdersQuery)
  return null
}
