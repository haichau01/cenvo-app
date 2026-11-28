import { sessionStore } from '@/lib/auth/session'
import { queryClient } from '@/lib/queryClient'
import { offeringsQuery, washersQuery } from './api/setup'
import { queueQuery } from './api/tickets'

/** Route loaders: start loading the data in parallel with the page code (never blocks navigation). */
export function prefetchQueue() {
  if (sessionStore.get()?.user.branchId == null) return null
  void queryClient.prefetchQuery(queueQuery)
  return null
}

export function prefetchIntake() {
  if (sessionStore.get()?.user.branchId == null) return null
  void queryClient.prefetchQuery(offeringsQuery())
  void queryClient.prefetchQuery(washersQuery)
  return null
}
