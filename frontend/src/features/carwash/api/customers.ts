import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { SellMembershipInput, VehicleLookup, WashCustomerDetail, WashCustomerListItem, WashMembership } from '@/lib/api/types'
import { washKeys } from './keys'

/** Car, owner, usable memberships and recent visits for a plate; null for a car never seen before. */
export function usePlateLookup(plate: string, enabled: boolean) {
  return useQuery({
    queryKey: washKeys.lookup(plate),
    queryFn: async () =>
      (await apiFetch<VehicleLookup | null>(`/api/carwash/vehicles/lookup?plate=${encodeURIComponent(plate)}`)) ?? null,
    enabled,
    staleTime: 0,
  })
}

export function useCustomerSearch(q: string) {
  return useQuery({
    queryKey: washKeys.customerSearch(q),
    queryFn: () => apiFetch<WashCustomerListItem[]>(`/api/carwash/customers?q=${encodeURIComponent(q)}`),
    placeholderData: (previous) => previous,
  })
}

export function useCustomer(id: string | null) {
  return useQuery({
    queryKey: washKeys.customer(id ?? ''),
    queryFn: () => apiFetch<WashCustomerDetail>(`/api/carwash/customers/${id}`),
    enabled: id !== null,
  })
}

function useCustomerMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: washKeys.customers })
      void queryClient.invalidateQueries({ queryKey: ['carwash', 'lookup'] })
      void queryClient.invalidateQueries({ queryKey: ['cafe', 'shifts', 'current'] }) // the sale is in the shift
    },
    meta: { inlineError: true },
  })
}

export const useUpdateCustomer = () =>
  useCustomerMutation(({ id, name, phone, note }: { id: string; name: string; phone: string; note: string | null }) =>
    apiFetch<void>(`/api/carwash/customers/${id}`, { method: 'PUT', body: { name, phone, note } }))

export const useSellMembership = () =>
  useCustomerMutation((input: SellMembershipInput) =>
    apiFetch<WashMembership>('/api/carwash/memberships', { method: 'POST', body: input }))

export const useCancelMembership = () =>
  useCustomerMutation(({ id, reason }: { id: string; reason: string }) =>
    apiFetch<WashMembership>(`/api/carwash/memberships/${id}/cancel`, { method: 'POST', body: { reason } }))
