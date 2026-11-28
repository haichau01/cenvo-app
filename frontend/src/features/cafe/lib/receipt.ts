import type { OrderDto } from '@/lib/api/types'

/** Heading of the printed slip: an open order is only a pre-bill, never a receipt. */
export function receiptTitle(status: OrderDto['status']): string {
  if (status === 'Open') return 'Phiếu tạm tính'
  if (status === 'Cancelled') return 'Hóa đơn đã hủy'
  return 'Hóa đơn thanh toán'
}
