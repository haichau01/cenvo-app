import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { DocumentStatus, StockDocumentDto, StockDocumentInput, StockDocumentType, StockWarning } from '@/lib/api/types'
import { inventoryKeys } from './keys'

export interface DocumentFilter {
  type: StockDocumentType | null
  status: DocumentStatus | null
}

export const useDocuments = (filter: DocumentFilter) => useQuery({
  queryKey: inventoryKeys.documentList(filter),
  queryFn: () => {
    const params = new URLSearchParams()
    if (filter.type) params.set('type', filter.type)
    if (filter.status) params.set('status', filter.status)
    return apiFetch<StockDocumentDto[]>(`/api/inventory/documents?${params}`)
  },
  placeholderData: keepPreviousData,
})

export const useIncomingTransfers = () => useQuery({
  queryKey: inventoryKeys.incoming,
  queryFn: () => apiFetch<StockDocumentDto[]>('/api/inventory/documents/incoming'),
})

export const useDocument = (id: string | null) => useQuery({
  queryKey: inventoryKeys.document(id ?? ''),
  queryFn: () => apiFetch<StockDocumentDto>(`/api/inventory/documents/${id}`),
  enabled: id !== null,
})

/** After any document change, stock and document lists are stale. */
function useInventoryMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: inventoryKeys.all }),
  })
}

export const useCreateDocument = () =>
  useInventoryMutation((input: StockDocumentInput) =>
    apiFetch<string>('/api/inventory/documents', { method: 'POST', body: input }))

export const useUpdateDocument = () =>
  useInventoryMutation(({ id, input }: { id: string; input: StockDocumentInput }) =>
    apiFetch<void>(`/api/inventory/documents/${id}`, { method: 'PUT', body: input }))

export const useDeleteDocument = () =>
  useInventoryMutation((id: string) => apiFetch<void>(`/api/inventory/documents/${id}`, { method: 'DELETE' }))

export const usePostDocument = () =>
  useInventoryMutation((id: string) => apiFetch<StockWarning[]>(`/api/inventory/documents/${id}/post`, { method: 'POST' }))

export const useReceiveTransfer = () =>
  useInventoryMutation((id: string) => apiFetch<void>(`/api/inventory/documents/${id}/receive`, { method: 'POST' }))

export function useCancelDocument() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      apiFetch<void>(`/api/inventory/documents/${id}/cancel`, { method: 'POST', body: { reason } }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: inventoryKeys.all }),
    meta: { inlineError: true }, // shown inside the cancel dialog
  })
}
