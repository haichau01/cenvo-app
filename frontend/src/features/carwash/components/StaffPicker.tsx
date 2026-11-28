import { Money } from '@/components/cenvo/Money'
import type { WashTicket } from '@/lib/api/types'
import { useWashers } from '../api/setup'
import { useAssignStaff } from '../api/tickets'

/** Toggle chips: who washes this car (commission is split equally between them when the wash is done). */
export function StaffPicker({ ticket }: { ticket: WashTicket }) {
  const washers = useWashers()
  const assign = useAssignStaff()
  const chosen = new Set(ticket.staff.map((member) => member.userId))

  function handleToggle(userId: string) {
    const next = new Set(chosen)
    if (next.has(userId)) next.delete(userId)
    else next.add(userId)
    assign.mutate({ id: ticket.id, staffIds: [...next] })
  }

  if (!ticket.isEditable) {
    return (
      <ul className="flex flex-wrap gap-2">
        {ticket.staff.map((member) => (
          <li key={member.userId} className="rounded-full bg-concrete px-3 py-1.5 font-medium">
            {member.fullName}{member.commission > 0 && <span className="text-ink-muted"> · hoa hồng <Money value={member.commission} /></span>}
          </li>
        ))}
      </ul>
    )
  }
  if (washers.data?.length === 0) {
    return <p className="text-ink-muted">Chi nhánh chưa có nhân viên rửa xe. Quản lý cần tạo tài khoản vai trò “Nhân viên rửa xe”.</p>
  }
  return (
    <div role="group" aria-label="Người rửa" className="flex flex-wrap gap-2">
      {washers.data?.map((washer) => (
        <button key={washer.userId} type="button" aria-pressed={chosen.has(washer.userId)} disabled={assign.isPending}
          onClick={() => handleToggle(washer.userId)}
          className={`h-12 rounded-full border-2 px-4 font-semibold
            ${chosen.has(washer.userId) ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
          {washer.fullName}
        </button>
      ))}
    </div>
  )
}
