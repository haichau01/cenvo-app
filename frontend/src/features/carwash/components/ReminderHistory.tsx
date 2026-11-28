import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { controlClass } from '@/components/ui/fieldStyles'
import { FormError } from '@/components/ui/FormError'
import { formatShortDateTime, vnDate } from '@/lib/format'
import { useReminderHistory } from '../api/reminders'
import { reminderKindLabel, reminderStatusLabel } from '../lib/reminders'

/** Send log of the branch: who got which message and whether the provider accepted it. */
export function ReminderHistory() {
  const [today] = useState(() => vnDate(Date.now()))
  const [from, setFrom] = useState(today)
  const [to, setTo] = useState(today)
  const history = useReminderHistory(from, to)

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-end gap-3">
        <label className="flex flex-col gap-1.5 text-sm font-medium">Từ ngày
          <input type="date" value={from} max={to} onChange={(event) => setFrom(event.target.value)} className={controlClass} />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">Đến ngày
          <input type="date" value={to} min={from} onChange={(event) => setTo(event.target.value)} className={controlClass} />
        </label>
      </div>
      <FormError message={history.error?.message} />
      {history.data?.length === 0 && <EmptyState title="Chưa gửi tin nhắc nào trong khoảng này" />}
      {history.data && history.data.length > 0 && (
        <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
          <thead className="border-b border-line text-caption text-ink-muted">
            <tr>
              <th className="px-3 py-2 text-left font-medium">Lúc gửi</th>
              <th className="px-3 py-2 text-left font-medium">Khách</th>
              <th className="px-3 py-2 text-left font-medium">Loại</th>
              <th className="px-3 py-2 text-left font-medium">Nội dung</th>
              <th className="px-3 py-2 text-left font-medium">Trạng thái</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {history.data.map((log) => (
              <tr key={log.id} className="align-top">
                <td className="whitespace-nowrap px-3 py-3 tabular-nums">{formatShortDateTime(log.attemptedAt)}</td>
                <td className="px-3 py-3">
                  <p className="font-medium">{log.customerName}</p>
                  <p className="tabular-nums text-ink-muted">{log.phone}</p>
                </td>
                <td className="whitespace-nowrap px-3 py-3">{reminderKindLabel(log.kind)}</td>
                <td className="max-w-md px-3 py-3 text-ink-muted">{log.message}</td>
                <td className="px-3 py-3">
                  <div className="flex flex-col items-start gap-1">
                    <StatusPill {...reminderStatusLabel(log.status)} />
                    {log.lastError && <span className="text-caption text-stop">{log.lastError}</span>}
                    {log.attempts > 1 && <span className="text-caption text-ink-muted tabular-nums">Gửi {log.attempts} lần</span>}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
