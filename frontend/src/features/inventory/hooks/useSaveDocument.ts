import { useState } from 'react'
import { useNavigate } from 'react-router'
import { toast } from '@/components/ui/toastStore'
import type { StockWarning } from '@/lib/api/types'
import { formatQuantity } from '@/lib/format'
import { useCreateDocument, usePostDocument, useUpdateDocument } from '../api/documents'
import { toInput, validateDraft, type DraftDocument } from '../lib/documentForm'
import { DOCUMENT_TYPES } from '../lib/documentTypes'

/**
 * "Lưu nháp" and "Ghi sổ": validate on the client first (clear Vietnamese message, no round trip),
 * create or update the draft, optionally post it, then show the saved document.
 */
export function useSaveDocument(documentId: string | null, today: string, onSaved: () => void) {
  const navigate = useNavigate()
  const create = useCreateDocument()
  const update = useUpdateDocument()
  const post = usePostDocument()
  // After the first attempt the form re-validates live, so the message disappears as soon as it is fixed.
  const [attempted, setAttempted] = useState(false)

  async function save(draft: DraftDocument, thenPost: boolean) {
    setAttempted(true)
    if (validateDraft(draft)) return

    const input = toInput(draft, today)
    const id = documentId ?? (await create.mutateAsync(input))
    if (documentId) await update.mutateAsync({ id, input })
    onSaved()

    const title = DOCUMENT_TYPES[draft.type].title.toLowerCase()
    if (thenPost) {
      const warnings = await post.mutateAsync(id)
      toast.success(`Đã ghi sổ ${title}`)
      if (warnings.length > 0) toast.error(negativeStockMessage(warnings, draft))
    } else {
      toast.success(`Đã lưu nháp ${title}`)
    }
    if (!documentId) navigate(`/kho/phieu/${id}`, { replace: true })
  }

  return {
    // Server errors are already shown as toasts by the mutation cache; swallow them here so nothing is unhandled.
    save: (draft: DraftDocument, thenPost: boolean) => save(draft, thenPost).catch(() => {}),
    problemOf: (draft: DraftDocument) => (attempted ? validateDraft(draft) : null),
    busy: create.isPending || update.isPending || post.isPending,
  }
}

function negativeStockMessage(warnings: StockWarning[], draft: DraftDocument): string {
  const names = new Map(draft.lines.map((line) => [line.productId, line.productName]))
  const items = warnings.map((w) => `${names.get(w.productId) ?? 'Sản phẩm'} (${formatQuantity(w.balanceAfter)})`)
  return `Ghi sổ xong nhưng tồn kho bị âm: ${items.join(', ')}. Kiểm tra lại số lượng hoặc nhập bổ sung.`
}
