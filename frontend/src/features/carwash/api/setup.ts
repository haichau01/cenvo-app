import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { ProductDto, WashOffering, WashOfferingInput, WashPackage, WashPackageInput, Washer } from '@/lib/api/types'
import { washKeys } from './keys'

const TEN_MINUTES = 10 * 60_000

/** `all`: managers also see inactive services; intake only offers active ones. */
export const offeringsQuery = (all = false) => ({
  queryKey: washKeys.offerings(all),
  queryFn: () => apiFetch<WashOffering[]>(`/api/carwash/services${all ? '?all=true' : ''}`),
  staleTime: TEN_MINUTES,
})

export const useOfferings = (all = false) => useQuery(offeringsQuery(all))

export const usePackages = (all = false) => useQuery({
  queryKey: washKeys.packages(all),
  queryFn: () => apiFetch<WashPackage[]>(`/api/carwash/packages${all ? '?all=true' : ''}`),
  staleTime: TEN_MINUTES,
})

export const washersQuery = {
  queryKey: washKeys.washers,
  queryFn: () => apiFetch<Washer[]>('/api/carwash/staff'),
  staleTime: TEN_MINUTES,
}

export const useWashers = () => useQuery(washersQuery)

function useSetupMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['carwash', 'services'] })
      void queryClient.invalidateQueries({ queryKey: ['carwash', 'packages'] })
    },
    meta: { inlineError: true },
  })
}

export const useSaveOffering = () =>
  useSetupMutation(({ id, input }: { id: string | null; input: WashOfferingInput }) =>
    apiFetch<string>(id ? `/api/carwash/services/${id}` : '/api/carwash/services', { method: id ? 'PUT' : 'POST', body: input }))

export const useDeleteOffering = () =>
  useSetupMutation((id: string) => apiFetch<void>(`/api/carwash/services/${id}`, { method: 'DELETE' }))

export const useSavePackage = () =>
  useSetupMutation(({ id, input }: { id: string | null; input: WashPackageInput }) =>
    apiFetch<string>(id ? `/api/carwash/packages/${id}` : '/api/carwash/packages', { method: id ? 'PUT' : 'POST', body: input }))

/** Stock-tracked supplies (chemicals, towels) that a service can consume. */
export const useSupplyProducts = () => useQuery({
  queryKey: ['carwash', 'supply-products'] as const,
  queryFn: async () => (await apiFetch<ProductDto[]>('/api/catalog/products')).filter((product) => product.kind === 'Supply'),
  staleTime: TEN_MINUTES,
})
