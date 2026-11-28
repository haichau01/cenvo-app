import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { UnpunchedShift } from '@/lib/api/types'
import { useAddManual } from '../api/attendance'
import { fromVnLocalInput, toVnLocalInput } from '../lib/attendance'

/** The person worked the shift but has no punch at all (phone dead, tablet down): the manager enters it with a reason. */
export function ManualDialog({ shift, date, onClose }: { shift: UnpunchedShift; date: string; onClose: () => void }) {
  const add = useAddManual()
  const [checkIn, setCheckIn] = useState(() => toVnLocalInput(shift.startsAt))
  const [checkOut, setCheckOut] = useState(() => toVnLocalInput(shift.endsAt))
  const [reason, setReason] = useState('')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    add.mutate({
      employeeId: shift.employeeId, workDate: date, shiftAssignmentId: shift.assignmentId,
      checkInAt: fromVnLocalInput(checkIn), checkOutAt: fromVnLocalInput(checkOut), reason,
    }, { onSuccess: () => { toast.success(`Đã nhập giờ cho ${shift.employeeName}`); onClose() } })
  }

  return (
    <Dialog open onClose={onClose} title={`Nhập giờ: ${shift.employeeName} · ca ${shift.shiftName}`}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <TextField label="Giờ vào" type="datetime-local" required value={checkIn} onChange={(e) => setCheckIn(e.target.value)} />
        <TextField label="Giờ ra" type="datetime-local" required value={checkOut} onChange={(e) => setCheckOut(e.target.value)} />
        <TextField label="Lý do" required maxLength={300} placeholder="Ví dụ: Điện thoại hết pin, có mặt cả ca" value={reason}
          onChange={(e) => setReason(e.target.value)} />
        <FormError message={add.error?.message} />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>Hủy</Button>
          <Button type="submit" busy={add.isPending}>Lưu giờ</Button>
        </div>
      </form>
    </Dialog>
  )
}
