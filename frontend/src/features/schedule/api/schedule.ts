import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { AssignResult, MyShift, ScheduleSettings, ScheduleWeek, ShiftTemplate, ShiftTemplateInput } from '@/lib/api/types'

export const scheduleKeys = {
  all: ['schedule'] as const,
  week: (weekStart: string) => ['schedule', 'week', weekStart] as const,
  templates: ['schedule', 'templates'] as const,
  settings: ['schedule', 'settings'] as const,
  mine: (from: string, to: string) => ['schedule', 'mine', from, to] as const,
}

export const useWeek = (weekStart: string) => useQuery({
  queryKey: scheduleKeys.week(weekStart),
  queryFn: () => apiFetch<ScheduleWeek>(`/api/schedule/week?date=${weekStart}`),
  placeholderData: keepPreviousData,
})

export const useTemplates = () =>
  useQuery({ queryKey: scheduleKeys.templates, queryFn: () => apiFetch<ShiftTemplate[]>('/api/schedule/templates') })

export const useScheduleSettings = () =>
  useQuery({ queryKey: scheduleKeys.settings, queryFn: () => apiFetch<ScheduleSettings>('/api/schedule/settings') })

export const useMySchedule = (from: string, to: string) => useQuery({
  queryKey: scheduleKeys.mine(from, to),
  queryFn: () => apiFetch<MyShift[]>(`/api/schedule/mine?from=${from}&to=${to}`),
})

function useScheduleMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSettled: () => void queryClient.invalidateQueries({ queryKey: scheduleKeys.all }),
    meta: { inlineError: true },
  })
}

export const useSaveTemplate = () =>
  useScheduleMutation(({ id, input }: { id: string | null; input: ShiftTemplateInput }) =>
    apiFetch<string>(id ? `/api/schedule/templates/${id}` : '/api/schedule/templates', { method: id ? 'PUT' : 'POST', body: input }))

export const useDeleteTemplate = () =>
  useScheduleMutation((id: string) => apiFetch<void>(`/api/schedule/templates/${id}`, { method: 'DELETE' }))

export const useSaveScheduleSettings = () =>
  useScheduleMutation((input: ScheduleSettings) => apiFetch<void>('/api/schedule/settings', { method: 'PUT', body: input }))

export const useAssign = () =>
  useScheduleMutation((input: { date: string; shiftTemplateId: string; employeeId: string }) =>
    apiFetch<AssignResult>('/api/schedule/assignments', { method: 'POST', body: input }))

export const useUnassign = () =>
  useScheduleMutation((id: string) => apiFetch<void>(`/api/schedule/assignments/${id}`, { method: 'DELETE' }))
