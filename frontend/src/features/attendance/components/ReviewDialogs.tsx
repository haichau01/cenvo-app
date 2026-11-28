import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { AttendanceRow } from '@/lib/api/types'
import { formatDateTime } from '@/lib/format'
import { useApproveOvertime, useCorrect, useEdits, useReject } from '../api/attendance'
import { formatMinutes, fromVnLocalInput, toVnLocalInput } from '../lib/attendance'

interface RowDialogProps { row: AttendanceRow; onClose: () => void }

function Actions({ onClose, busy, submit }: { onClose: () => void; busy: boolean; submit: string }) {
  return (
    <div className="flex justify-end gap-3">
      <Button type="button" variant="secondary" onClick={onClose}>Hủy</Button>
      <Button type="submit" busy={busy}>{submit}</Button>
    </div>
  )
}

/** Forgotten check-out or a wrong scan: new times plus a reason, kept in the history the person can ask about. */
export function CorrectDialog({ row, onClose }: RowDialogProps) {
  const correct = useCorrect()
  const [checkIn, setCheckIn] = useState(() => toVnLocalInput(row.checkInAt))
  const [checkOut, setCheckOut] = useState(() => toVnLocalInput(row.checkOutAt ?? row.scheduledEnd ?? row.checkInAt))
  const [reason, setReason] = useState('')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    correct.mutate({ id: row.id, input: { checkInAt: fromVnLocalInput(checkIn), checkOutAt: fromVnLocalInput(checkOut), reason } },
      { onSuccess: () => { toast.success(`Đã sửa giờ của ${row.employeeName}`); onClose() } })
  }

  return (
    <Dialog open onClose={onClose} title={`Sửa giờ: ${row.employeeName}`}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <TextField label="Giờ vào" type="datetime-local" required value={checkIn} onChange={(e) => setCheckIn(e.target.value)} />
        <TextField label="Giờ ra" type="datetime-local" required value={checkOut} onChange={(e) => setCheckOut(e.target.value)} />
        <TextField label="Lý do" required maxLength={300} placeholder="Ví dụ: Quên chấm ra, về lúc 16:10" value={reason}
          onChange={(e) => setReason(e.target.value)} />
        <FormError message={correct.error?.message} />
        <Actions onClose={onClose} busy={correct.isPending} submit="Lưu giờ" />
      </form>
    </Dialog>
  )
}

/** Overtime is paid only for the minutes approved here, at most the time after the shift end. */
export function OvertimeDialog({ row, onClose }: RowDialogProps) {
  const approve = useApproveOvertime()
  const [minutes, setMinutes] = useState(row.overtimeApprovedMinutes ?? row.proposedOvertimeMinutes)

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    approve.mutate({ id: row.id, minutes }, { onSuccess: () => { toast.success(`Đã duyệt ${formatMinutes(minutes)} tăng ca`); onClose() } })
  }

  return (
    <Dialog open onClose={onClose} title={`Tăng ca: ${row.employeeName}`}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <p>Làm thêm sau giờ ca: <strong className="tabular-nums">{formatMinutes(row.overtimeLimitMinutes)}</strong>.</p>
        <TextField label="Số phút được trả tăng ca" type="number" min={0} max={row.overtimeLimitMinutes} required value={minutes}
          onChange={(e) => setMinutes(Number(e.target.value))} />
        <p className="text-caption text-ink-muted">Nhập 0 nếu không trả tăng ca cho lượt này.</p>
        <FormError message={approve.error?.message} />
        <Actions onClose={onClose} busy={approve.isPending} submit="Duyệt tăng ca" />
      </form>
    </Dialog>
  )
}

export function RejectDialog({ row, onClose }: RowDialogProps) {
  const reject = useReject()
  const [reason, setReason] = useState('')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    reject.mutate({ id: row.id, reason }, { onSuccess: () => { toast.success('Đã ghi không duyệt'); onClose() } })
  }

  return (
    <Dialog open onClose={onClose} title={`Không duyệt: ${row.employeeName}`}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <p className="text-ink-muted">Lượt này sẽ không được tính công. Nhân viên thấy lý do trong "Chấm công của tôi".</p>
        <TextField label="Lý do" required maxLength={300} value={reason} onChange={(e) => setReason(e.target.value)} />
        <FormError message={reject.error?.message} />
        <Actions onClose={onClose} busy={reject.isPending} submit="Không duyệt" />
      </form>
    </Dialog>
  )
}

export function EditsDialog({ row, onClose }: RowDialogProps) {
  const edits = useEdits(row.id)
  const time = (iso: string | null | undefined) => (iso ? formatDateTime(iso) : '—')
  return (
    <Dialog open onClose={onClose} title={`Lịch sử sửa: ${row.employeeName}`}>
      <FormError message={edits.error?.message} />
      <ol className="flex flex-col gap-3">
        {edits.data?.map((edit) => (
          <li key={edit.at} className="border-l-2 border-line pl-3">
            <p className="font-semibold">{edit.editorName} · {formatDateTime(edit.at)}</p>
            <p className="tabular-nums">{edit.oldCheckIn ? `${time(edit.oldCheckIn)} – ${time(edit.oldCheckOut)} → ` : 'Nhập mới: '}
              {time(edit.newCheckIn)} – {time(edit.newCheckOut)}</p>
            <p className="text-ink-muted">{edit.reason}</p>
          </li>
        ))}
      </ol>
    </Dialog>
  )
}
