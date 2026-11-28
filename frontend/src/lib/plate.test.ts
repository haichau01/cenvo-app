import { describe, expect, it } from 'vitest'
import { formatPlate, isPlausiblePlate, normalizePlate } from './plate'

describe('plate', () => {
  it('normalizes like the backend', () => {
    expect(normalizePlate(' 51a-123.45 ')).toBe('51A12345')
  })

  it('shows plates with dash and dot', () => {
    expect(formatPlate('51A12345')).toBe('51A-123.45')
    expect(formatPlate('30K199999')).toBe('30K1-999.99')
    expect(formatPlate('51LD1234')).toBe('51LD-1234')
  })

  it('keeps unknown shapes as stored', () => {
    expect(formatPlate('NG12345AB')).toBe('NG12345AB')
  })

  it('checks the length the backend accepts', () => {
    expect(isPlausiblePlate('51A1')).toBe(false)
    expect(isPlausiblePlate('51A-123.45')).toBe(true)
  })
})
