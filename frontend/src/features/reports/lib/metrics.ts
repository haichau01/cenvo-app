import type { DailyRevenue } from '@/lib/api/types'

/** Gross margin in whole percent; null when there is no revenue (a 0% would be misleading). */
export function marginPercent(grossProfit: number, revenue: number): number | null {
  return revenue > 0 ? Math.round((grossProfit / revenue) * 100) : null
}

/** Share of a part in a total, 0–1 (0 when the total is 0). */
export function share(part: number, total: number): number {
  return total > 0 ? part / total : 0
}

/** Cash counted − cash expected; null while the shift is still open. Negative = missing money. */
export function shiftDifference(shift: { expectedCash?: number | null; countedCash?: number | null }): number | null {
  if (shift.expectedCash == null || shift.countedCash == null) return null
  return shift.countedCash - shift.expectedCash
}

/**
 * Axis maximum and ticks: a "nice" round step (1, 2 or 5 × 10ⁿ) so gridlines land on readable amounts.
 * Always at least one step above zero, so an empty period still draws a baseline.
 */
export function niceTicks(max: number, targetCount = 4): number[] {
  if (max <= 0) return [0, 1]
  const rough = max / targetCount
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const step = [1, 2, 5, 10].map((m) => m * magnitude).find((candidate) => candidate >= rough)!
  const ticks: number[] = []
  for (let value = 0; value < max + step; value += step) ticks.push(value)
  return ticks
}

/** Every day of the period, with zeros for days without sales — a bar chart must not skip dates. */
export function fillDays(daily: DailyRevenue[], days: string[]): DailyRevenue[] {
  const byDate = new Map(daily.map((day) => [day.date, day]))
  return days.map((date) => byDate.get(date) ?? { date, orders: 0, revenue: 0, discount: 0, cost: 0, grossProfit: 0 })
}
