import type { KitchenStatus, OrderDto, OrderLine } from '@/lib/api/types'

/** Lines not yet sent to the kitchen — what "Gửi pha chế" will send. */
export function unsentLines(order: OrderDto): OrderLine[] {
  return order.lines.filter((line) => line.kitchenStatus === 'New')
}

export function totalQuantity(order: OrderDto): number {
  return order.lines.reduce((sum, line) => sum + line.quantity, 0)
}

export function orderTitle(order: Pick<OrderDto, 'type' | 'tableName'>): string {
  return order.type === 'TakeAway' ? 'Mang đi' : (order.tableName ?? 'Tại bàn')
}

interface KitchenStatusView {
  label: string
  /** Token class for the status dot. */
  dot: string
}

/** Single place mapping backend kitchen statuses to Vietnamese labels + colors. */
export const kitchenStatusView: Record<KitchenStatus, KitchenStatusView> = {
  New: { label: 'Chưa gửi', dot: 'bg-line' },
  Sent: { label: 'Đã gửi', dot: 'bg-lane' },
  Preparing: { label: 'Đang pha', dot: 'bg-robusta' },
  Done: { label: 'Đã xong', dot: 'bg-go' },
  Served: { label: 'Đã mang ra', dot: 'bg-ink-muted' },
}
