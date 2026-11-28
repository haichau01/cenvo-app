import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { SelectField } from '@/components/ui/SelectField'
import type { WashTicket } from '@/lib/api/types'
import { formatVnd } from '@/lib/format'
import { useCustomer } from '../api/customers'
import { useOfferings } from '../api/setup'
import { useAddService, useRemoveService } from '../api/tickets'
import { membershipFor, priceFor } from '../lib/intake'

/** Services on the ticket; while editable, staff can add one (using the customer's membership when it covers it) or remove one. */
export function TicketServices({ ticket }: { ticket: WashTicket }) {
  const remove = useRemoveService()
  return (
    <section aria-label="Dịch vụ" className="flex flex-col gap-2">
      <ul className="divide-y divide-line rounded-(--radius-panel) border border-line">
        {ticket.lines.map((line) => (
          <li key={line.id} className="flex min-h-12 items-center gap-3 px-3 py-2">
            <span className="flex-1 font-medium">{line.name}</span>
            {line.membershipId && <span className="text-sm text-go">Trừ 1 lượt gói</span>}
            <Money value={line.amount} className="tabular-nums" />
            {ticket.isEditable && (
              <Button variant="ghost" aria-label={`Bỏ ${line.name}`} busy={remove.isPending && remove.variables?.lineId === line.id}
                onClick={() => remove.mutate({ id: ticket.id, lineId: line.id })}>
                Bỏ
              </Button>
            )}
          </li>
        ))}
      </ul>
      {ticket.isEditable && <AddService ticket={ticket} />}
    </section>
  )
}

function AddService({ ticket }: { ticket: WashTicket }) {
  const offerings = useOfferings().data ?? []
  const memberships = useCustomer(ticket.customerId ?? null).data?.memberships ?? []
  const add = useAddService()
  const [offeringId, setOfferingId] = useState('')
  const available = offerings.filter((offering) => !ticket.lines.some((line) => line.offeringId === offering.id))
  const membership = offeringId ? membershipFor(memberships, offeringId, ticket.vehicleType) : null

  function handleAdd() {
    add.mutate({ id: ticket.id, offeringId, membershipId: membership?.id ?? null }, { onSuccess: () => setOfferingId('') })
  }

  if (available.length === 0) return null
  return (
    <div className="flex items-end gap-2">
      <SelectField label="Thêm dịch vụ" className="flex-1" value={offeringId} onChange={setOfferingId} placeholder="Chọn dịch vụ"
        options={available.map((offering) => ({ value: offering.id, label: optionLabel(offering.name, priceFor(offering, ticket.vehicleType)) }))} />
      <Button variant="secondary" onClick={handleAdd} disabled={!offeringId} busy={add.isPending}>
        {membership ? 'Thêm, trừ gói' : 'Thêm'}
      </Button>
    </div>
  )
}

function optionLabel(name: string, price: number | null): string {
  if (price === null) return `${name} — chưa có giá cho loại xe này`
  return `${name} — ${formatVnd(price)}`
}
