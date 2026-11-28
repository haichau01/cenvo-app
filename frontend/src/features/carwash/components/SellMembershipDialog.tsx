import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { PaymentMethod, WashPackage } from '@/lib/api/types'
import { previewChange } from '@/lib/payment'
import { useSellMembership } from '../api/customers'
import { usePackages } from '../api/setup'
import { vehicleLabel } from '../lib/vehicle'
import { CashInput, MethodPicker } from './PaymentInputs'

/** Who the package is sold to: a known customer, or a new one typed at the counter. */
export interface MembershipBuyer {
  phone: string
  name: string
}

export function SellMembershipDialog({ buyer, onClose }: { buyer: MembershipBuyer | null; onClose: () => void }) {
  return (
    <Dialog open={buyer !== null} onClose={onClose} title="Bán gói thành viên">
      {buyer && <SellForm buyer={buyer} onSold={onClose} />}
    </Dialog>
  )
}

/** Paid now, in the cashier's open shift; then used one wash at a time at any branch. */
function SellForm({ buyer, onSold }: { buyer: MembershipBuyer; onSold: () => void }) {
  const packages = usePackages().data ?? []
  const sell = useSellMembership()
  const [phone, setPhone] = useState(buyer.phone)
  const [name, setName] = useState(buyer.name)
  const [packageId, setPackageId] = useState('')
  const [method, setMethod] = useState<PaymentMethod>('Cash')
  const [given, setGiven] = useState(0)
  const chosen = packages.find((item) => item.id === packageId)
  const price = chosen?.price ?? 0
  const short = method === 'Cash' && previewChange(price, given) < 0

  function handleConfirm() {
    const amount = method === 'Cash' ? given : price
    sell.mutate({ packageId, customerPhone: phone, customerName: name.trim() || null, payments: [{ method, amount }] }, {
      onSuccess: (membership) => {
        toast.success(`Đã bán gói ${membership.code} · ${membership.totalUses} lượt`)
        onSold()
      },
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="grid grid-cols-2 gap-3">
        <TextField label="Số điện thoại khách" inputMode="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
        <TextField label="Tên khách" value={name} onChange={(event) => setName(event.target.value)} />
      </div>
      <SelectField label="Gói" value={packageId} placeholder="Chọn gói" options={packages.map(packageOption)}
        onChange={(id) => { setPackageId(id); setGiven(packages.find((item) => item.id === id)?.price ?? 0) }} />
      {chosen && (
        <>
          <p className="text-center"><Money value={price} className="amount text-amount" /></p>
          <MethodPicker method={method} onMethod={setMethod} />
          {method === 'Cash' && <CashInput total={price} given={given} onGiven={setGiven} change={previewChange(price, given)} />}
        </>
      )}
      <FormError message={sell.error?.message} />
      <Button size="lg" onClick={handleConfirm} busy={sell.isPending} disabled={!chosen || !phone.trim() || short}>
        Thu tiền và bán gói
      </Button>
    </div>
  )
}

function packageOption(item: WashPackage) {
  const vehicle = item.vehicleType ? ` · ${vehicleLabel(item.vehicleType)}` : ''
  return { value: item.id, label: `${item.name} — ${item.uses} lượt ${item.offeringName}${vehicle}, ${item.validDays} ngày` }
}
