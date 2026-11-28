import { describe, expect, it } from 'vitest'
import { receiptTitle } from './receipt'

describe('receipt title', () => {
  it('never calls an unpaid order a receipt', () => {
    expect(receiptTitle('Open')).toBe('Phiếu tạm tính')
    expect(receiptTitle('Paid')).toBe('Hóa đơn thanh toán')
    expect(receiptTitle('Cancelled')).toBe('Hóa đơn đã hủy')
  })
})
