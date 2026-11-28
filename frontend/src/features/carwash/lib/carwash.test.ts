import { describe, expect, it } from 'vitest'
import type { QueueTicket, WashMembership, WashOffering } from '@/lib/api/types'
import { membershipFor, previewTotal, priceFor } from './intake'
import { cardClock, estimatedWait } from './queue'
import { groupByStatus } from './status'

const now = Date.parse('2026-11-28T03:00:00Z')
const minutesAgo = (minutes: number) => new Date(now - minutes * 60_000).toISOString()

function ticket(overrides: Partial<QueueTicket>): QueueTicket {
  return {
    id: 't', code: 'RX', plate: '51A12345', vehicleType: 'Sedan', vehicleDescription: null, customerName: null,
    status: 'Waiting', isPaid: false, total: 0, estimatedMinutes: 30, services: [], staff: [], photoCount: 0,
    createdAt: minutesAgo(10), startedAt: null, completedAt: null, deliveredAt: null, ...overrides,
  }
}

const foam: WashOffering = {
  id: 'foam', code: 'RX-BOT', name: 'Rửa bọt tuyết', durationMinutes: 30, commissionType: 'Percent', commissionValue: 20,
  sortOrder: 1, isActive: true, prices: [{ vehicleType: 'Sedan', price: 100000 }], supplies: [],
}

function membership(overrides: Partial<WashMembership>): WashMembership {
  return {
    id: 'm', code: 'TV', customerId: 'c', packageName: 'Gói', offeringId: 'foam', offeringName: 'Rửa bọt tuyết',
    vehicleType: 'Sedan', totalUses: 12, remainingUses: 5, price: 1000000, paidAt: minutesAgo(100),
    expiresAt: minutesAgo(-1000), status: 'Active', isUsable: true, ...overrides,
  }
}

describe('car wash queue', () => {
  it('groups tickets into lanes and drops cancelled ones', () => {
    const groups = groupByStatus([ticket({ id: 'a' }), ticket({ id: 'b', status: 'Washing' }), ticket({ id: 'c', status: 'Cancelled' })])
    expect(groups.Waiting.map((t) => t.id)).toEqual(['a'])
    expect(groups.Washing.map((t) => t.id)).toEqual(['b'])
  })

  it('flags a wash running longer than planned', () => {
    expect(cardClock(ticket({ status: 'Washing', startedAt: minutesAgo(40) }), now)).toEqual({ label: 'Đang rửa', minutes: 40, late: true })
    expect(cardClock(ticket({ status: 'Waiting' }), now).label).toBe('Đã chờ')
  })

  it('estimates the wait from the work ahead, shared between bays', () => {
    const tickets = [
      ticket({ id: 'w', status: 'Washing', startedAt: minutesAgo(10), estimatedMinutes: 30 }), // 20 left
      ticket({ id: 'a', estimatedMinutes: 40 }),
      ticket({ id: 'b', estimatedMinutes: 15 }),
    ]
    expect(estimatedWait(tickets, 'a', now, 1)).toBe(20)
    expect(estimatedWait(tickets, 'b', now, 2)).toBe(30) // (20 + 40) / 2
  })
})

describe('car wash intake', () => {
  it('prices per vehicle type and knows unpriced types', () => {
    expect(priceFor(foam, 'Sedan')).toBe(100000)
    expect(priceFor(foam, 'Suv')).toBeNull()
  })

  it('offers only a usable membership for the same service and vehicle type', () => {
    expect(membershipFor([membership({})], 'foam', 'Sedan')?.id).toBe('m')
    expect(membershipFor([membership({ vehicleType: 'Suv' })], 'foam', 'Sedan')).toBeNull()
    expect(membershipFor([membership({ isUsable: false })], 'foam', 'Sedan')).toBeNull()
  })

  it('previews the total without services covered by a membership', () => {
    expect(previewTotal([{ offeringId: 'foam', membershipId: null }], [foam], 'Sedan')).toBe(100000)
    expect(previewTotal([{ offeringId: 'foam', membershipId: 'm' }], [foam], 'Sedan')).toBe(0)
  })
})

describe('photo', () => {
  it('shrinks the long side to the limit and keeps the ratio', async () => {
    const { fitWithin } = await import('./photo')
    expect(fitWithin(4000, 3000)).toEqual({ width: 1600, height: 1200 })
    expect(fitWithin(800, 600)).toEqual({ width: 800, height: 600 })
  })
})

describe('queue card action', async () => {
  const { cardAction } = await import('./status')
  it('asks for a washer before starting', () => {
    expect(cardAction(ticket({ staff: [] }), true)?.label).toBe('Gán người rửa')
    expect(cardAction(ticket({ staff: ['A'] }), true)?.label).toBe('Bắt đầu rửa')
  })

  it('takes payment before delivering, only for cashiers', () => {
    expect(cardAction(ticket({ status: 'Done' }), true)?.kind).toBe('pay')
    expect(cardAction(ticket({ status: 'Done' }), false)?.kind).toBe('open')
    expect(cardAction(ticket({ status: 'Done', isPaid: true }), false)?.label).toBe('Giao xe')
    expect(cardAction(ticket({ status: 'Delivered' }), true)).toBeNull()
  })
})

describe('service form', async () => {
  const { toOfferingForm, toOfferingInput } = await import('./offeringForm')
  it('round-trips a service and leaves unpriced vehicle types out', () => {
    const form = toOfferingForm(foam)
    expect(form.prices.Sedan).toBe(100000)
    expect(form.prices.Suv).toBe(0)
    const input = toOfferingInput({ ...form, supplies: [{ productId: '', unitId: null, quantity: 5 }] })
    expect(input.prices).toEqual([{ vehicleType: 'Sedan', price: 100000 }])
    expect(input.supplies).toEqual([])
  })
})
