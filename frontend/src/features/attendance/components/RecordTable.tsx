import { useState } from 'react'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import type { AttendanceRow } from '@/lib/api/types'
import { isOwner } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { formatDate, formatTime } from '@/lib/format'
import { useApprove } from '../api/attendance'
import { formatMinutes, methodLabel, needsOvertimeReview, rowStatus } from '../lib/attendance'
import { CorrectDialog, EditsDialog, OvertimeDialog, RejectDialog } from './ReviewDialogs'

type Open = { kind: 'correct' | 'overtime' | 'reject' | 'edits'; row: AttendanceRow } | null

/** Punches with what the manager can do about each; their own punches go to the owner. */
export function RecordTable({ rows, showDate }: { rows: AttendanceRow[]; showDate?: boolean }) {
  const [open, setOpen] = useState<Open>(null)
  const approve = useApprove()
  const close = () => setOpen(null)
  return (
    <>
      <FormError message={approve.error?.message} />
      <table className="w-full rounded-(--radius-panel) bg-surface text-table">
        <thead className="border-b border-line text-caption text-ink-muted">
          <tr>
            <th className="px-4 py-2 text-left font-medium">Nhân viên</th>
            {showDate && <th className="py-2 text-left font-medium">Ngày</th>}
            <th className="py-2 text-left font-medium">Ca</th>
            <th className="py-2 text-left font-medium">Vào – ra</th>
            <th className="py-2 text-right font-medium">Giờ làm</th>
            <th className="py-2 pl-6 text-left font-medium">Ghi chú</th>
            <th className="py-2 text-left font-medium">Trạng thái</th>
            <th><span className="sr-only">Thao tác</span></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {rows.map((row) => (
            <tr key={row.id} className="h-11 align-middle">
              <td className="px-4 font-medium">{row.employeeName}</td>
              {showDate && <td className="tabular-nums">{formatDate(row.workDate)}</td>}
              <td>{row.shiftName ?? <span className="text-ink-muted">Ngoài lịch</span>}</td>
              <td className="tabular-nums">{formatTime(row.checkInAt)} – {row.checkOutAt ? formatTime(row.checkOutAt) : '…'}</td>
              <td className="text-right tabular-nums">{row.checkOutAt ? formatMinutes(row.workedMinutes) : ''}</td>
              <td className="pl-6 text-caption"><Notes row={row} /></td>
              <td><StatusPill {...rowStatus(row)} /></td>
              <td className="pr-2 text-right"><RowActions row={row} onOpen={(kind) => setOpen({ kind, row })} approve={approve} /></td>
            </tr>
          ))}
        </tbody>
      </table>
      {open?.kind === 'correct' && <CorrectDialog row={open.row} onClose={close} />}
      {open?.kind === 'overtime' && <OvertimeDialog row={open.row} onClose={close} />}
      {open?.kind === 'reject' && <RejectDialog row={open.row} onClose={close} />}
      {open?.kind === 'edits' && <EditsDialog row={open.row} onClose={close} />}
    </>
  )
}

function Notes({ row }: { row: AttendanceRow }) {
  const notes = [
    row.lateMinutes > 0 && `Muộn ${formatMinutes(row.lateMinutes)}`,
    row.earlyLeaveMinutes > 0 && `Về sớm ${formatMinutes(row.earlyLeaveMinutes)}`,
    row.overtimeApprovedMinutes != null && `Tăng ca ${formatMinutes(row.overtimeApprovedMinutes)}`,
    needsOvertimeReview(row) && `Làm thêm ${formatMinutes(row.proposedOvertimeMinutes)} chờ duyệt`,
    row.checkInMethod !== 'Qr' && methodLabel(row.checkInMethod),
    row.onOwnScreen && 'Tự chấm trên máy đăng nhập tài khoản của mình — kiểm tra có ở quán',
    row.onOwnScreen && 'Tự chấm trên máy đăng nhập tài khoản của mình — kiểm tra có ở quán',
    row.reviewNote,
  ].filter(Boolean)
  return <span className="text-ink-muted">{notes.join(' · ')}</span>
}

function RowActions({ row, onOpen, approve }: {
  row: AttendanceRow; onOpen: (kind: 'correct' | 'overtime' | 'reject' | 'edits') => void; approve: ReturnType<typeof useApprove>
}) {
  const user = useSession()!.user
  if (row.employeeId === user.userId && !isOwner(user)) return <span className="text-caption text-ink-muted">Chủ quán duyệt</span>
  // After a planned shift only past the review threshold; an approved unplanned shift may be paid as overtime whole.
  const canOvertime = row.counts && (row.proposedOvertimeMinutes > 0 || row.overtimeApprovedMinutes != null || !row.shiftName)
  return (
    <div className="flex justify-end gap-1">
      {row.approval === 'Pending' && (
        <>
          <Button variant="secondary" busy={approve.isPending}
            onClick={() => approve.mutate(row.id, { onSuccess: () => toast.success(`Đã duyệt lượt của ${row.employeeName}`) })}>Duyệt</Button>
          <Button variant="ghost" onClick={() => onOpen('reject')}>Không duyệt</Button>
        </>
      )}
      {canOvertime && <Button variant={needsOvertimeReview(row) ? 'secondary' : 'ghost'} onClick={() => onOpen('overtime')}>Tăng ca</Button>}
      <Button variant={row.forgotten ? 'secondary' : 'ghost'} onClick={() => onOpen('correct')}>Sửa giờ</Button>
      {row.editCount > 0 && <Button variant="ghost" onClick={() => onOpen('edits')}>Lịch sử</Button>}
    </div>
  )
}
