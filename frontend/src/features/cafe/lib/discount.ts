import type { OrderDto, OrderLine } from '@/lib/api/types'

/** Quick reasons on the discount dialog (free text is allowed too). */
export const DISCOUNT_REASONS = ['Khách quen', 'Món làm lỗi', 'Khuyến mãi', 'Nhân viên quán']

export type DiscountTarget = { kind: 'order' } | { kind: 'line'; line: OrderLine }
export type DiscountMode = 'percent' | 'amount'

/** Price of a line before its discount. */
export function lineGross(line: OrderLine): number {
  return line.amount + line.discountAmount
}

/** Amount the discount would take off (preview — the server computes the real one, rounding like it does). */
export function discountAmount(mode: DiscountMode, value: number, base: number): number {
  if (value <= 0) return 0
  if (mode === 'percent') return Math.min(base, Math.round((base * Math.min(value, 100)) / 100))
  return Math.min(base, value)
}

/** Base the discount applies to: the subtotal for the whole bill, the line's price for an item. */
export function discountBase(order: OrderDto, target: DiscountTarget): number {
  return target.kind === 'order' ? order.subtotal : lineGross(target.line)
}

/** Every discount on the bill after this change (order-level + items), as the server will count it. */
export function totalDiscountAfter(order: OrderDto, target: DiscountTarget, amount: number): number {
  const lineDiscounts = order.lines.reduce((sum, line) => sum + line.discountAmount, 0)
  if (target.kind === 'order') return amount + lineDiscounts
  return order.discountAmount + lineDiscounts - target.line.discountAmount + amount
}

/** Preview of the server rule: above the cashier's limit a manager must approve (managers approve themselves). */
export function needsManagerApproval(order: OrderDto, totalAfter: number, isManager: boolean): boolean {
  return !isManager && totalAfter > order.cashierDiscountLimit
}
