import { useId, useState } from 'react'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { ReminderSettings, ReminderSettingsInput } from '@/lib/api/types'
import { useSaveReminderSettings } from '../api/reminders'

/** What each placeholder becomes, shown under the templates. */
const PLACEHOLDER_HELP: Record<string, string> = {
  ten: 'tên khách',
  bienso: 'biển số lần rửa cuối',
  chinhanh: 'tên chi nhánh',
  sdtchinhanh: 'số điện thoại chi nhánh',
  songay: 'số ngày từ lần rửa cuối',
  goi: 'tên gói',
  conlai: 'số lượt còn lại',
  hethan: 'ngày hết hạn gói',
}

interface ReminderSettingsDialogProps {
  settings: ReminderSettings | undefined
  open: boolean
  onClose: () => void
}

export function ReminderSettingsDialog({ settings, open, onClose }: ReminderSettingsDialogProps) {
  return (
    <Dialog open={open && settings !== undefined} onClose={onClose} title="Cài đặt nhắc khách" className="w-[min(44rem,calc(100vw-2rem))]">
      {settings && <SettingsForm settings={settings} onDone={onClose} />}
    </Dialog>
  )
}

function SettingsForm({ settings, onDone }: { settings: ReminderSettings; onDone: () => void }) {
  const save = useSaveReminderSettings()
  const [form, setForm] = useState<ReminderSettingsInput>({
    returnAfterDays: settings.returnAfterDays,
    expiringWithinDays: settings.expiringWithinDays,
    lowUsesThreshold: settings.lowUsesThreshold,
    returnTemplate: settings.returnTemplate,
    expiringTemplate: settings.expiringTemplate,
    lowUsesTemplate: settings.lowUsesTemplate,
  })
  const set = <K extends keyof ReminderSettingsInput>(key: K, value: ReminderSettingsInput[K]) =>
    setForm((current) => ({ ...current, [key]: value }))

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    save.mutate(form, { onSuccess: () => { toast.success('Đã lưu cài đặt nhắc khách'); onDone() } })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="grid grid-cols-3 gap-3">
        <NumberField label="Nhắc quay lại sau (ngày)" value={form.returnAfterDays} min={1} max={365}
          onChange={(value) => set('returnAfterDays', value)} />
        <NumberField label="Gói hết hạn trong (ngày)" value={form.expiringWithinDays} min={1} max={365}
          onChange={(value) => set('expiringWithinDays', value)} />
        <NumberField label="Gói còn tối đa (lượt)" value={form.lowUsesThreshold} min={0} max={10}
          onChange={(value) => set('lowUsesThreshold', value)} />
      </div>
      <TemplateField label="Tin nhắc quay lại" value={form.returnTemplate} max={settings.maxTemplateLength}
        onChange={(value) => set('returnTemplate', value)} />
      <TemplateField label="Tin gói sắp hết hạn" value={form.expiringTemplate} max={settings.maxTemplateLength}
        onChange={(value) => set('expiringTemplate', value)} />
      <TemplateField label="Tin gói sắp hết lượt" value={form.lowUsesTemplate} max={settings.maxTemplateLength}
        onChange={(value) => set('lowUsesTemplate', value)} />
      <div className="rounded-(--radius-control) bg-concrete px-4 py-3 text-caption text-ink-muted">
        <p className="mb-1 font-medium text-ink">Chỗ điền tự động</p>
        <ul className="grid grid-cols-2 gap-x-6 gap-y-0.5">
          {settings.placeholders.map((key) => (
            <li key={key}><span className="font-semibold text-ink">{`{${key}}`}</span> {PLACEHOLDER_HELP[key] ?? ''}</li>
          ))}
        </ul>
      </div>
      <FormError message={save.error?.message} />
      <div className="flex justify-end gap-3">
        <Button type="button" variant="secondary" onClick={onDone}>Hủy</Button>
        <Button type="submit" busy={save.isPending}>Lưu cài đặt</Button>
      </div>
    </form>
  )
}

function NumberField({ label, value, min, max, onChange }: {
  label: string; value: number; min: number; max: number; onChange: (value: number) => void
}) {
  return (
    <TextField label={label} type="number" inputMode="numeric" min={min} max={max} required value={Number.isNaN(value) ? '' : value}
      onChange={(event) => onChange(event.target.valueAsNumber)} className="tabular-nums" />
  )
}

function TemplateField({ label, value, max, onChange }: { label: string; value: string; max: number; onChange: (value: string) => void }) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between">
        <label htmlFor={id} className="text-sm font-medium text-ink">{label}</label>
        <span className="text-caption tabular-nums text-ink-muted">{value.length}/{max}</span>
      </div>
      <textarea id={id} value={value} maxLength={max} rows={3} required onChange={(event) => onChange(event.target.value)}
        className="resize-y rounded-(--radius-control) border border-line bg-surface px-3 py-2 text-base text-ink outline-none
          focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam" />
    </div>
  )
}
