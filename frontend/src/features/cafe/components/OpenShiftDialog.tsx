import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { Dialog } from '@/components/ui/Dialog'
import { MoneyField } from '@/components/ui/MoneyField'
import { toast } from '@/components/ui/toastStore'
import { useOpenShift } from '../api/shifts'

interface OpenShiftDialogProps {
  open: boolean
  onClose: () => void
}

export function OpenShiftDialog({ open, onClose }: OpenShiftDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title="Mở ca">
      {open && <OpenShiftForm onDone={onClose} />}
    </Dialog>
  )
}

function OpenShiftForm({ onDone }: { onDone: () => void }) {
  const openShift = useOpenShift()
  const [openingCash, setOpeningCash] = useState(0)

  function handleConfirm() {
    openShift.mutate(openingCash, {
      onSuccess: (shift) => {
        toast.success(`Đã mở ca ${shift.code}`)
        onDone()
      },
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <p className="text-ink-muted">Đếm tiền lẻ đang có trong két rồi nhập vào. Cuối ca sẽ đối soát theo số này.</p>
      <MoneyField label="Tiền đầu ca" value={openingCash} onChange={setOpeningCash} autoFocus />
      <FormError message={openShift.error?.message} />
      <Button size="lg" onClick={handleConfirm} busy={openShift.isPending}>Mở ca</Button>
    </div>
  )
}
