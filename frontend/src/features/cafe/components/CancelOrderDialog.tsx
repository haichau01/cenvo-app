import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { Dialog } from '@/components/ui/Dialog'
import { toast } from '@/components/ui/toastStore'
import type { OrderDto } from '@/lib/api/types'
import { useCancelOrder } from '../api/orders'

const QUICK_REASONS = ['Khách đổi ý', 'Nhập nhầm', 'Khách chờ lâu, bỏ về']

interface CancelOrderDialogProps {
  order: OrderDto | null
  onClose: () => void
  onCancelled: () => void
}

/** Orders are never deleted: cancelling needs a reason and reverses stock already issued. */
export function CancelOrderDialog({ order, onClose, onCancelled }: CancelOrderDialogProps) {
  return (
    <Dialog open={order !== null} onClose={onClose} title={order ? `Hủy hóa đơn ${order.code}` : ''}>
      {order && <CancelForm key={order.id} order={order} onBack={onClose} onCancelled={onCancelled} />}
    </Dialog>
  )
}

function CancelForm({ order, onBack, onCancelled }: { order: OrderDto; onBack: () => void; onCancelled: () => void }) {
  const cancel = useCancelOrder()
  const [reason, setReason] = useState('')

  function handleConfirm() {
    cancel.mutate({ orderId: order.id, reason: reason.trim() }, {
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
        <input value={reason} onChange={(event) => setReason(event.target.value)} maxLength={200}
          className="h-12 rounded-(--radius-control) border border-line bg-surface px-3 outline-none
            focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam" />
      </label>
      <FormError message={cancel.error?.message} />
      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onBack}>Giữ hóa đơn</Button>
        <Button variant="danger" onClick={handleConfirm} busy={cancel.isPending} disabled={!reason.trim()}>
          Hủy hóa đơn
        </Button>
      </div>
    </div>
  )
}
