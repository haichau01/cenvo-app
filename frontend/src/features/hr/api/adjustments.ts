import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { AdjustmentInput, PayAdjustment, StaffMember } from '@/lib/api/types'
import { hrKeys } from './keys'

export const useAdjustments = (from: string, to: string) => useQuery({
  queryKey: hrKeys.adjustmentList(from, to),
  queryFn: () => apiFetch<PayAdjustment[]>(`/api/hr/adjustments?from=${from}&to=${to}`),
  placeholderData: keepPreviousData,
})

/** Active staff of the current branch — who can be given an advance, bonus or penalty. */
export const useAdjustmentStaff = () =>
  useQuery({ queryKey: hrKeys.staff, queryFn: () => apiFetch<StaffMember[]>('/api/hr/adjustments/staff') })

function useAdjustmentMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSettled: () => void queryClient.invalidateQueries({ queryKey: hrKeys.adjustments }),
    meta: { inlineError: true },
  })
}

export const useRecordAdjustment = () =>
  useAdjustmentMutation((input: AdjustmentInput) => apiFetch<string>('/api/hr/adjustments', { method: 'POST', body: input }))

export const useCancelAdjustment = () =>
  useAdjustmentMutation(({ id, reason }: { id: string; reason: string }) =>
    apiFetch<void>(`/api/hr/adjustments/${id}/cancel`, { method: 'POST', body: { reason } }))
