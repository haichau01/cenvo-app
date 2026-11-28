import type { DailyRevenue, WashDailyRevenue } from '@/lib/api/types'

/** One series of the revenue chart. Colour follows the module (design system): Cà phê = robusta, Rửa xe = foam. */
export interface ChartSeries {
  label: string
  /** Tailwind fill class of the bar segment and its legend swatch. */
  fill: string
}

export interface ChartDetail {
  label: string
  value: number
  kind: 'money' | 'count'
}

/** One column: a value per series (stacked bottom-up in series order) and the exact numbers for the tooltip. */
export interface ChartDay {
  date: string
  values: number[]
  details: ChartDetail[]
}

export const CAFE_SERIES: ChartSeries = { label: 'Cà phê', fill: 'fill-robusta' }
export const WASH_SERIES: ChartSeries = { label: 'Rửa xe', fill: 'fill-foam' }

const money = (label: string, value: number): ChartDetail => ({ label, value, kind: 'money' })
const count = (label: string, value: number): ChartDetail => ({ label, value, kind: 'count' })

const emptyCafeDay = (date: string): DailyRevenue => ({ date, orders: 0, revenue: 0, discount: 0, cost: 0, grossProfit: 0 })
const emptyWashDay = (date: string): WashDailyRevenue => ({
  date, tickets: 0, washRevenue: 0, membershipRevenue: 0, revenue: 0, supplyCost: 0, commission: 0, grossProfit: 0,
})

/** Every day of the period, with zeros for days without sales — a bar chart must not skip dates. */
function byDay<T extends { date: string }>(daily: T[], days: string[], empty: (date: string) => T): T[] {
  const byDate = new Map(daily.map((day) => [day.date, day]))
  return days.map((date) => byDate.get(date) ?? empty(date))
}

export const fillCafeDays = (daily: DailyRevenue[], days: string[]) => byDay(daily, days, emptyCafeDay)
export const fillWashDays = (daily: WashDailyRevenue[], days: string[]) => byDay(daily, days, emptyWashDay)

export function cafeChartDays(daily: DailyRevenue[], days: string[]): ChartDay[] {
  return fillCafeDays(daily, days).map((day) => ({
    date: day.date,
    values: [day.revenue],
    details: [money('Doanh thu', day.revenue), money('Giá vốn', day.cost), money('Lãi gộp', day.grossProfit), count('Hóa đơn', day.orders)],
  }))
}

export function washChartDays(daily: WashDailyRevenue[], days: string[]): ChartDay[] {
  return fillWashDays(daily, days).map((day) => ({
    date: day.date,
    values: [day.revenue],
    details: [
      money('Doanh thu', day.revenue), money('Bán gói', day.membershipRevenue),
      money('Hóa chất + hoa hồng', day.supplyCost + day.commission), money('Lãi gộp', day.grossProfit), count('Xe', day.tickets),
    ],
  }))
}

/** Café and car wash revenue stacked per day (café at the bottom, as in the series order). */
export function combinedChartDays(cafe: DailyRevenue[], wash: WashDailyRevenue[], days: string[]): ChartDay[] {
  const washDays = fillWashDays(wash, days)
  return fillCafeDays(cafe, days).map((cafeDay, index) => {
    const washDay = washDays[index]
    return {
      date: cafeDay.date,
      values: [cafeDay.revenue, washDay.revenue],
      details: [
        money('Tổng doanh thu', cafeDay.revenue + washDay.revenue), money('Cà phê', cafeDay.revenue), money('Rửa xe', washDay.revenue),
        money('Lãi gộp', cafeDay.grossProfit + washDay.grossProfit),
      ],
    }
  })
}

/** Height of a column = sum of its stacked values. */
export const columnTotal = (day: ChartDay) => day.values.reduce((sum, value) => sum + value, 0)
