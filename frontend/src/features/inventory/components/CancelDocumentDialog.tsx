import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { controlClass } from '@/components/ui/fieldStyles'
import { toast } from '@/components/ui/toastStore'
import type { StockDocumentDto } from '@/lib/api/types'
import { useCancelDocument } from '../api/documents'

/** Posted documents are never edited or deleted: cancelling writes reversing entries, with a reason. */
export function CancelDocumentDialog({ doc, onClose }: { doc: StockDocumentDto | null; onClose: () => void }) {
  return (
    <Dialog open={doc !== null} onClose={onClose} title={doc ? `Hủy phiếu ${doc.code}` : ''}>
      {doc && <CancelForm key={doc.id} doc={doc} onDone={onClose} />}
    </Dialog>
  )
}

function CancelForm({ doc, onDone }: { doc: StockDocumentDto; onDone: () => void }) {
  const cancel = useCancelDocument()
  const [reason, setReason] = useState('')

  function handleConfirm() {
    cancel.mutate({ id: doc.id, reason: reason.trim() }, {
      onSuccess: () => {
        toast.success(`Đã hủy phiếu ${doc.code}`)
        onDone()
      },
    })
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="text-ink-muted">Hệ thống sẽ ghi bút toán đảo để trả tồn kho về như trước khi ghi sổ phiếu này.</p>
      <label className="flex flex-col gap-1.5 font-medium">Lý do hủy
        <input value={reason} onChange={(event) => setReason(event.target.value)} maxLength={300} autoFocus className={controlClass} />
      </label>
      <FormError message={cancel.error?.message} />
      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onDone}>Giữ phiếu</Button>
        <Button variant="danger" onClick={handleConfirm} busy={cancel.isPending} disabled={!reason.trim()}>Hủy phiếu</Button>
      </div>
    </div>
  )
}
