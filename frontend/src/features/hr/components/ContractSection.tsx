import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { ContractInput, ContractType, EmployeeDetail } from '@/lib/api/types'
import { formatDate, vnDate } from '@/lib/format'
import { useSaveContract } from '../api/employees'
import { CONTRACT_TYPES, contractLabel } from '../lib/labels'

type Contract = EmployeeDetail['contracts'][number]

/** Labour contracts, newest first; the latest started one is the current one. */
export function ContractSection({ employeeId, contracts }: { employeeId: string; contracts: Contract[] }) {
  const [editing, setEditing] = useState<Contract | 'new' | null>(null)
  return (
    <section className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-5">
      <div className="flex items-center justify-between">
        <h2 className="font-display text-block font-semibold">Hợp đồng</h2>
        <Button variant="secondary" onClick={() => setEditing('new')}>Thêm hợp đồng</Button>
      </div>
      {contracts.length === 0 && <p className="text-ink-muted">Chưa có hợp đồng.</p>}
      <ul className="divide-y divide-line">
        {contracts.map((c) => (
          <li key={c.id} className="flex items-center gap-4 py-2">
            <span className="w-40 font-medium">{contractLabel(c.type)}</span>
            <span className="tabular-nums">{formatDate(c.startDate)} – {c.endDate ? formatDate(c.endDate) : 'không thời hạn'}</span>
            <span className="flex-1 text-ink-muted">{c.note}</span>
            <Button variant="ghost" onClick={() => setEditing(c)}>Sửa</Button>
          </li>
        ))}
      </ul>
      <ContractDialog key={editing === 'new' ? 'new' : editing?.id ?? 'closed'} employeeId={employeeId}
        contract={editing === 'new' ? null : editing} open={editing !== null} onClose={() => setEditing(null)} />
    </section>
  )
}

function ContractDialog({ employeeId, contract, open, onClose }: {
  employeeId: string; contract: Contract | null; open: boolean; onClose: () => void
}) {
  const save = useSaveContract()
  const [form, setForm] = useState<ContractInput>(() => ({
    type: contract?.type ?? 'FullTime',
    startDate: contract?.startDate ?? vnDate(Date.now()),
    endDate: contract?.endDate ?? null,
    note: contract?.note ?? '',
  }))

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    save.mutate({ id: employeeId, contractId: contract?.id ?? null, input: { ...form, endDate: form.endDate || null, note: form.note || null } },
      { onSuccess: () => { toast.success('Đã lưu hợp đồng'); onClose() } })
  }

  return (
    <Dialog open={open} onClose={onClose} title={contract ? 'Sửa hợp đồng' : 'Thêm hợp đồng'}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <SelectField label="Loại hợp đồng" value={form.type} options={CONTRACT_TYPES}
          onChange={(v) => setForm({ ...form, type: v as ContractType })} />
        <div className="grid grid-cols-2 gap-3">
          <TextField label="Ngày bắt đầu" type="date" required value={form.startDate}
            onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          <TextField label="Ngày hết hạn (bỏ trống nếu không thời hạn)" type="date" value={form.endDate ?? ''} min={form.startDate}
            onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
        </div>
        <TextField label="Ghi chú" maxLength={300} value={form.note ?? ''} onChange={(e) => setForm({ ...form, note: e.target.value })} />
        <FormError message={save.error?.message} />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>Hủy</Button>
          <Button type="submit" busy={save.isPending}>Lưu hợp đồng</Button>
        </div>
      </form>
    </Dialog>
  )
}
