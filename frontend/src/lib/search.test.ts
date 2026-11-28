import { describe, expect, it } from 'vitest'
import { foldText, searchBy } from './search'

describe('search', () => {
  it('ignores Vietnamese diacritics and case', () => {
    expect(foldText('Sữa TƯƠI Đà Lạt')).toBe('sua tuoi da lat')
  })

  const items = [
    { code: 'SUA-TUOI', name: 'Sữa tươi' },
    { code: 'SUA-DAC', name: 'Sữa đặc' },
    { code: 'DUONG', name: 'Đường cát' },
  ]
  const fields = (item: (typeof items)[number]) => [item.code, item.name]

  it('matches every word in any order, on code or name', () => {
    expect(searchBy(items, 'tuoi sua', fields, 10).map((i) => i.code)).toEqual(['SUA-TUOI'])
    expect(searchBy(items, 'duong', fields, 10).map((i) => i.code)).toEqual(['DUONG'])
    expect(searchBy(items, 'sua', fields, 1)).toHaveLength(1)
  })

  it('returns nothing for an empty query', () => {
    expect(searchBy(items, '  ', fields, 10)).toEqual([])
  })
})
