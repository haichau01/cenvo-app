import type { WashTicket } from '@/lib/api/types'

type SlipTicket = Pick<WashTicket, 'status' | 'isPaid'>

/**
 * One ticket prints as different slips over its life: the intake slip the customer keeps to collect the car,
 * then the paid receipt. An unpaid ticket is never called a receipt ("hóa đơn").
 */
export function washSlipTitle(ticket: SlipTicket): string {
  if (ticket.status === 'Cancelled') return 'Phiếu rửa đã hủy'
  if (ticket.isPaid) return 'Hóa đơn rửa xe'
  return 'Phiếu nhận xe'
}

/** Label of the amount line: what is still owed on an intake slip, what was paid on a receipt. */
export function washSlipTotalLabel(ticket: SlipTicket): string {
  return ticket.isPaid ? 'Khách đã trả' : 'Cần thanh toán'
}
