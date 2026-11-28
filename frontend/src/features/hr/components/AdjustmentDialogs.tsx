import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { MoneyField } from '@/components/ui/MoneyField'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { AdjustmentKind, PayAdjustment } from '@/lib/api/types'
import { vnDate } from '@/lib/format'
import { useAdjustmentStaff, useCancelAdjustment, useRecordAdjustment } from '../api/adjustments'
import { ADJUSTMENT_KINDS, adjustmentLabel } from '../lib/labels'

export function RecordAdjustmentDialog({ onClose }: { onClose: () => void }) {
  const staff = useAdjustmentStaff()
  const record = useRecordAdjustment()
  const [today] = useState(() => vnDate(Date.now()))
  const [employeeId, setEmployeeId] = useState('')
  const [kind, setKind] = useState<AdjustmentKind>('Advance')
  const [amount, setAmount] = useState(0)
  const [date, setDate] = useState(today)
  const [reason, setReason] = useState('')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    record.mutate({ employeeId, kind, amount, date, reason }, {
      onSuccess: () => { toast.success(`Đã ghi ${adjustmentLabel(kind).toLowerCase()}`); onClose() },
    })
  }

  return (
    <Dialog open onClose={onClose} title="Ghi thưởng, phạt, tạm ứng">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <SelectField label="Nhân viên" required value={employeeId} onChange={setEmployeeId} placeholder="Chọn nhân viên"
          options={(staff.data ?? []).map((s) => ({ value: s.userId, label: s.fullName }))} />
        <fieldset className="flex flex-col gap-1.5">
          <legend className="mb-1.5 text-sm font-medium">Loại</legend>
          <div className="grid grid-cols-3 gap-2">
            {ADJUSTMENT_KINDS.map((option) => (
              <label key={option.value} className={`flex h-12 cursor-pointer items-center justify-center rounded-(--radius-control) border
                has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-foam
                font-semibold ${kind === option.value ? 'border-ink bg-ink text-surface' : 'border-line bg-surface'}`}>
                <input type="radio" name="kind" value={option.value} checked={kind === option.value}
                  onChange={() => setKind(option.value)} className="sr-only" />
                {option.label}
              </label>
            ))}
          </div>
        </fieldset>
        <MoneyField label="Số tiền" value={amount} onChange={setAmount} />
        <TextField label="Ngày" type="date" required max={today} value={date} onChange={(e) => setDate(e.target.value)} />
        <TextField label="Lý do" required maxLength={200} value={reason} onChange={(e) => setReason(e.target.value)}
          placeholder={kind === 'Penalty' ? 'Ví dụ: đi trễ 30 phút ngày 25/11' : undefined} />
        <FormError message={record.error?.message ?? staff.error?.message} />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>Để sau</Button>
          <Button type="submit" busy={record.isPending} disabled={!employeeId || amount <= 0}>Ghi {adjustmentLabel(kind).toLowerCase()}</Button>
        </div>
      </form>
    </Dialog>
  )
}

export function CancelAdjustmentDialog({ adjustment, onClose }: { adjustment: PayAdjustment; onClose: () => void }) {
  const cancel = useCancelAdjustment()
  const [reason, setReason] = useState('')
  return (
    <Dialog open onClose={onClose} title={`Hủy ${adjustmentLabel(adjustment.kind).toLowerCase()} của ${adjustment.employeeName}`}
      footer={<>
        <Button variant="secondary" onClick={onClose}>Để sau</Button>
        <Button variant="danger" busy={cancel.isPending} disabled={!reason.trim()}
          onClick={() => cancel.mutate({ id: adjustment.id, reason }, { onSuccess: () => { toast.success('Đã hủy khoản'); onClose() } })}>
          Hủy khoản này
        </Button>
      </>}>
      <div className="flex flex-col gap-4">
        <p><Money value={adjustment.amount} /> · {adjustment.reason}</p>
        <TextField label="Lý do hủy" required maxLength={200} value={reason} onChange={(e) => setReason(e.target.value)} />
        <FormError message={cancel.error?.message} />
      </div>
    </Dialog>
  )
}
