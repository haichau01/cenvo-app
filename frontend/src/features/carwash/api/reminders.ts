import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { DueReminder, ReminderKind, ReminderLog, ReminderSettings, ReminderSettingsInput, SendRemindersResult } from '@/lib/api/types'
import { washKeys } from './keys'

export function useDueReminders() {
  return useQuery({ queryKey: washKeys.remindersDue, queryFn: () => apiFetch<DueReminder[]>('/api/carwash/reminders/due') })
}

export function useReminderSettings() {
  return useQuery({ queryKey: washKeys.reminderSettings, queryFn: () => apiFetch<ReminderSettings>('/api/carwash/reminders/settings') })
}

export function useReminderHistory(from: string, to: string) {
  return useQuery({
    queryKey: washKeys.reminderHistory(from, to),
    queryFn: () => apiFetch<ReminderLog[]>(`/api/carwash/reminders/history?from=${from}&to=${to}`),
  })
}

function useReminderMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    // Settings change who is due and the message; a send moves people from "due" to the log.
    onSettled: () => void queryClient.invalidateQueries({ queryKey: washKeys.reminders }),
    meta: { inlineError: true },
  })
}

export const useSaveReminderSettings = () =>
  useReminderMutation((input: ReminderSettingsInput) =>
    apiFetch<void>('/api/carwash/reminders/settings', { method: 'PUT', body: input }))

export const useSendReminders = () =>
  useReminderMutation(({ kind, refIds }: { kind: ReminderKind; refIds: string[] }) =>
    apiFetch<SendRemindersResult>('/api/carwash/reminders/send', { method: 'POST', body: { kind, refIds } }))

/** Counter staff or managers: the customer asked to stop (or restart) reminder messages. */
export function useSetAcceptsReminders() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ customerId, accepts }: { customerId: string; accepts: boolean }) =>
      apiFetch<void>(`/api/carwash/customers/${customerId}/reminders`, { method: 'PUT', body: { accepts } }),
    onSettled: (_data, _error, { customerId }) => {
      void queryClient.invalidateQueries({ queryKey: washKeys.customer(customerId) })
      void queryClient.invalidateQueries({ queryKey: washKeys.reminders })
    },
    meta: { inlineError: true },
  })
}
