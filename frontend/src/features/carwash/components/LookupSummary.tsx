import { Link } from 'react-router'
import { Money } from '@/components/cenvo/Money'
import type { VehicleLookup } from '@/lib/api/types'
import { formatDate } from '@/lib/format'

/** What the shop already knows about this car: an unfinished ticket (warning), owner, packages, last visits. */
export function LookupSummary({ lookup }: { lookup: VehicleLookup | null | undefined }) {
  if (lookup === undefined) return null
  if (lookup === null) return <p className="text-ink-muted">Xe mới — chưa có lịch sử rửa.</p>

  return (
    <div className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-4">
      {lookup.openTicket && (
        <p role="alert" className="rounded-(--radius-control) bg-lane px-3 py-2 font-medium text-ink">
          Xe đang có phiếu {lookup.openTicket.code} chưa giao.{' '}
          <Link to={`/rua-xe?phieu=${lookup.openTicket.id}`} className="font-semibold underline">Mở phiếu đó</Link>
        </p>
      )}
      {lookup.customer && <p className="font-semibold">{lookup.customer.name} · {lookup.customer.phone}</p>}
      {lookup.memberships.map((membership) => (
        <p key={membership.id} className="text-go">
          Gói “{membership.packageName}”: còn {membership.remainingUses}/{membership.totalUses} lượt, hạn {formatDate(membership.expiresAt)}
        </p>
      ))}
      {lookup.recentTickets.length > 0 && (
        <ul className="flex flex-col gap-1 text-sm text-ink-muted">
          {lookup.recentTickets.slice(0, 3).map((visit) => (
            <li key={visit.id} className="flex justify-between gap-3">
              <span>{formatDate(visit.createdAt)} · {visit.services.join(' + ')}</span>
              <Money value={visit.total} />
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
