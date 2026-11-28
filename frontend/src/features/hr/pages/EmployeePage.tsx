import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { EmployeeDetail } from '@/lib/api/types'
import { formatDate, vnDate } from '@/lib/format'
import { useEmployee, useLeave, useRehire, useSaveEmployee } from '../api/employees'
import { ContractSection } from '../components/ContractSection'
import { EmployeeForm } from '../components/EmployeeForm'
import { SalarySection } from '../components/SalarySection'

/** /nhan-su/moi hires; /nhan-su/:id edits the account + profile, contracts, pay and employment status. */
export function EmployeePage() {
  const { id } = useParams()
  const isNew = id === undefined
  const employee = useEmployee(isNew ? null : id)

  if (isNew) return <NewEmployee />
  if (employee.isPending) return <div className="h-96 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (employee.isError) return <FormError message={employee.error.message} />
  return <ExistingEmployee key={employee.data.account.id} detail={employee.data} />
}

function BackLink() {
  return <Link to="/nhan-su" className="mb-2 inline-block text-ink-muted hover:underline">Danh sách nhân viên</Link>
}

function NewEmployee() {
  const save = useSaveEmployee()
  const navigate = useNavigate()
  return (
    <div className="flex max-w-5xl flex-col">
      <BackLink />
      <PageHeader title="Thêm nhân viên" description="Tạo tài khoản đăng nhập và hồ sơ cùng lúc. Hợp đồng và lương nhập sau khi lưu." />
      <EmployeeForm employee={null} busy={save.isPending} error={save.error?.message}
        onSubmit={(input) => save.mutate({ id: null, input }, {
          onSuccess: (newId) => { toast.success(`Đã thêm ${input.fullName}`); navigate(`/nhan-su/${newId}`, { replace: true }) },
        })} />
    </div>
  )
}

function ExistingEmployee({ detail }: { detail: EmployeeDetail }) {
  const save = useSaveEmployee()
  const rehire = useRehire()
  const [leaving, setLeaving] = useState(false)
  const { account, profile } = detail
  const hasLeft = profile?.leaveDate != null

  const actions = profile && (hasLeft
    ? <Button variant="secondary" busy={rehire.isPending}
        onClick={() => rehire.mutate(account.id, { onSuccess: () => toast.success(`${account.fullName} đã đi làm lại`) })}>Đi làm lại</Button>
    : <Button variant="secondary" onClick={() => setLeaving(true)}>Cho nghỉ việc</Button>)

  return (
    <div className="flex max-w-5xl flex-col gap-6">
      <div>
        <BackLink />
        <PageHeader title={account.fullName} actions={actions}
          description={hasLeft ? `Đã nghỉ từ ${formatDate(profile!.leaveDate!)} — tài khoản bị khóa.` : profile?.position ?? 'Chưa có hồ sơ'} />
        {!profile && <StatusPill label="Chưa có hồ sơ — điền chức vụ, ngày vào làm rồi lưu" tone="lane" />}
        <FormError message={rehire.error?.message} />
      </div>
      <EmployeeForm employee={detail} busy={save.isPending} error={save.error?.message}
        onSubmit={(input) => save.mutate({ id: account.id, input }, { onSuccess: () => toast.success('Đã lưu hồ sơ') })} />
      {profile && <ContractSection employeeId={account.id} contracts={detail.contracts} />}
      {profile && <SalarySection employeeId={account.id} detail={detail} />}
      {leaving && <LeaveDialog employeeId={account.id} name={account.fullName} onClose={() => setLeaving(false)} />}
    </div>
  )
}

function LeaveDialog({ employeeId, name, onClose }: { employeeId: string; name: string; onClose: () => void }) {
  const leave = useLeave()
  const [date, setDate] = useState(() => vnDate(Date.now()))
  return (
    <Dialog open onClose={onClose} title={`Cho ${name} nghỉ việc`}
      footer={<>
        <Button variant="secondary" onClick={onClose}>Để sau</Button>
        <Button variant="danger" busy={leave.isPending}
          onClick={() => leave.mutate({ id: employeeId, leaveDate: date }, { onSuccess: () => { toast.success(`${name} đã nghỉ việc`); onClose() } })}>
          Cho nghỉ việc
        </Button>
      </>}>
      <div className="flex flex-col gap-4">
        <TextField label="Ngày làm cuối" type="date" required value={date} onChange={(e) => setDate(e.target.value)} />
        <p className="text-ink-muted">Tài khoản bị khóa ngay, không đăng nhập được nữa. Hồ sơ, hợp đồng và lịch sử lương được giữ lại.</p>
        <FormError message={leave.error?.message} />
      </div>
    </Dialog>
  )
}
