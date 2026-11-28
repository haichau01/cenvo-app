import { describe, expect, it } from 'vitest'
import type { MenuItem } from '@/lib/api/types'
import { ALL_CATEGORIES, categoriesOf, itemsInCategory, splitMenu } from './menu'
import { previewChange, suggestCash } from '@/lib/payment'

function item(name: string, categoryId: string | null, isTopping = false): MenuItem {
  return {
    productId: name, code: name, name, categoryId, categoryName: categoryId ? `Nhóm ${categoryId}` : null,
    categorySort: 0, price: 10_000, imageUrl: null, isTopping, showOnPos: true, sortOrder: 0, hasRecipe: true,
  }
}

describe('menu', () => {
  const menu = [item('Bạc xỉu', 'cf'), item('Trà đào', 'tra'), item('Trân châu', 'tp', true), item('Bánh', null)]

  it('keeps toppings out of the grid', () => {
    const { items, toppings } = splitMenu(menu)
    expect(items.map((i) => i.name)).toEqual(['Bạc xỉu', 'Trà đào', 'Bánh'])
    expect(toppings.map((i) => i.name)).toEqual(['Trân châu'])
  })

  it('lists categories in menu order with a fallback name', () => {
    const { items } = splitMenu(menu)
    expect(categoriesOf(items).map((c) => c.name)).toEqual(['Nhóm cf', 'Nhóm tra', 'Khác'])
  })

  it('filters by category', () => {
    const { items } = splitMenu(menu)
    expect(itemsInCategory(items, ALL_CATEGORIES)).toHaveLength(3)
    expect(itemsInCategory(items, 'none').map((i) => i.name)).toEqual(['Bánh'])
  })
})

describe('suggestCash', () => {
  it('offers the exact amount then common notes', () => {
    expect(suggestCash(85_000)).toEqual([85_000, 90_000, 100_000, 200_000])
  })

  it('does not duplicate when the total is already a round note', () => {
    expect(suggestCash(100_000)).toEqual([100_000, 200_000, 500_000])
  })

  it('returns nothing for an empty order', () => {
    expect(suggestCash(0)).toEqual([])
  })

  it('previews change and debt', () => {
    expect(previewChange(85_000, 100_000)).toBe(15_000)
    expect(previewChange(85_000, 50_000)).toBe(-35_000)
  })
})
