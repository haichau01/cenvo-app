import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { CommissionReport } from '@/lib/api/types'
import { washKeys } from './keys'

export function useCommissionReport(from: string, to: string) {
  return useQuery({
    queryKey: washKeys.commissions(from, to),
    queryFn: () => apiFetch<CommissionReport>(`/api/reports/carwash-commissions?from=${from}&to=${to}`),
  })
}
