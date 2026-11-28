import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import type { ReminderKind, ReminderSettings } from '@/lib/api/types'
import { useDueReminders, useReminderSettings } from '../api/reminders'
import { DueReminderList } from '../components/DueReminderList'
import { ReminderHistory } from '../components/ReminderHistory'
import { ReminderSettingsDialog } from '../components/ReminderSettingsDialog'
import { RequireBranch } from '../components/RequireBranch'
import { REMINDER_KINDS } from '../lib/reminders'

const TAB_PARAM = 'muc'
const HISTORY = 'da-gui'

export function RemindersPage() {
  return <RequireBranch from="/rua-xe/nhac-khach"><RemindersScreen /></RequireBranch>
}

/** Managers: customers due for a reminder (no visit for a while, package expiring or almost used up), sent on demand. */
function RemindersScreen() {
  const [params, setParams] = useSearchParams()
  const tab = params.get(TAB_PARAM) ?? REMINDER_KINDS[0].kind
  const due = useDueReminders()
  const settings = useReminderSettings()
  const [settingsOpen, setSettingsOpen] = useState(false)
  const count = (kind: ReminderKind) => due.data?.filter((item) => item.reason.kind === kind).length
  const current = REMINDER_KINDS.find((k) => k.kind === tab)

  return (
    <div data-module="wash" className="flex flex-col">
      <PageHeader title="Nhắc khách quay lại" description={settings.data && describe(settings.data)}
        actions={<Button variant="secondary" onClick={() => setSettingsOpen(true)} disabled={!settings.data}>Cài đặt nhắc</Button>} />
      <p className="mb-4 max-w-3xl rounded-(--radius-control) bg-surface px-4 py-3 text-ink-muted">
        Chưa kết nối Zalo OA hoặc nhà mạng SMS: tin chỉ được ghi vào nhật ký, khách chưa nhận được.
      </p>
      <div role="tablist" aria-label="Loại nhắc" className="mb-4 flex gap-1 border-b border-line">
        {REMINDER_KINDS.map((item) => (
          <Tab key={item.kind} selected={tab === item.kind} onSelect={() => setParams({ [TAB_PARAM]: item.kind }, { replace: true })}>
            {item.label}{count(item.kind) !== undefined && <span className="ml-2 tabular-nums">{count(item.kind)}</span>}
          </Tab>
        ))}
        <Tab selected={tab === HISTORY} onSelect={() => setParams({ [TAB_PARAM]: HISTORY }, { replace: true })}>Đã gửi</Tab>
      </div>
      <FormError message={due.error?.message ?? settings.error?.message} />
      <div role="tabpanel" aria-label={current?.label ?? 'Đã gửi'}>
        {tab === HISTORY && <ReminderHistory />}
        {current && due.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
        {current && due.data && (
          <DueReminderList key={current.kind} kind={current.kind} emptyText={current.empty}
            items={due.data.filter((item) => item.reason.kind === current.kind)} />
        )}
      </div>
      <ReminderSettingsDialog settings={settings.data} open={settingsOpen} onClose={() => setSettingsOpen(false)} />
    </div>
  )
}

function Tab({ selected, onSelect, children }: { selected: boolean; onSelect: () => void; children: React.ReactNode }) {
  return (
    <button type="button" role="tab" aria-selected={selected} onClick={onSelect}
      className={`-mb-px h-11 border-b-2 px-4 font-semibold ${selected ? 'border-foam text-foam' : 'border-transparent text-ink-muted'}`}>
      {children}
    </button>
  )
}

function describe(s: ReminderSettings): string {
  return `Nhắc sau ${s.returnAfterDays} ngày chưa quay lại · gói hết hạn trong ${s.expiringWithinDays} ngày · gói còn từ ${s.lowUsesThreshold} lượt trở xuống.`
}
