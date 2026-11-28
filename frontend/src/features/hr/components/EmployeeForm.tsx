import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { SelectField } from '@/components/ui/SelectField'
import { TextField } from '@/components/ui/TextField'
import type { Department, EmployeeDetail, EmployeeInput } from '@/lib/api/types'
import { vnDate } from '@/lib/format'
import { useBranches } from '../api/employees'
import { DEPARTMENTS, ROLES } from '../lib/labels'

interface EmployeeFormProps {
  /** Null = hiring a new employee. */
  employee: EmployeeDetail | null
  busy: boolean
  error?: string
  onSubmit: (input: EmployeeInput) => void
}

function initial(employee: EmployeeDetail | null): EmployeeInput {
  const profile = employee?.profile
  return {
    userName: employee?.account.userName ?? '',
    fullName: employee?.account.fullName ?? '',
    password: '',
    roles: employee?.account.roles ?? [],
    branchIds: employee?.account.branchIds ?? [],
    department: profile?.department ?? 'Cafe',
    position: profile?.position ?? '',
    hireDate: profile?.hireDate ?? vnDate(Date.now()),
    nationalId: profile?.nationalId ?? '',
    phone: profile?.phone ?? '',
    birthDate: profile?.birthDate ?? null,
    address: profile?.address ?? '',
    note: profile?.note ?? '',
  }
}

const toggle = (list: string[], value: string) => (list.includes(value) ? list.filter((v) => v !== value) : [...list, value])

/** Login account and HR profile in one form — saved together. */
export function EmployeeForm({ employee, busy, error, onSubmit }: EmployeeFormProps) {
  const branches = useBranches()
  const [form, setForm] = useState<EmployeeInput>(() => initial(employee))
  const set = <K extends keyof EmployeeInput>(key: K, value: EmployeeInput[K]) => setForm((current) => ({ ...current, [key]: value }))
  const isNew = employee === null

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({ ...form, password: form.password || null, birthDate: form.birthDate || null })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <Section title="Tài khoản đăng nhập">
        <div className="grid grid-cols-3 gap-3">
          <TextField label="Họ tên" required maxLength={100} value={form.fullName} onChange={(e) => set('fullName', e.target.value)} />
          <TextField label="Tên đăng nhập" required autoComplete="off" maxLength={50} value={form.userName}
            onChange={(e) => set('userName', e.target.value)} />
          <TextField label={isNew ? 'Mật khẩu' : 'Mật khẩu mới (để trống nếu giữ)'} type="password" autoComplete="new-password"
            required={isNew} minLength={6} value={form.password ?? ''} onChange={(e) => set('password', e.target.value)} />
        </div>
        <CheckGroup legend="Vai trò" options={ROLES} selected={form.roles} onToggle={(v) => set('roles', toggle(form.roles, v))} />
        <CheckGroup legend="Làm ở chi nhánh" options={(branches.data ?? []).map((b) => ({ value: b.id, label: b.name }))}
          selected={form.branchIds} onToggle={(v) => set('branchIds', toggle(form.branchIds, v))} />
      </Section>
      <Section title="Hồ sơ">
        <div className="grid grid-cols-3 gap-3">
          <SelectField label="Bộ phận" value={form.department} onChange={(v) => set('department', v as Department)} options={DEPARTMENTS} />
          <TextField label="Chức vụ" required maxLength={100} value={form.position} onChange={(e) => set('position', e.target.value)} />
          <TextField label="Ngày vào làm" type="date" required value={form.hireDate} onChange={(e) => set('hireDate', e.target.value)} />
          <TextField label="Số CCCD" inputMode="numeric" maxLength={15} value={form.nationalId ?? ''}
            onChange={(e) => set('nationalId', e.target.value)} />
          <TextField label="Số điện thoại" inputMode="tel" maxLength={15} value={form.phone ?? ''} onChange={(e) => set('phone', e.target.value)} />
          <TextField label="Ngày sinh" type="date" value={form.birthDate ?? ''} onChange={(e) => set('birthDate', e.target.value)} />
        </div>
        <TextField label="Địa chỉ" maxLength={200} value={form.address ?? ''} onChange={(e) => set('address', e.target.value)} />
        <TextField label="Ghi chú" maxLength={300} value={form.note ?? ''} onChange={(e) => set('note', e.target.value)} />
      </Section>
      <FormError message={error} />
      <Button type="submit" className="self-start" busy={busy}>{isNew ? 'Thêm nhân viên' : 'Lưu thay đổi'}</Button>
    </form>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-5">
      <h2 className="font-display text-block font-semibold">{title}</h2>
      {children}
    </section>
  )
}

function CheckGroup({ legend, options, selected, onToggle }: {
  legend: string; options: { value: string; label: string }[]; selected: string[]; onToggle: (value: string) => void
}) {
  return (
    <fieldset className="flex flex-col gap-1.5">
      <legend className="mb-1.5 text-sm font-medium">{legend}</legend>
      <div className="flex flex-wrap gap-x-5">
        {options.map((option) => (
          <label key={option.value} className="flex h-11 items-center gap-2">
            <input type="checkbox" checked={selected.includes(option.value)} onChange={() => onToggle(option.value)}
              className="size-5 accent-ink" />
            {option.label}
          </label>
        ))}
      </div>
    </fieldset>
  )
}
