import { describe, expect, it } from 'vitest'
import type { KitchenTicket } from '@/lib/api/types'
import { groupByStatus, isLate, nextStatus, previousStatus, ticketPlace } from './kitchen'

function ticket(lineId: string, status: KitchenTicket['status']): KitchenTicket {
  return {
    orderId: 'o', orderCode: 'HD-1', tableName: 'Bàn 6', type: 'DineIn', lineId, name: 'Bạc xỉu',
    quantity: 1, note: null, toppings: [], status, sentAt: '2026-11-27T07:00:00Z',
  }
}

describe('kitchen flow', () => {
  it('moves one step forward, Done leaves the screen as Served', () => {
    expect(nextStatus('Sent')).toBe('Preparing')
    expect(nextStatus('Preparing')).toBe('Done')
    expect(nextStatus('Done')).toBe('Served')
  })

  it('undoes one step, never back to "not sent"', () => {
    expect(previousStatus('Done')).toBe('Preparing')
    expect(previousStatus('Preparing')).toBe('Sent')
    expect(previousStatus('Sent')).toBeNull()
  })

  it('groups tickets into the three columns and drops served ones', () => {
    const groups = groupByStatus([ticket('a', 'Sent'), ticket('b', 'Done'), ticket('c', 'Served'), ticket('d', 'Sent')])
    expect(groups.Sent.map((t) => t.lineId)).toEqual(['a', 'd'])
    expect(groups.Preparing).toEqual([])
    expect(groups.Done.map((t) => t.lineId)).toEqual(['b'])
  })

  it('names the place', () => {
    expect(ticketPlace({ type: 'DineIn', tableName: 'Bàn 6' })).toBe('Bàn 6')
    expect(ticketPlace({ type: 'TakeAway', tableName: null })).toBe('Mang đi')
  })

  it('flags late drinks that are not ready yet', () => {
    expect(isLate('Sent', 10)).toBe(true)
    expect(isLate('Preparing', 9)).toBe(false)
    expect(isLate('Done', 30)).toBe(false)
  })
})
