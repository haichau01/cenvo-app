import type { MenuCategory } from '../lib/menu'
import { ALL_CATEGORIES } from '../lib/menu'

interface CategoryTabsProps {
  categories: MenuCategory[]
  value: string
  onChange: (categoryId: string) => void
}

export function CategoryTabs({ categories, value, onChange }: CategoryTabsProps) {
  const tabs = [{ id: ALL_CATEGORIES, name: 'Tất cả' }, ...categories]
  return (
    <div role="tablist" aria-label="Nhóm món" className="flex gap-2 overflow-x-auto px-6 pt-4 pb-1">
      {tabs.map((tab) => {
        const active = tab.id === value
        return (
          <button key={tab.id} type="button" role="tab" aria-selected={active} onClick={() => onChange(tab.id)}
            className={`h-12 shrink-0 rounded-full px-5 text-base font-semibold transition-colors duration-100
              ${active ? 'bg-(--module) text-white' : 'bg-surface text-ink active:bg-line/60'}`}>
            {tab.name}
          </button>
        )
      })}
    </div>
  )
}
