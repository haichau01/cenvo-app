import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { MoneyField } from '@/components/ui/MoneyField'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { EmployeeDetail } from '@/lib/api/types'
import { vnDate } from '@/lib/format'
import { useAddSalaryRate, useRemoveSalaryRate } from '../api/employees'
import { firstOfMonth, nextMonth, monthLabel } from '../lib/labels'

/**
 * Monthly salary history: each change is a new row from the 1st of a month with a reason; started rows are kept as they
 * are, only a future row typed by mistake can be removed.
 */
export function SalarySection({ employeeId, detail }: { employeeId: string; detail: EmployeeDetail }) {
  const [adding, setAdding] = useState(false)
  const remove = useRemoveSalaryRate()
  return (
    <section className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-5">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="font-display text-block font-semibold">Lương tháng</h2>
          <p className="text-ink-muted">
            {detail.currentRate
              ? <>Đang áp dụng: <Money value={detail.currentRate.monthlySalary} /> + phụ cấp <Money value={detail.currentRate.allowance} /></>
              : 'Chưa có mức lương đang áp dụng.'}
          </p>
        </div>
        <Button variant="secondary" onClick={() => setAdding(true)}>Đổi mức lương</Button>
      </div>
      {detail.salaryRates.length > 0 && (
        <table className="w-full text-table">
          <thead className="border-b border-line text-caption text-ink-muted">
            <tr>
              <th className="py-2 text-left font-medium">Áp dụng từ</th>
              <th className="py-2 text-right font-medium">Lương</th>
              <th className="py-2 text-right font-medium">Phụ cấp</th>
              <th className="py-2 text-right font-medium">Tăng ca / giờ</th>
              <th className="py-2 pl-6 text-left font-medium">Lý do</th>
              <th><span className="sr-only">Thao tác</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {detail.salaryRates.map((rate) => (
              <tr key={rate.id} className="h-11">
                <td className="tabular-nums">{monthLabel(rate.effectiveFrom)}</td>
                <td className="text-right"><Money value={rate.monthlySalary} /></td>
                <td className="text-right"><Money value={rate.allowance} /></td>
                <td className="text-right"><Money value={rate.overtimeHourlyRate} /></td>
                <td className="pl-6">{rate.reason}</td>
                <td className="text-right">
                  {rate.canRemove && (
                    <Button variant="ghost" busy={remove.isPending}
                      onClick={() => remove.mutate({ id: employeeId, rateId: rate.id }, { onSuccess: () => toast.success('Đã xóa mức lương chưa áp dụng') })}>
                      Xóa
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <FormError message={remove.error?.message} />
      {adding && <AddRateDialog employeeId={employeeId} current={detail.currentRate} onClose={() => setAdding(false)} />}
    </section>
  )
}

function AddRateDialog({ employeeId, current, onClose }: {
  employeeId: string; current: EmployeeDetail['currentRate']; onClose: () => void
}) {
  const add = useAddSalaryRate()
  const [month, setMonth] = useState(() => (current ? nextMonth(vnDate(Date.now())) : vnDate(Date.now()).slice(0, 7)))
  const [salary, setSalary] = useState(current?.monthlySalary ?? 0)
  const [allowance, setAllowance] = useState(current?.allowance ?? 0)
  const [overtime, setOvertime] = useState(current?.overtimeHourlyRate ?? 0)
  const [reason, setReason] = useState(current ? '' : 'Mức khởi điểm')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    add.mutate({ id: employeeId, input: { effectiveFrom: firstOfMonth(month), monthlySalary: salary, allowance, overtimeHourlyRate: overtime, reason } },
      { onSuccess: () => { toast.success('Đã lưu mức lương mới'); onClose() } })
  }

  return (
    <Dialog open onClose={onClose} title="Đổi mức lương">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <TextField label="Áp dụng từ tháng" type="month" required value={month} onChange={(e) => setMonth(e.target.value)} />
        <MoneyField label="Lương tháng" value={salary} onChange={setSalary} />
        <MoneyField label="Phụ cấp cố định mỗi tháng" value={allowance} onChange={setAllowance} />
        <MoneyField label="Tiền tăng ca mỗi giờ (đã duyệt)" value={overtime} onChange={setOvertime} />
        <TextField label="Lý do" required maxLength={200} value={reason} onChange={(e) => setReason(e.target.value)} />
        <p className="text-caption text-ink-muted">Mức cũ giữ nguyên trong lịch sử; kỳ lương nào dùng mức của kỳ đó.</p>
        <FormError message={add.error?.message} />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>Hủy</Button>
          <Button type="submit" busy={add.isPending}>Lưu mức lương</Button>
        </div>
      </form>
    </Dialog>
  )
}
