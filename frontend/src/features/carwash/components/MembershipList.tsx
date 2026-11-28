import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { StatusPill, type PillTone } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { WashMembership } from '@/lib/api/types'
import { formatDate } from '@/lib/format'
import { useCancelMembership } from '../api/customers'
import { usePrintMembership } from '../api/printing'

function membershipState(membership: WashMembership): { label: string; tone: PillTone } {
  if (membership.status === 'Cancelled') return { label: 'Đã hủy', tone: 'stop' }
  if (membership.isUsable) return { label: 'Đang dùng', tone: 'go' }
  return { label: membership.remainingUses === 0 ? 'Hết lượt' : 'Hết hạn', tone: 'muted' }
}

interface MembershipListProps {
  memberships: WashMembership[]
  canPrint: boolean
  canCancel: boolean
}

/**
 * A customer's packages. The counter can reprint a sale receipt (only at the selling branch — the server returns 404 elsewhere);
 * a manager can cancel (refund) one that has not been used yet — the server checks the rest.
 */
export function MembershipList({ memberships, canPrint, canCancel }: MembershipListProps) {
  const printMembership = usePrintMembership()
  const [cancelling, setCancelling] = useState<WashMembership | null>(null)
  if (memberships.length === 0) return <p className="text-ink-muted">Khách chưa mua gói nào.</p>
  return (
    <>
      <ul className="flex flex-col gap-2">
        {memberships.map((membership) => {
          const state = membershipState(membership)
          const unused = membership.remainingUses === membership.totalUses && membership.status === 'Active'
          return (
            <li key={membership.id} className="flex flex-wrap items-center gap-3 rounded-(--radius-control) border border-line px-3 py-2">
              <div className="flex-1">
                <p className="font-semibold">{membership.packageName}</p>
                <p className="text-sm text-ink-muted">
                  {membership.code} · mua {formatDate(membership.paidAt)} · hạn {formatDate(membership.expiresAt)} · <Money value={membership.price} />
                </p>
              </div>
              <span className="font-display text-xl font-bold tabular-nums">{membership.remainingUses}/{membership.totalUses}</span>
              <StatusPill label={state.label} tone={state.tone} />
              {canPrint && <Button variant="ghost" onClick={() => printMembership.mutate(membership.id)}>In lại</Button>}
              {canCancel && unused && <Button variant="ghost" className="text-stop" onClick={() => setCancelling(membership)}>Hủy gói</Button>}
            </li>
          )
        })}
      </ul>
      <CancelMembershipDialog membership={cancelling} onClose={() => setCancelling(null)} />
    </>
  )
}

function CancelMembershipDialog({ membership, onClose }: { membership: WashMembership | null; onClose: () => void }) {
  const cancel = useCancelMembership()
  const [reason, setReason] = useState('')
  function handleConfirm() {
    if (!membership) return
    cancel.mutate({ id: membership.id, reason: reason.trim() }, {
      onSuccess: () => { toast.success(`Đã hủy gói ${membership.code}`); setReason(''); onClose() },
    })
  }
  return (
    <Dialog open={membership !== null} onClose={onClose} title={membership ? `Hủy gói ${membership.code}` : ''}
      footer={<Button variant="danger" onClick={handleConfirm} busy={cancel.isPending} disabled={!reason.trim()}>Hủy gói, hoàn tiền</Button>}>
      <div className="flex flex-col gap-3">
        <p>Tiền gói được trừ khỏi ca đã bán. Chỉ hủy được gói chưa dùng lượt nào.</p>
        <TextField label="Lý do hủy" value={reason} onChange={(event) => setReason(event.target.value)} maxLength={200} />
        <FormError message={cancel.error?.message} />
      </div>
    </Dialog>
  )
}
