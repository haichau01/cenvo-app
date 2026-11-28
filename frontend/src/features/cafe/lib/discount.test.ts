import { describe, expect, it } from 'vitest'
import type { OrderDto, OrderLine } from '@/lib/api/types'
import { discountAmount, needsManagerApproval, totalDiscountAfter } from './discount'

const line = (id: string, amount: number, discount = 0) => ({ id, amount, discountAmount: discount }) as OrderLine
const order = (lines: OrderLine[], orderDiscount = 0) =>
  ({ lines, discountAmount: orderDiscount, subtotal: 100000, cashierDiscountLimit: 10000 }) as OrderDto

describe('discount preview', () => {
  it('rounds percent to whole VND and never exceeds the base', () => {
    expect(discountAmount('percent', 10, 55555)).toBe(5556)
    expect(discountAmount('amount', 200000, 100000)).toBe(100000)
    expect(discountAmount('percent', 0, 100000)).toBe(0)
  })

  it('counts item and order discounts together', () => {
    const a = line('a', 45000, 5000)
    const bill = order([a, line('b', 50000)], 0)
    expect(totalDiscountAfter(bill, { kind: 'order' }, 6000)).toBe(11000)
    expect(totalDiscountAfter(bill, { kind: 'line', line: a }, 2000)).toBe(2000)
  })

  it('asks for a manager above the cashier limit, never for a manager', () => {
    const bill = order([])
    expect(needsManagerApproval(bill, 10000, false)).toBe(false)
    expect(needsManagerApproval(bill, 10001, false)).toBe(true)
    expect(needsManagerApproval(bill, 50000, true)).toBe(false)
  })
})

describe('move summary', async () => {
  const { moveSummary } = await import('./move')
  const free = { id: 't2', name: 'Bàn 2', openOrderId: null } as never
  const busy = { id: 't3', name: 'Bàn 3', openOrderId: 'o3' } as never
  it('explains move, merge and split', () => {
    expect(moveSummary({ kind: 'table', table: free }, true, 3)).toBe('Khách chuyển sang Bàn 2.')
    expect(moveSummary({ kind: 'table', table: busy }, true, 3)).toBe('Cả hóa đơn sẽ gộp vào hóa đơn đang mở của Bàn 3.')
    expect(moveSummary({ kind: 'table', table: free }, false, 1)).toBe('1 món đã chọn sẽ tách sang hóa đơn mới ở Bàn 2.')
    expect(moveSummary({ kind: 'takeAway' }, false, 2)).toBe('2 món đã chọn sẽ sang một hóa đơn mang đi mới.')
  })
})
