import { useMemo, useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { MenuItem } from '@/lib/api/types'
import { useMenu } from '../api/setup'
import { ALL_CATEGORIES, categoriesOf, itemsInCategory, splitMenu } from '../lib/menu'
import { CategoryTabs } from './CategoryTabs'
import { MenuTile } from './MenuTile'

interface MenuBoardProps {
  /** Tap = add 1 straight away (the fast path). */
  onAdd: (item: MenuItem) => void
  /** "Tùy chọn" = quantity, toppings, note. */
  onOptions: (item: MenuItem) => void
  disabled: boolean
}

export function MenuBoard({ onAdd, onOptions, disabled }: MenuBoardProps) {
  const menu = useMenu()
  const [categoryId, setCategoryId] = useState(ALL_CATEGORIES)
  const { items } = useMemo(() => splitMenu(menu.data ?? []), [menu.data])
  const categories = useMemo(() => categoriesOf(items), [items])
  const visible = useMemo(() => itemsInCategory(items, categoryId), [items, categoryId])

  if (menu.isPending) return <MenuSkeleton />
  if (menu.isError) return <EmptyState title="Không tải được thực đơn" description={menu.error.message}
    action={<Button variant="secondary" onClick={() => menu.refetch()}>Tải lại</Button>} />
  if (items.length === 0) return <EmptyState title="Thực đơn chưa có món nào"
    description="Nhờ quản lý thêm món và bật “Hiện trên POS” trong phần thiết lập thực đơn." />

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <CategoryTabs categories={categories} value={categoryId} onChange={setCategoryId} />
      <div className="grid flex-1 auto-rows-min grid-cols-[repeat(auto-fill,minmax(9.5rem,1fr))] gap-3 overflow-y-auto p-6 pt-3">
        {visible.map((item) => (
          <MenuTile key={item.productId} item={item} onAdd={onAdd} onOptions={onOptions} disabled={disabled} />
        ))}
      </div>
    </div>
  )
}

function MenuSkeleton() {
  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(9.5rem,1fr))] gap-3 p-6" aria-busy>
      {Array.from({ length: 12 }, (_, i) => <div key={i} className="h-28 animate-pulse rounded-(--radius-panel) bg-surface" />)}
    </div>
  )
}
