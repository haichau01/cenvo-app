import { describe, expect, it } from 'vitest'
import { formatMinutes, fromVnLocalInput, isValidPin, monthTitle, punchUrl, refreshDelay, rowStatus, shiftMonth, toVnLocalInput } from './attendance'

describe('Vietnam local time inputs', () => {
  it('round-trips through datetime-local in UTC+7', () => {
    expect(toVnLocalInput('2026-11-28T01:05:00Z')).toBe('2026-11-28T08:05')
    expect(fromVnLocalInput('2026-11-28T08:05')).toBe('2026-11-28T01:05:00.000Z')
    expect(fromVnLocalInput('2026-11-29T02:00')).toBe('2026-11-28T19:00:00.000Z') // after midnight VN
  })
})

describe('formatMinutes', () => {
  it('shows minutes, whole hours and hours with minutes', () => {
    expect(formatMinutes(45)).toBe('45 phút')
    expect(formatMinutes(480)).toBe('8 giờ')
    expect(formatMinutes(485)).toBe('8 giờ 05')
  })
})

describe('months', () => {
  it('moves across years and titles the month', () => {
    expect(shiftMonth('2026-12-15', 1)).toBe('2027-01-01')
    expect(shiftMonth('2026-01-31', -1)).toBe('2025-12-01')
    expect(monthTitle('2026-11-01')).toBe('Tháng 11/2026')
  })
})

describe('kiosk code', () => {
  it('builds the phone link and refreshes before expiry', () => {
    expect(punchUrl('https://quan.vn', 'a.b+c')).toBe('https://quan.vn/cham-cong/quet?ma=a.b%2Bc')
    const now = Date.parse('2026-11-28T01:00:00Z')
    expect(refreshDelay('2026-11-28T01:00:30Z', now)).toBe(25_000)
    expect(refreshDelay('2026-11-28T01:00:02Z', now)).toBe(1_000)
  })
})

describe('rowStatus', () => {
  it('puts what needs the manager first', () => {
    expect(rowStatus({ approval: 'Pending', forgotten: true, checkOutAt: null }).label).toBe('Quên chấm ra')
    expect(rowStatus({ approval: 'Pending', forgotten: false, checkOutAt: '2026-11-28T09:00:00Z' }).label).toBe('Chờ duyệt')
    expect(rowStatus({ approval: 'NotNeeded', forgotten: false, checkOutAt: null }).label).toBe('Đang làm')
    expect(rowStatus({ approval: 'Approved', forgotten: false, checkOutAt: '2026-11-28T09:00:00Z' }).tone).toBe('go')
    expect(rowStatus({ approval: 'Rejected', forgotten: false, checkOutAt: '2026-11-28T09:00:00Z' }).tone).toBe('stop')
  })
})

it('accepts 4 to 6 digit PINs only', () => {
  expect(isValidPin('2468')).toBe(true)
  expect(isValidPin('123')).toBe(false)
  expect(isValidPin('12a4')).toBe(false)
})
