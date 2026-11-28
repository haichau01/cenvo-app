import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { Branch, ContractInput, EmployeeDetail, EmployeeInput, EmployeeListItem, SalaryRateInput } from '@/lib/api/types'
import { hrKeys } from './keys'

export const useEmployees = () =>
  useQuery({ queryKey: hrKeys.employees, queryFn: () => apiFetch<EmployeeListItem[]>('/api/hr/employees') })

export const useEmployee = (id: string | null) => useQuery({
  queryKey: hrKeys.employee(id ?? ''),
  queryFn: () => apiFetch<EmployeeDetail>(`/api/hr/employees/${id}`),
  enabled: id !== null,
})

export const useBranches = () =>
  useQuery({ queryKey: hrKeys.branches, queryFn: () => apiFetch<Branch[]>('/api/branches'), staleTime: 5 * 60_000 })

function useEmployeeMutation<TVariables, TResult>(request: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSettled: () => void queryClient.invalidateQueries({ queryKey: hrKeys.employees }),
    meta: { inlineError: true },
  })
}

/** Creates (id null) or updates the account + profile; returns the employee (= account) id. */
export const useSaveEmployee = () =>
  useEmployeeMutation(({ id, input }: { id: string | null; input: EmployeeInput }) =>
    apiFetch<string>(id ? `/api/hr/employees/${id}` : '/api/hr/employees', { method: id ? 'PUT' : 'POST', body: input }))

export const useLeave = () =>
  useEmployeeMutation(({ id, leaveDate }: { id: string; leaveDate: string }) =>
    apiFetch<void>(`/api/hr/employees/${id}/leave`, { method: 'POST', body: { leaveDate } }))

export const useRehire = () =>
  useEmployeeMutation((id: string) => apiFetch<void>(`/api/hr/employees/${id}/rehire`, { method: 'POST' }))

export const useSaveContract = () =>
  useEmployeeMutation(({ id, contractId, input }: { id: string; contractId: string | null; input: ContractInput }) =>
    apiFetch<string>(contractId ? `/api/hr/employees/${id}/contracts/${contractId}` : `/api/hr/employees/${id}/contracts`,
      { method: contractId ? 'PUT' : 'POST', body: input }))

export const useAddSalaryRate = () =>
  useEmployeeMutation(({ id, input }: { id: string; input: SalaryRateInput }) =>
    apiFetch<string>(`/api/hr/employees/${id}/salary-rates`, { method: 'POST', body: input }))

export const useRemoveSalaryRate = () =>
  useEmployeeMutation(({ id, rateId }: { id: string; rateId: string }) =>
    apiFetch<string>(`/api/hr/employees/${id}/salary-rates/${rateId}`, { method: 'DELETE' }))
