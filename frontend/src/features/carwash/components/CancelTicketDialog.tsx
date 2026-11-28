import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import { controlClass } from '@/components/ui/fieldStyles'
import type { WashTicket } from '@/lib/api/types'
import { useCancelTicket } from '../api/tickets'

const QUICK_REASONS = ['Khách đổi ý', 'Nhập nhầm xe', 'Khách chờ lâu, bỏ về']

interface CancelTicketDialogProps {
  ticket: WashTicket | null
  onClose: () => void
  onCancelled: () => void
}

/**
 * Tickets are never deleted: cancelling needs a reason, gives membership uses back and, for a washed car,
 * returns the chemicals to stock. Washed or paid tickets need a manager (the server checks).
 */
export function CancelTicketDialog({ ticket, onClose, onCancelled }: CancelTicketDialogProps) {
  return (
    <Dialog open={ticket !== null} onClose={onClose} title={ticket ? `Hủy phiếu ${ticket.code}` : ''}>
      {ticket && <CancelForm key={ticket.id} ticket={ticket} onBack={onClose} onCancelled={onCancelled} />}
    </Dialog>
  )
}

function CancelForm({ ticket, onBack, onCancelled }: { ticket: WashTicket; onBack: () => void; onCancelled: () => void }) {
  const cancel = useCancelTicket()
  const [reason, setReason] = useState('')

  function handleConfirm() {
    cancel.mutate({ id: ticket.id, reason: reason.trim() }, {
      onSuccess: (cancelled) => {
        toast.success(`Đã hủy ${cancelled.code}`)
        onCancelled()
      },
    })
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap gap-2">
        {QUICK_REASONS.map((text) => (
          <button key={text} type="button" onClick={() => setReason(text)} aria-pressed={reason === text}
            className={`h-12 rounded-full border px-4 font-medium ${reason === text ? 'border-ink bg-ink text-white' : 'border-line'}`}>
            {text}
          </button>
        ))}
      </div>
      <label className="flex flex-col gap-1.5">
        <span className="font-medium">Lý do hủy</span>
        <input value={reason} onChange={(event) => setReason(event.target.value)} maxLength={200} className={controlClass} />
      </label>
      <FormError message={cancel.error?.message} />
      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onBack}>Giữ phiếu</Button>
        <Button variant="danger" onClick={handleConfirm} busy={cancel.isPending} disabled={!reason.trim()}>Hủy phiếu</Button>
      </div>
    </div>
  )
}
