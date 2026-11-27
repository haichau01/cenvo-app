import { useRef, useState } from 'react'
import type { ProductDto } from '@/lib/api/types'
import { lineFromProduct, type DraftDocument, type DraftLine } from '../lib/documentForm'

/** Local editing state of a draft document. Nothing is saved until the user presses "Lưu nháp" or "Ghi sổ". */
export function useDraftDocument(initial: DraftDocument) {
  const [draft, setDraft] = useState(initial)
  // A ref, not state: "saved" must be visible immediately to the navigation guard that runs right after saving.
  const dirty = useRef(false)

  function edit(change: (current: DraftDocument) => DraftDocument) {
    setDraft(change)
    dirty.current = true
  }

  return {
    draft,
    hasUnsavedChanges: () => dirty.current,
    markSaved: () => { dirty.current = false },
    setHeader: (patch: Partial<DraftDocument>) => edit((d) => ({ ...d, ...patch })),
    addProduct: (product: ProductDto) =>
      edit((d) => ({ ...d, lines: [...d.lines, lineFromProduct(product, crypto.randomUUID())] })),
    changeLine: (key: string, patch: Partial<DraftLine>) =>
      edit((d) => ({ ...d, lines: d.lines.map((line) => (line.key === key ? { ...line, ...patch } : line)) })),
    removeLine: (key: string) => edit((d) => ({ ...d, lines: d.lines.filter((line) => line.key !== key) })),
  }
}
