import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { CreateTicketInput, QueueTicket, TenderedPayment, TicketHistory, WashTicket } from '@/lib/api/types'
import { washKeys } from './keys'

export const queueQuery = {
  queryKey: washKeys.queue,
  queryFn: () => apiFetch<QueueTicket[]>('/api/carwash/tickets/queue'),
  staleTime: 0,
}

export const useWashQueue = () => useQuery(queueQuery)

export function useTicket(id: string | null) {
  return useQuery({
    queryKey: washKeys.ticket(id ?? ''),
    queryFn: () => apiFetch<WashTicket>(`/api/carwash/tickets/${id}`),
    enabled: id !== null,
    staleTime: 0,
  })
}

export function useTicketHistory(from: string, to: string, q: string) {
  const params = new URLSearchParams({ from, to, q })
  return useQuery({
    queryKey: washKeys.history(from, to, q),
    queryFn: () => apiFetch<TicketHistory[]>(`/api/carwash/tickets?${params}`),
  })
}

/**
 * Edits of one ticket run one after another (same scope): the ticket has an optimistic-concurrency version,
 * so two parallel taps would fail with 409.
 */
const ticketEdit = { scope: { id: 'carwash-ticket-edit' } }

/** Every ticket mutation returns the full ticket: store it, then refresh the queue and the customer's page. */
function useTicketMutation<TVariables>(request: (variables: TVariables) => Promise<WashTicket>, inlineError = false) {
  const queryClient = useQueryClient()
  return useMutation({
    ...ticketEdit,
    mutationFn: request,
    onSuccess: (ticket) => {
      queryClient.setQueryData(washKeys.ticket(ticket.id), ticket)
      void queryClient.invalidateQueries({ queryKey: washKeys.queue })
      void queryClient.invalidateQueries({ queryKey: washKeys.customers })
    },
    meta: { inlineError },
  })
}

const post = <T>(path: string, body?: unknown) => apiFetch<T>(path, { method: 'POST', body: body ?? {} })

export const useCreateTicket = () =>
  useTicketMutation((input: CreateTicketInput) => post<WashTicket>('/api/carwash/tickets', input), true)

export type FlowStep = 'start' | 'return' | 'complete' | 'deliver'

export const useMoveTicket = () =>
  useTicketMutation(({ id, step }: { id: string; step: FlowStep }) => post<WashTicket>(`/api/carwash/tickets/${id}/${step}`))

export const usePayTicket = () =>
  useTicketMutation(({ id, payments }: { id: string; payments: TenderedPayment[] }) =>
    post<WashTicket>(`/api/carwash/tickets/${id}/pay`, { payments }), true)

export const useCancelTicket = () =>
  useTicketMutation(({ id, reason }: { id: string; reason: string }) =>
    post<WashTicket>(`/api/carwash/tickets/${id}/cancel`, { reason }), true)

export const useAddService = () =>
  useTicketMutation(({ id, offeringId, membershipId }: { id: string; offeringId: string; membershipId: string | null }) =>
    post<WashTicket>(`/api/carwash/tickets/${id}/services`, { offeringId, membershipId }))

export const useRemoveService = () =>
  useTicketMutation(({ id, lineId }: { id: string; lineId: string }) =>
    apiFetch<WashTicket>(`/api/carwash/tickets/${id}/services/${lineId}`, { method: 'DELETE' }))

export const useAssignStaff = () =>
  useTicketMutation(({ id, staffIds }: { id: string; staffIds: string[] }) =>
    apiFetch<WashTicket>(`/api/carwash/tickets/${id}/staff`, { method: 'PUT', body: { staffIds } }))
