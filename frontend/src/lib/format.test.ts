import { describe, expect, it } from 'vitest'
import { formatDuration, formatTime, formatVnd, formatVndNumber, minutesSince } from './format'

describe('format', () => {
  it('formats VND with dot separators and rounds to whole đồng', () => {
    expect(formatVndNumber(85000)).toBe('85.000')
    expect(formatVndNumber(1234567.6)).toBe('1.234.568')
    expect(formatVnd(0)).toBe('0 ₫')
  })

  it('uses a real minus sign for negative amounts', () => {
    expect(formatVndNumber(-20000)).toBe('−20.000')
  })

  it('shows time in Vietnam time zone', () => {
    expect(formatTime('2026-11-26T07:05:00Z')).toBe('14:05')
  })

  it('counts whole minutes and never goes negative', () => {
    const now = Date.parse('2026-11-26T07:30:00Z')
    expect(minutesSince('2026-11-26T07:17:40Z', now)).toBe(12)
    expect(minutesSince('2026-11-26T07:31:00Z', now)).toBe(0)
  })

  it('formats waiting time', () => {
    expect(formatDuration(12)).toBe('12 phút')
    expect(formatDuration(65)).toBe('1 giờ 05 phút')
  })
})
