import { useId, useMemo, useRef, useState } from 'react'
import { controlClass } from '@/components/ui/fieldStyles'
import type { ProductDto } from '@/lib/api/types'
import { searchBy } from '@/lib/search'

const MAX_RESULTS = 8

interface ProductPickerProps {
  products: ProductDto[]
  onPick: (product: ProductDto) => void
}

/** Type a code or name (diacritics optional), arrows + Enter or click to add. Stays focused for the next item. */
export function ProductPicker({ products, onPick }: ProductPickerProps) {
  const listId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [text, setText] = useState('')
  const [active, setActive] = useState(0)
  const results = useMemo(() => searchBy(products, text, (p) => [p.code, p.name], MAX_RESULTS), [products, text])

  function pick(product: ProductDto) {
    onPick(product)
    setText('')
    setActive(0)
    inputRef.current?.focus()
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown') setActive((i) => Math.min(i + 1, results.length - 1))
    else if (event.key === 'ArrowUp') setActive((i) => Math.max(i - 1, 0))
    else if (event.key === 'Enter' && results[active]) pick(results[active])
    else if (event.key === 'Escape') setText('')
    else return
    event.preventDefault()
  }

  return (
    <div className="relative w-full max-w-xl">
      <input ref={inputRef} role="combobox" aria-expanded={results.length > 0} aria-controls={listId}
        aria-activedescendant={results[active] ? `${listId}-${active}` : undefined} aria-label="Thêm sản phẩm"
        value={text} onChange={(event) => { setText(event.target.value); setActive(0) }} onKeyDown={handleKeyDown}
        placeholder="Thêm sản phẩm: gõ mã hoặc tên (không cần dấu)" className={`${controlClass} w-full`} />
      {results.length > 0 && (
        <ul id={listId} role="listbox" className="absolute z-10 mt-1 w-full overflow-hidden rounded-(--radius-control) bg-surface shadow-overlay">
          {results.map((product, index) => (
            <li key={product.id} id={`${listId}-${index}`} role="option" aria-selected={index === active}
              onMouseDown={(event) => { event.preventDefault(); pick(product) }} onMouseEnter={() => setActive(index)}
              className={`flex h-11 cursor-pointer items-center justify-between gap-3 px-3 ${index === active ? 'bg-concrete' : ''}`}>
              <span><span className="text-ink-muted">{product.code}</span> · <span className="font-medium">{product.name}</span></span>
              <span className="text-caption text-ink-muted">{product.baseUnitName}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
