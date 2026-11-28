import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { Department, ShiftTemplate, ShiftTemplateInput } from '@/lib/api/types'
import { useDeleteTemplate, useSaveScheduleSettings, useSaveTemplate, useScheduleSettings, useTemplates } from '../api/schedule'
import { DEPARTMENT_SHORT, shiftHours, timeLabel } from '../lib/week'

const DEPARTMENTS: Department[] = ['Cafe', 'CarWash', 'General']

/** Managers: the shifts the branch runs (with minimum staff) and the weekly hours limit. */
export function TemplatesDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const templates = useTemplates()
  const [editing, setEditing] = useState<ShiftTemplate | 'new' | null>(null)
  return (
    <Dialog open={open} onClose={onClose} title="Ca làm của chi nhánh" className="w-[min(46rem,calc(100vw-2rem))]">
      <div className="flex flex-col gap-5">
        <WeeklyLimit />
        <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
          {(templates.data ?? []).map((t) => (
            <li key={t.id} className="flex items-center gap-4 px-4 py-2">
              <span className="w-28 font-semibold">{t.name}</span>
              <span className="w-48 tabular-nums">{shiftHours(t.startTime, t.endTime)}</span>
              <span className="flex-1 text-ink-muted">
                {t.needs.length ? t.needs.map((n) => `${n.minStaff} ${DEPARTMENT_SHORT[n.department]}`).join(' · ') : 'Không đặt tối thiểu'}
                {!t.isActive && ' · Ngừng dùng'}
              </span>
              <Button variant="ghost" onClick={() => setEditing(t)}>Sửa</Button>
            </li>
          ))}
          {templates.data?.length === 0 && <li className="px-4 py-3 text-ink-muted">Chưa có ca nào. Thêm ca đầu tiên, ví dụ Sáng 06:00–12:00.</li>}
        </ul>
        {editing === null
          ? <Button variant="secondary" className="self-start" onClick={() => setEditing('new')}>Thêm ca</Button>
          : <TemplateForm key={editing === 'new' ? 'new' : editing.id} template={editing === 'new' ? null : editing}
              nextOrder={(templates.data?.length ?? 0) + 1} onDone={() => setEditing(null)} />}
      </div>
    </Dialog>
  )
}

function WeeklyLimit() {
  const settings = useScheduleSettings()
  const save = useSaveScheduleSettings()
  const [hours, setHours] = useState<number | null>(null)
  const value = hours ?? settings.data?.maxWeeklyHours ?? 48
  return (
    <div className="flex items-end gap-3">
      <TextField label="Cảnh báo khi một người làm quá (giờ/tuần)" type="number" min={1} max={168} value={value}
        onChange={(e) => setHours(e.target.valueAsNumber)} className="w-80" />
      {hours !== null && hours !== settings.data?.maxWeeklyHours && (
        <Button variant="secondary" busy={save.isPending}
          onClick={() => save.mutate({ maxWeeklyHours: value }, { onSuccess: () => { toast.success('Đã lưu giới hạn giờ'); setHours(null) } })}>
          Lưu
        </Button>
      )}
      <FormError message={save.error?.message} />
    </div>
  )
}

function TemplateForm({ template, nextOrder, onDone }: { template: ShiftTemplate | null; nextOrder: number; onDone: () => void }) {
  const save = useSaveTemplate()
  const remove = useDeleteTemplate()
  const [form, setForm] = useState<ShiftTemplateInput>(() => ({
    name: template?.name ?? '',
    startTime: timeLabel(template?.startTime ?? '06:00'),
    endTime: timeLabel(template?.endTime ?? '12:00'),
    sortOrder: template?.sortOrder ?? nextOrder,
    isActive: template?.isActive ?? true,
    needs: DEPARTMENTS.map((department) => ({
      department, minStaff: template?.needs.find((n) => n.department === department)?.minStaff ?? 0,
    })),
  }))
  const setNeed = (department: Department, minStaff: number) =>
    setForm({ ...form, needs: form.needs.map((n) => (n.department === department ? { ...n, minStaff } : n)) })

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    save.mutate({ id: template?.id ?? null, input: form }, { onSuccess: () => { toast.success(`Đã lưu ca ${form.name}`); onDone() } })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 rounded-(--radius-panel) bg-concrete p-4">
      <div className="grid grid-cols-3 gap-3">
        <TextField label="Tên ca" required maxLength={50} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <TextField label="Bắt đầu" type="time" required value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} />
        <TextField label="Kết thúc (trước giờ bắt đầu = qua đêm)" type="time" required value={form.endTime}
          onChange={(e) => setForm({ ...form, endTime: e.target.value })} />
      </div>
      <fieldset className="grid grid-cols-3 gap-3">
        <legend className="mb-1.5 text-sm font-medium">Số người tối thiểu</legend>
        {form.needs.map((n) => (
          <TextField key={n.department} label={DEPARTMENT_SHORT[n.department]} type="number" min={0} max={50} value={n.minStaff}
            onChange={(e) => setNeed(n.department, e.target.valueAsNumber || 0)} />
        ))}
      </fieldset>
      <label className="flex h-11 items-center gap-2 self-start">
        <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="size-5 accent-ink" />
        Đang dùng (bỏ chọn để ẩn khỏi lưới, lịch cũ giữ nguyên)
      </label>
      <FormError message={save.error?.message ?? remove.error?.message} />
      <div className="flex gap-3">
        <Button type="submit" busy={save.isPending}>Lưu ca</Button>
        <Button type="button" variant="secondary" onClick={onDone}>Để sau</Button>
        {template && (
          <Button type="button" variant="ghost" className="ml-auto text-stop" busy={remove.isPending}
            onClick={() => remove.mutate(template.id, { onSuccess: () => { toast.success(`Đã xóa ca ${template.name}`); onDone() } })}>
            Xóa ca
          </Button>
        )}
      </div>
    </form>
  )
}
