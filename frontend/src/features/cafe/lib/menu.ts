import type { MenuItem } from '@/lib/api/types'

export interface MenuCategory {
  id: string
  name: string
}

export const ALL_CATEGORIES = 'all'
const UNCATEGORIZED = 'none'

/** The POS grid shows drinks/food; toppings are only offered inside the item options sheet. */
export function splitMenu(menu: MenuItem[]): { items: MenuItem[]; toppings: MenuItem[] } {
  return {
    items: menu.filter((item) => !item.isTopping),
    toppings: menu.filter((item) => item.isTopping),
  }
}

/** Categories in menu order (the backend already sorts by category, then item). */
export function categoriesOf(items: MenuItem[]): MenuCategory[] {
  const seen = new Map<string, MenuCategory>()
  for (const item of items) {
    const id = item.categoryId ?? UNCATEGORIZED
    if (!seen.has(id)) seen.set(id, { id, name: item.categoryName ?? 'Khác' })
  }
  return [...seen.values()]
}

export function itemsInCategory(items: MenuItem[], categoryId: string): MenuItem[] {
  if (categoryId === ALL_CATEGORIES) return items
  return items.filter((item) => (item.categoryId ?? UNCATEGORIZED) === categoryId)
}
