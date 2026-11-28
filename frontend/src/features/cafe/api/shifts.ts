import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { ShiftSummary } from '@/lib/api/types'
import { cafeKeys } from './keys'

/** The cashier's open shift, or null when no shift is open. */
export const currentShiftQuery = {
  queryKey: cafeKeys.currentShift,
  queryFn: async () => (await apiFetch<ShiftSummary | null>('/api/cafe/shifts/current')) ?? null,
  staleTime: 30_000,
}

export const useCurrentShift = (enabled: boolean) => useQuery({ ...currentShiftQuery, enabled })

export function useOpenShift() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (openingCash: number) =>
      apiFetch<ShiftSummary>('/api/cafe/shifts/open', { method: 'POST', body: { openingCash } }),
    onSuccess: (shift) => queryClient.setQueryData(cafeKeys.currentShift, shift),
    meta: { inlineError: true },
  })
}

export function useCloseShift() {
  return useMutation({
    mutationFn: (input: { countedCash: number; note: string | null }) =>
      apiFetch<ShiftSummary>('/api/cafe/shifts/close', { method: 'POST', body: input }),
    meta: { inlineError: true },
  })
}
