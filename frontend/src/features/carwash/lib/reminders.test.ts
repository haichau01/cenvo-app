import { describe, expect, it } from 'vitest'
import type { DueReminder } from '@/lib/api/types'
import { reminderKindLabel, reminderReason, reminderStatusLabel } from './reminders'

const base: DueReminder['reason'] = {
  kind: 'ReturnVisit', refId: 'r', customerId: 'c', customerName: 'Chị Lan', phone: '0901234567',
}

describe('reminderReason', () => {
  it('counts whole days since the last wash', () => {
    const now = Date.parse('2026-11-28T03:00:00Z')
    expect(reminderReason({ ...base, lastWashAt: '2026-11-08T03:00:00Z' }, now)).toBe('Rửa lần cuối 08/11/2026 · 20 ngày trước')
  })

  it('shows uses left and expiry for packages', () => {
    expect(reminderReason({ ...base, kind: 'MembershipExpiring', remainingUses: 3, expiresAt: '2026-12-01T05:00:00Z' }))
      .toBe('còn 3 lượt · hết hạn 01/12/2026')
    expect(reminderReason({ ...base, kind: 'MembershipLowUses', remainingUses: 0, expiresAt: '2026-12-01T05:00:00Z' }))
      .toBe('đã hết lượt · hết hạn 01/12/2026')
  })
})

describe('labels', () => {
  it('names kinds and statuses in Vietnamese', () => {
    expect(reminderKindLabel('MembershipLowUses')).toBe('Gói sắp hết lượt')
    expect(reminderStatusLabel('Failed')).toEqual({ label: 'Gửi lỗi', tone: 'stop' })
  })
})
