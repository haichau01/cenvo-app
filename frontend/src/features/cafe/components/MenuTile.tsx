import { memo } from 'react'
import { Money } from '@/components/cenvo/Money'
import type { MenuItem } from '@/lib/api/types'

interface MenuTileProps {
  item: MenuItem
  onAdd: (item: MenuItem) => void
  onOptions: (item: MenuItem) => void
  disabled: boolean
}

/** Memoized: re-renders only when its own item changes, not when the order panel updates. */
export const MenuTile = memo(function MenuTile({ item, onAdd, onOptions, disabled }: MenuTileProps) {
  return (
    <div className="relative">
      <button type="button" disabled={disabled} onClick={() => onAdd(item)}
        className="flex h-28 w-full flex-col justify-between rounded-(--radius-panel) bg-surface p-3 pr-12 text-left
          transition-[filter,transform] duration-100 active:scale-[0.98] active:brightness-95 disabled:opacity-50">
        <span className="line-clamp-2 text-base font-semibold leading-snug text-ink">{item.name}</span>
        <Money value={item.price} className="text-ink-muted" />
      </button>
      <button type="button" disabled={disabled} onClick={() => onOptions(item)} aria-label={`Tùy chọn ${item.name}`}
        className="absolute top-0 right-0 flex size-12 items-center justify-center rounded-tr-(--radius-panel) text-xl
          text-ink-muted active:bg-ink/10 disabled:opacity-50">
        ⋯
      </button>
    </div>
  )
})
