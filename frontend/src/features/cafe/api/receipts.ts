import { useMutation, useQuery } from '@tanstack/react-query'
import { createElement } from 'react'
import { apiFetch } from '@/lib/api/client'
import type { OrderDto, ReceiptDto } from '@/lib/api/types'
import { vnDate } from '@/lib/format'
import { printContent } from '@/lib/print'
import { queryClient } from '@/lib/queryClient'
import { Receipt } from '../components/receipt/Receipt'

const receiptQuery = (orderId: string) => ({
  queryKey: ['cafe', 'receipts', orderId],
  queryFn: () => apiFetch<ReceiptDto>(`/api/cafe/orders/${orderId}/receipt`),
  staleTime: 0, // always print the latest lines and totals
})

export const useReceipt = (orderId: string) => useQuery(receiptQuery(orderId))

/** Fetch the receipt fresh from the server, then print it. Errors surface as toasts (mutation cache). */
export function usePrintReceipt() {
  return useMutation({
    mutationFn: async (orderId: string) => {
      const receipt = await queryClient.fetchQuery(receiptQuery(orderId))
      await printContent(createElement(Receipt, { receipt, printedAt: new Date().toISOString() }))
    },
  })
}

/** Today's paid orders, newest first — the list to reprint from. */
export const useTodayPaidOrders = (enabled: boolean) => useQuery({
  queryKey: ['cafe', 'orders', 'paid-today'],
  queryFn: () => {
    const today = vnDate(Date.now())
    return apiFetch<OrderDto[]>(`/api/cafe/orders?from=${today}&to=${today}&status=Paid`)
  },
  enabled,
  staleTime: 0,
})
