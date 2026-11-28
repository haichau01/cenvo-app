import { useMutation } from '@tanstack/react-query'
import { createElement } from 'react'
import { apiFetch } from '@/lib/api/client'
import type { MembershipReceipt, WashTicketReceipt } from '@/lib/api/types'
import { printContent } from '@/lib/print'
import { MembershipSlip } from '../components/slip/MembershipSlip'
import { WashSlip } from '../components/slip/WashSlip'

/** Device setting: print the intake slip as soon as a ticket is created. */
export const PRINT_INTAKE_KEY = 'cenvo.carwash.printIntake'
/** Device setting: print the receipt after taking payment (ticket or membership). */
export const PRINT_AFTER_PAY_KEY = 'cenvo.carwash.printAfterPay'

/** Always fetched fresh (no cache), so the slip shows the latest services, payment and status. Errors surface as toasts. */
export function usePrintWashTicket() {
  return useMutation({
    mutationFn: async (ticketId: string) => {
      const receipt = await apiFetch<WashTicketReceipt>(`/api/carwash/tickets/${ticketId}/receipt`)
      await printContent(createElement(WashSlip, { receipt, printedAt: new Date().toISOString() }))
    },
  })
}

export function usePrintMembership() {
  return useMutation({
    mutationFn: async (membershipId: string) => {
      const receipt = await apiFetch<MembershipReceipt>(`/api/carwash/memberships/${membershipId}/receipt`)
      await printContent(createElement(MembershipSlip, { receipt, printedAt: new Date().toISOString() }))
    },
  })
}
