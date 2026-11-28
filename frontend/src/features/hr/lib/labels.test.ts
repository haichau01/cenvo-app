import { describe, expect, it } from 'vitest'
import { adjustmentSign, employeeStatus, firstOfMonth, monthLabel, nextMonth, roleLabel } from './labels'

const base = { leaveDate: null, hasProfile: true, contractEndsSoon: false, isActive: true }

describe('employeeStatus', () => {
  it('puts leaving first, then account lock, missing profile and contract end', () => {
    expect(employeeStatus({ ...base, leaveDate: '2026-11-20', isActive: false }).label).toBe('Đã nghỉ')
    expect(employeeStatus({ ...base, isActive: false }).label).toBe('Tài khoản khóa')
    expect(employeeStatus({ ...base, hasProfile: false }).label).toBe('Chưa có hồ sơ')
    expect(employeeStatus({ ...base, contractEndsSoon: true })).toEqual({ label: 'Sắp hết hợp đồng', tone: 'lane' })
    expect(employeeStatus(base).label).toBe('Đang làm')
  })
})

describe('labels', () => {
  it('names roles in Vietnamese and keeps unknown ones', () => {
    expect(roleLabel('BranchManager')).toBe('Quản lý chi nhánh')
    expect(roleLabel('Other')).toBe('Other')
  })

  it('advances and penalties come out of pay', () => {
    expect(adjustmentSign('Advance')).toBe(-1)
    expect(adjustmentSign('Penalty')).toBe(-1)
    expect(adjustmentSign('Bonus')).toBe(1)
  })
})

describe('months', () => {
  it('rolls over the year and labels months in Vietnamese', () => {
    expect(nextMonth('2026-12-15')).toBe('2027-01')
    expect(nextMonth('2026-11-28')).toBe('2026-12')
    expect(firstOfMonth('2026-12')).toBe('2026-12-01')
    expect(monthLabel('2026-03-01')).toBe('Tháng 3/2026')
  })
})
