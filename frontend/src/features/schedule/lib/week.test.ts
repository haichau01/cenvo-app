import { describe, expect, it } from 'vitest'
import { addDays, dayLabel, hoursLabel, shiftHours, weekDays, weekStartOf } from './week'

describe('week', () => {
  it('starts on Monday; Sunday belongs to the week before', () => {
    expect(weekStartOf('2026-12-10')).toBe('2026-12-07')
    expect(weekStartOf('2026-12-13')).toBe('2026-12-07')
    expect(weekStartOf('2026-12-07')).toBe('2026-12-07')
  })

  it('crosses months and years', () => {
    expect(addDays('2026-12-28', 7)).toBe('2027-01-04')
    expect(weekDays('2026-11-30')[6]).toBe('2026-12-06')
  })

  it('labels days, hours and overnight shifts', () => {
    expect(dayLabel('2026-12-13')).toEqual({ weekday: 'CN', date: '13/12' })
    expect(shiftHours('22:00:00', '06:00:00')).toBe('22:00–06:00 (hôm sau)')
    expect(shiftHours('06:00:00', '12:00:00')).toBe('06:00–12:00')
    expect(hoursLabel(7.5)).toBe('7,5 giờ')
    expect(hoursLabel(48)).toBe('48 giờ')
  })
})
