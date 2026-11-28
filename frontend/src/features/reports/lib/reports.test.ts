import { describe, expect, it } from 'vitest'
import { columnTotal, combinedChartDays, fillCafeDays, washChartDays } from './chart'
import { marginPercent, niceTicks, shiftDifference, sumByMethod } from './metrics'
import { addDays, matchingPreset, PRESETS } from './period'

const presetPeriod = (id: string, today: string) => PRESETS.find((p) => p.id === id)!.period(today)

describe('periods', () => {
  it('adds days across month and year ends', () => {
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28')
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01')
  })

  it('computes presets from the Vietnam date', () => {
    expect(presetPeriod('last7', '2026-11-27')).toEqual({ from: '2026-11-21', to: '2026-11-27' })
    expect(presetPeriod('thisMonth', '2026-11-27')).toEqual({ from: '2026-11-01', to: '2026-11-27' })
    expect(presetPeriod('lastMonth', '2026-03-15')).toEqual({ from: '2026-02-01', to: '2026-02-28' })
  })

  it('recognises a preset after manual edits', () => {
    expect(matchingPreset({ from: '2026-11-26', to: '2026-11-26' }, '2026-11-27')).toBe('yesterday')
    expect(matchingPreset({ from: '2026-11-02', to: '2026-11-27' }, '2026-11-27')).toBeNull()
  })
})

describe('metrics', () => {
  it('reports margin only when there is revenue', () => {
    expect(marginPercent(450_000, 1_000_000)).toBe(45)
    expect(marginPercent(0, 0)).toBeNull()
  })

  it('shows shift difference only for closed shifts', () => {
    expect(shiftDifference({ expectedCash: 756_000, countedCash: 600_000 })).toBe(-156_000)
    expect(shiftDifference({ expectedCash: null, countedCash: null })).toBeNull()
  })

  it('picks round axis steps', () => {
    expect(niceTicks(1_234_000)).toEqual([0, 500_000, 1_000_000, 1_500_000])
    expect(niceTicks(0)).toEqual([0, 1])
  })

  it('fills days without sales with zeros', () => {
    const filled = fillCafeDays([{ date: '2026-11-26', orders: 3, revenue: 90_000, discount: 0, cost: 30_000, grossProfit: 60_000 }],
      ['2026-11-25', '2026-11-26'])
    expect(filled.map((d) => d.revenue)).toEqual([0, 90_000])
  })
})

describe('revenue chart data', () => {
  const washDay = { date: '2026-11-26', tickets: 2, washRevenue: 160_000, membershipRevenue: 1_000_000, revenue: 1_160_000,
    supplyCost: 20_000, commission: 32_000, grossProfit: 1_108_000 }
  const cafeDay = { date: '2026-11-26', orders: 3, revenue: 90_000, discount: 0, cost: 30_000, grossProfit: 60_000 }

  it('stacks café under car wash and fills missing days with zero columns', () => {
    const days = combinedChartDays([cafeDay], [washDay], ['2026-11-25', '2026-11-26'])
    expect(days.map((d) => d.values)).toEqual([[0, 0], [90_000, 1_160_000]])
    expect(columnTotal(days[1])).toBe(1_250_000)
    expect(days[1].details[0]).toEqual({ label: 'Tổng doanh thu', value: 1_250_000, kind: 'money' })
  })

  it('shows car wash revenue including packages sold that day', () => {
    const [day] = washChartDays([washDay], ['2026-11-26'])
    expect(day.values).toEqual([1_160_000])
    expect(day.details.find((d) => d.label === 'Hóa chất + hoa hồng')?.value).toBe(52_000)
  })

  it('adds payment methods of both modules for display', () => {
    expect(sumByMethod({ Cash: 100, Transfer: 50 }, { Cash: 20, Card: 5 })).toEqual({ Cash: 120, Transfer: 50, Card: 5 })
  })
})
