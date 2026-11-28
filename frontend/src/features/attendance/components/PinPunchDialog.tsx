import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import type { PunchResult, StaffMember } from '@/lib/api/types'
import { usePunchByPin } from '../api/attendance'
import { isValidPin } from '../lib/attendance'
import { punchMessage } from '../lib/punchMessage'
import { PinPad } from './PinPad'

/** Fallback on the shop tablet: the person picks their name and types their own PIN. */
export function PinPunchDialog({ staff, onClose }: { staff: StaffMember; onClose: () => void }) {
  const [pin, setPin] = useState('')
  const punch = usePunchByPin()

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    punch.mutate({ employeeId: staff.userId, pin }, {
      onSuccess: (result: PunchResult) => { toast.success(punchMessage(result)); onClose() },
      onError: () => setPin(''),
    })
  }

  return (
    <Dialog open onClose={onClose} title={`Chấm công: ${staff.fullName}`}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <p className="text-ink-muted">Nhập mã PIN của bạn. Lượt chấm bằng PIN được đánh dấu để quản lý xem lại.</p>
        <PinPad value={pin} onChange={setPin} />
        <FormError message={punch.error?.message} />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>Hủy</Button>
          <Button type="submit" size="lg" disabled={!isValidPin(pin)} busy={punch.isPending}>Chấm công</Button>
        </div>
      </form>
    </Dialog>
  )
}
