import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Money } from '@/components/cenvo/Money'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { formatDate } from '@/lib/format'
import { foldText } from '@/lib/search'
import { useEmployees } from '../api/employees'
import { contractLabel, departmentLabel, employeeStatus, roleLabel } from '../lib/labels'

const linkClass = `inline-flex h-12 items-center rounded-(--radius-control) bg-(--module) px-4 font-semibold text-(--primary-foreground)
  hover:brightness-110`

/** Owner only: every account of the chain with its HR profile, contract and current pay. */
export function EmployeesPage() {
  const employees = useEmployees()
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [showLeft, setShowLeft] = useState(false)
  const needle = foldText(query)
  const rows = (employees.data ?? [])
    .filter((e) => showLeft || !e.leaveDate)
    .filter((e) => needle === '' || foldText(`${e.fullName} ${e.userName} ${e.position ?? ''}`).includes(needle))

  return (
    <>
      <PageHeader title="Nhân viên" description="Mỗi nhân viên là một tài khoản đăng nhập. CCCD và lương chỉ chủ quán xem được."
        actions={<Link to="/nhan-su/moi" className={linkClass}>Thêm nhân viên</Link>} />
      <div className="mb-4 flex items-end gap-4">
        <TextField label="Tìm theo tên, tài khoản, chức vụ" type="search" value={query} onChange={(event) => setQuery(event.target.value)}
          className="w-80" />
        <label className="flex h-12 items-center gap-2">
          <input type="checkbox" checked={showLeft} onChange={(event) => setShowLeft(event.target.checked)} className="size-5 accent-ink" />
          Hiện người đã nghỉ
        </label>
      </div>
      <FormError message={employees.error?.message} />
      {employees.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {employees.data && rows.length === 0 && <EmptyState title="Không có nhân viên khớp bộ lọc" />}
      {rows.length > 0 && (
        <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
          <thead className="border-b border-line text-caption text-ink-muted">
            <tr>
              <th className="px-3 py-2 text-left font-medium">Nhân viên</th>
              <th className="px-3 py-2 text-left font-medium">Vai trò</th>
              <th className="px-3 py-2 text-left font-medium">Bộ phận · chức vụ</th>
              <th className="px-3 py-2 text-left font-medium">Vào làm</th>
              <th className="px-3 py-2 text-left font-medium">Hợp đồng</th>
              <th className="px-3 py-2 text-right font-medium">Lương tháng</th>
              <th className="px-3 py-2 text-left font-medium">Trạng thái</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {rows.map((e) => (
              <tr key={e.id} onClick={() => navigate(`/nhan-su/${e.id}`)} className="h-14 cursor-pointer hover:bg-concrete/60">
                <td className="px-3">
                  <Link to={`/nhan-su/${e.id}`} onClick={(event) => event.stopPropagation()} className="font-medium hover:underline">
                    {e.fullName}
                  </Link>
                  <p className="text-caption text-ink-muted">{e.userName}</p>
                </td>
                <td className="px-3">{e.roles.map(roleLabel).join(', ')}</td>
                <td className="px-3">{e.hasProfile ? `${departmentLabel(e.department)} · ${e.position}` : '—'}</td>
                <td className="px-3 tabular-nums">{e.hireDate ? formatDate(e.hireDate) : '—'}</td>
                <td className="px-3">
                  {e.contractType ? contractLabel(e.contractType) : '—'}
                  {e.contractEnd && <p className="text-caption tabular-nums text-ink-muted">đến {formatDate(e.contractEnd)}</p>}
                </td>
                <td className="px-3 text-right">{e.monthlySalary != null ? <Money value={e.monthlySalary} /> : '—'}</td>
                <td className="px-3"><StatusPill {...employeeStatus(e)} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
