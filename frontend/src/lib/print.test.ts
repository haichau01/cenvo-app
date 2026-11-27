import { describe, expect, it } from 'vitest'
import { pageCss } from './print'

describe('receipt page size', () => {
  it('uses a valid two-length @page size (width × measured height)', () => {
    expect(pageCss('80mm', 128)).toContain('size: 80mm 128mm;')
    expect(pageCss('58mm', 140)).toContain('size: 58mm 140mm;')
  })
})
