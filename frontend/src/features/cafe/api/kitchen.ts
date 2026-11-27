import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { KitchenStatus, KitchenTicket } from '@/lib/api/types'
import { cafeKeys } from './keys'

export const kitchenQuery = {
  queryKey: cafeKeys.kitchen,
  queryFn: () => apiFetch<KitchenTicket[]>('/api/cafe/kitchen'),
  staleTime: 0,
}

export const useKitchenQueue = () => useQuery(kitchenQuery)

const MOVE_KEY = ['cafe', 'kitchen-move'] as const

interface MoveVariables {
  lineId: string
  status: KitchenStatus
}

/**
 * Moves a drink to another column. Optimistic: the card moves on tap, the server confirms in the background.
 * Moves run one after another (same scope) because two lines of one order share the order's concurrency version.
 */
export function useMoveTicket() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationKey: MOVE_KEY,
    scope: { id: 'cafe-kitchen' },
    mutationFn: ({ lineId, status }: MoveVariables) =>
      apiFetch<void>(`/api/cafe/kitchen/${lineId}`, { method: 'PUT', body: { status } }),
    onMutate: async ({ lineId, status }) => {
      await queryClient.cancelQueries({ queryKey: cafeKeys.kitchen })
      queryClient.setQueryData<KitchenTicket[]>(cafeKeys.kitchen, (tickets) =>
        tickets?.map((ticket) => (ticket.lineId === lineId ? { ...ticket, status } : ticket)))
    },
    // Success or failure, re-read the queue: the server is the source of truth (and POS bills show the new status).
    onSettled: () => {
      // Later taps are still queued: refetching now would briefly move their cards back.
      if (queryClient.isMutating({ mutationKey: MOVE_KEY }) > 1) return
      void queryClient.invalidateQueries({ queryKey: cafeKeys.kitchen })
      void queryClient.invalidateQueries({ queryKey: cafeKeys.orders })
    },
  })
}
