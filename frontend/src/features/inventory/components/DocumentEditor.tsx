import { useEffect, useMemo } from 'react'
import { useBlocker } from 'react-router'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { useDeleteDocumentFlow } from '../hooks/useDeleteDocumentFlow'
import { useDraftDocument } from '../hooks/useDraftDocument'
import { useSaveDocument } from '../hooks/useSaveDocument'
import { useStockProducts } from '../api/references'
import type { DraftDocument } from '../lib/documentForm'
import { DOCUMENT_TYPES } from '../lib/documentTypes'
import { DocumentHeaderFields } from './DocumentHeaderFields'
import { LinesEditor } from './LinesEditor'
import { ProductPicker } from './ProductPicker'

interface DocumentEditorProps {
  /** null = new document, not saved yet. */
  documentId: string | null
  initial: DraftDocument
  today: string
}

/** Draft document form: header, lines, then "Lưu nháp" / "Ghi sổ". */
export function DocumentEditor({ documentId, initial, today }: DocumentEditorProps) {
  const config = DOCUMENT_TYPES[initial.type]
  const editor = useDraftDocument(initial)
  const saver = useSaveDocument(documentId, today, editor.markSaved)
  const remover = useDeleteDocumentFlow(documentId)
  const products = useStockProducts()
  const productById = useMemo(() => new Map((products.data ?? []).map((p) => [p.id, p])), [products.data])
  useLeaveGuard(editor.hasUnsavedChanges)

  return (
    <div className="flex flex-col gap-6">
      <p className="text-ink-muted">{config.hint}</p>
      <section className="rounded-(--radius-panel) bg-surface p-5">
        <DocumentHeaderFields draft={editor.draft} config={config} today={today} onChange={editor.setHeader} />
      </section>
      <section className="flex flex-col gap-4 rounded-(--radius-panel) bg-surface p-5" aria-label="Dòng hàng">
        <ProductPicker products={products.data ?? []} onPick={editor.addProduct} />
        {editor.draft.lines.length > 0
          ? <LinesEditor lines={editor.draft.lines} config={config} products={productById} onChange={editor.changeLine} onRemove={editor.removeLine} />
          : <p className="py-6 text-center text-ink-muted">Chưa có dòng hàng. Gõ tên sản phẩm ở ô phía trên để thêm.</p>}
      </section>
      <FormError message={saver.problemOf(editor.draft)} />
      <div className="flex flex-wrap justify-end gap-3">
        {documentId && <Button variant="ghost" className="mr-auto text-stop" onClick={remover.remove} busy={remover.busy}>Xóa nháp</Button>}
        <Button variant="secondary" size="lg" onClick={() => saver.save(editor.draft, false)} busy={saver.busy}>Lưu nháp</Button>
        <Button size="lg" onClick={() => saver.save(editor.draft, true)} busy={saver.busy}>Ghi sổ</Button>
      </div>
    </div>
  )
}

/** Leaving the page with unsaved changes asks first (closing the browser tab is not covered). */
function useLeaveGuard(hasUnsavedChanges: () => boolean) {
  const blocker = useBlocker(({ currentLocation, nextLocation }) =>
    hasUnsavedChanges() && currentLocation.pathname !== nextLocation.pathname)
  useEffect(() => {
    if (blocker.state !== 'blocked') return
    if (window.confirm('Phiếu có thay đổi chưa lưu. Rời trang và bỏ thay đổi?')) blocker.proceed()
    else blocker.reset()
  }, [blocker])
}
