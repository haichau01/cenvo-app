import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Plate } from '@/components/cenvo/Plate'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { WashCustomerDetail } from '@/lib/api/types'
import { formatDate } from '@/lib/format'
import { useCustomer, useUpdateCustomer } from '../api/customers'
import { washStatusLabel } from '../lib/status'
import { vehicleLabel } from '@/lib/vehicle'
import { MembershipList } from './MembershipList'

interface CustomerDetailProps {
  customerId: string
  canSell: boolean
  /** Cashier/manager only — the phone number decides whose packages are used. */
  canEdit: boolean
  canCancelMembership: boolean
  onSell: (customer: WashCustomerDetail) => void
}

export function CustomerDetail({ customerId, canSell, canEdit, canCancelMembership, onSell }: CustomerDetailProps) {
  const customer = useCustomer(customerId)
  if (customer.isPending) return <div className="h-80 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (customer.isError) return <FormError message={customer.error.message} />
  const data = customer.data
  return (
    <div className="flex flex-col gap-6">
      <CustomerInfo key={data.id} customer={data} canEdit={canEdit} />
      <Section title="Xe">
        <ul className="flex flex-wrap gap-3">
          {data.vehicles.map((vehicle) => (
            <li key={vehicle.id} className="flex items-center gap-2">
              <Plate value={vehicle.plate} />
              <span className="text-sm text-ink-muted">{vehicleLabel(vehicle.vehicleType)}{vehicle.description && ` · ${vehicle.description}`}</span>
            </li>
          ))}
        </ul>
      </Section>
      <Section title="Gói thành viên" action={canSell && <Button variant="secondary" onClick={() => onSell(data)}>Bán gói</Button>}>
        <MembershipList memberships={data.memberships} canPrint={canSell} canCancel={canCancelMembership} />
      </Section>
      <Section title="Lần rửa gần đây tại chi nhánh">
        {data.recentTickets.length === 0 && <p className="text-ink-muted">Chưa rửa ở chi nhánh này.</p>}
        <ul className="divide-y divide-line">
          {data.recentTickets.map((visit) => (
            <li key={visit.id} className="flex items-center gap-3 py-2 text-table">
              <span className="w-24 tabular-nums">{formatDate(visit.createdAt)}</span>
              <Plate value={visit.plate} />
              <span className="flex-1">{visit.services.join(' + ')}</span>
              <StatusPill {...washStatusLabel(visit.status)} />
              <Money value={visit.total} className="w-24 text-right" />
            </li>
          ))}
        </ul>
      </Section>
    </div>
  )
}

function CustomerInfo({ customer, canEdit }: { customer: WashCustomerDetail; canEdit: boolean }) {
  const update = useUpdateCustomer()
  const [name, setName] = useState(customer.name)
  const [phone, setPhone] = useState(customer.phone)
  const [note, setNote] = useState(customer.note ?? '')
  const changed = name !== customer.name || phone !== customer.phone || note !== (customer.note ?? '')

  function handleSave() {
    update.mutate({ id: customer.id, name, phone, note: note.trim() || null }, { onSuccess: () => toast.success('Đã lưu thông tin khách') })
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-3">
        <TextField label="Tên khách" value={name} onChange={(event) => setName(event.target.value)} maxLength={100} />
        <TextField label="Số điện thoại" inputMode="tel" value={phone} onChange={(event) => setPhone(event.target.value)} maxLength={15} />
      </div>
      <TextField label="Ghi chú" value={note} onChange={(event) => setNote(event.target.value)} maxLength={300} />
      <FormError message={update.error?.message} />
      {canEdit && changed && <Button className="self-start" onClick={handleSave} busy={update.isPending}>Lưu thông tin</Button>}
    </div>
  )
}

function Section({ title, action, children }: { title: string; action?: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <h2 className="font-display text-block font-semibold">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}
