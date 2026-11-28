import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type {
  AttendanceDay, AttendanceEdit, AttendanceRow, CorrectInput, Holiday, HolidayInput, KioskCode, ManualPunchInput, MyAttendance,
  PunchResult, StaffMember, Timesheet,
} from '@/lib/api/types'
import { refreshDelay } from '../lib/attendance'

export const attendanceKeys = {
  all: ['attendance'] as const,
  kioskCode: ['attendance', 'kiosk', 'code'] as const,
  kioskStaff: ['attendance', 'kiosk', 'staff'] as const,
  mine: (month: string) => ['attendance', 'mine', month] as const,
  day: (date: string) => ['attendance', 'day', date] as const,
  toReview: ['attendance', 'to-review'] as const,
  timesheet: (month: string) => ['attendance', 'timesheet', month] as const,
  edits: (id: string) => ['attendance', 'edits', id] as const,
  holidays: (year: number) => ['attendance', 'holidays', year] as const,
  hasPin: ['attendance', 'has-pin'] as const,
}

/** The rotating QR code: refetched just before it expires, also when the tab was asleep. */
export const useKioskCode = () => useQuery({
  queryKey: attendanceKeys.kioskCode,
  queryFn: () => apiFetch<KioskCode>('/api/attendance/kiosk/code'),
  refetchInterval: (query) => (query.state.data ? refreshDelay(query.state.data.expiresAt, Date.now()) : 5_000),
  refetchIntervalInBackground: true,
  staleTime: 0,
})

export const useKioskStaff = () => useQuery({
  queryKey: attendanceKeys.kioskStaff,
  queryFn: () => apiFetch<StaffMember[]>('/api/attendance/kiosk/staff'),
  staleTime: 10 * 60_000,
})

export const useMyAttendance = (month: string) => useQuery({
  queryKey: attendanceKeys.mine(month),
  queryFn: () => apiFetch<MyAttendance>(`/api/attendance/mine?month=${month}`),
  placeholderData: keepPreviousData,
})

export const useAttendanceDay = (date: string) => useQuery({
  queryKey: attendanceKeys.day(date),
  queryFn: () => apiFetch<AttendanceDay>(`/api/attendance/day?date=${date}`),
  placeholderData: keepPreviousData,
})

export const useToReview = () =>
  useQuery({ queryKey: attendanceKeys.toReview, queryFn: () => apiFetch<AttendanceRow[]>('/api/attendance/to-review') })

export const useTimesheet = (month: string) => useQuery({
  queryKey: attendanceKeys.timesheet(month),
  queryFn: () => apiFetch<Timesheet>(`/api/attendance/timesheet?month=${month}`),
  placeholderData: keepPreviousData,
})

export const useEdits = (id: string) =>
  useQuery({ queryKey: attendanceKeys.edits(id), queryFn: () => apiFetch<AttendanceEdit[]>(`/api/attendance/records/${id}/edits`) })

export const useHolidays = (year: number) => useQuery({
  queryKey: attendanceKeys.holidays(year),
  queryFn: () => apiFetch<Holiday[]>(`/api/attendance/holidays?year=${year}`),
  staleTime: 10 * 60_000,
})

export const useHasPin = () =>
  useQuery({ queryKey: attendanceKeys.hasPin, queryFn: () => apiFetch<{ hasPin: boolean }>('/api/auth/me/pin') })

function useAttendanceMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSettled: () => void queryClient.invalidateQueries({ queryKey: attendanceKeys.all }),
    meta: { inlineError: true },
  })
}

export const usePunchByCode = () =>
  useAttendanceMutation((code: string) => apiFetch<PunchResult>('/api/attendance/punch', { method: 'POST', body: { code } }))

export const usePunchByPin = () =>
  useAttendanceMutation((input: { employeeId: string; pin: string }) =>
    apiFetch<PunchResult>('/api/attendance/kiosk/pin', { method: 'POST', body: input }))

export const useSetPin = () =>
  useAttendanceMutation((input: { currentPassword: string; pin: string }) =>
    apiFetch<void>('/api/auth/me/pin', { method: 'PUT', body: input }))

export const useCorrect = () =>
  useAttendanceMutation(({ id, input }: { id: string; input: CorrectInput }) =>
    apiFetch<void>(`/api/attendance/records/${id}/times`, { method: 'PUT', body: input }))

export const useAddManual = () =>
  useAttendanceMutation((input: ManualPunchInput) => apiFetch<string>('/api/attendance/records', { method: 'POST', body: input }))

export const useApprove = () =>
  useAttendanceMutation((id: string) => apiFetch<void>(`/api/attendance/records/${id}/approve`, { method: 'POST' }))

export const useReject = () =>
  useAttendanceMutation(({ id, reason }: { id: string; reason: string }) =>
    apiFetch<void>(`/api/attendance/records/${id}/reject`, { method: 'POST', body: { reason } }))

export const useApproveOvertime = () =>
  useAttendanceMutation(({ id, minutes }: { id: string; minutes: number }) =>
    apiFetch<void>(`/api/attendance/records/${id}/overtime`, { method: 'POST', body: { minutes } }))

export const useSaveHoliday = () =>
  useAttendanceMutation(({ id, input }: { id: string | null; input: HolidayInput }) =>
    apiFetch<string>(id ? `/api/attendance/holidays/${id}` : '/api/attendance/holidays', { method: id ? 'PUT' : 'POST', body: input }))

export const useDeleteHoliday = () =>
  useAttendanceMutation((id: string) => apiFetch<void>(`/api/attendance/holidays/${id}`, { method: 'DELETE' }))
