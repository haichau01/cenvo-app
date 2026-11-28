import { expect, it } from 'vitest'
import { punchMessage } from './punchMessage'

const base = { employeeName: 'Thu', at: '2026-11-28T01:05:00Z', shiftName: 'Sáng', needsApproval: false, lateMinutes: 0, workedMinutes: 0 }

it('tells the person what was recorded', () => {
  expect(punchMessage({ ...base, action: 'CheckIn', lateMinutes: 5 })).toBe('Thu đã vào ca Sáng lúc 08:05, muộn 5 phút.')
  expect(punchMessage({ ...base, action: 'CheckIn', needsApproval: true, shiftName: null }))
    .toBe('Thu đã vào lúc 08:05. Không có ca được xếp: chờ quản lý duyệt.')
  expect(punchMessage({ ...base, action: 'CheckOut', workedMinutes: 485 })).toBe('Thu đã ra ca lúc 08:05 — làm 8 giờ 05.')
})
