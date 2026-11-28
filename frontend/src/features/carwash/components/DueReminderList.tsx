import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Plate } from '@/components/cenvo/Plate'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { FormError } from '@/components/ui/FormError'
import { toast } from '@/components/ui/toastStore'
import type { DueReminder, ReminderKind, SendRemindersResult } from '@/lib/api/types'
import { useSendReminders, useSetAcceptsReminders } from '../api/reminders'
import { reminderReason } from '../lib/reminders'

/** The API accepts at most this many reasons per send. */
const MAX_PER_SEND = 100

interface DueReminderListProps {
  kind: ReminderKind
  items: DueReminder[]
  emptyText: string
}

/** Due customers of one kind: tick who to remind, check the message, send. */
export function DueReminderList({ kind, items, emptyText }: DueReminderListProps) {
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [confirming, setConfirming] = useState(false)
  const visible = new Set(items.map((item) => item.reason.refId))
  const chosen = [...selected].filter((id) => visible.has(id)) // rows can leave the list after a refresh
  const allChosen = items.length > 0 && chosen.length === Math.min(items.length, MAX_PER_SEND)

  function toggle(id: string) {
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else if (next.size < MAX_PER_SEND) next.add(id)
      return next
    })
  }

  if (items.length === 0) return <EmptyState title={emptyText} description="Danh sách tự cập nhật theo cài đặt nhắc khách." />

  return (
    <div className="flex flex-col gap-3">
      <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
        <thead className="border-b border-line text-caption text-ink-muted">
          <tr>
            <th className="w-12 px-3 py-2">
              <input type="checkbox" aria-label="Chọn tất cả" checked={allChosen} className="size-5 accent-foam"
                onChange={() => setSelected(allChosen ? new Set() : new Set(items.slice(0, MAX_PER_SEND).map((i) => i.reason.refId)))} />
            </th>
            <th className="px-3 py-2 text-left font-medium">Khách</th>
            <th className="px-3 py-2 text-left font-medium">{kind === 'ReturnVisit' ? 'Xe · lần rửa cuối' : 'Gói'}</th>
            <th className="px-3 py-2 text-left font-medium">Tin sẽ gửi</th>
            <th className="px-3 py-2"><span className="sr-only">Thao tác</span></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {items.map((item) => (
            <DueRow key={item.reason.refId} item={item} checked={selected.has(item.reason.refId)}
              onToggle={() => toggle(item.reason.refId)} />
          ))}
        </tbody>
      </table>
      <div className="sticky bottom-0 flex items-center justify-end gap-4 border-t border-line bg-concrete py-3">
        <span className="text-ink-muted tabular-nums">Đã chọn {chosen.length}/{items.length}</span>
        <Button disabled={chosen.length === 0} onClick={() => setConfirming(true)}>Gửi {chosen.length || ''} tin nhắc</Button>
      </div>
      <SendDialog open={confirming} kind={kind} items={items.filter((item) => chosen.includes(item.reason.refId))}
        onClose={() => setConfirming(false)} onSent={() => { setSelected(new Set()); setConfirming(false) }} />
    </div>
  )
}

function DueRow({ item, checked, onToggle }: { item: DueReminder; checked: boolean; onToggle: () => void }) {
  const optOut = useSetAcceptsReminders()
  const { reason } = item
  return (
    <tr className="align-top">
      <td className="px-3 py-3">
        <input type="checkbox" aria-label={`Chọn ${reason.customerName}`} checked={checked} onChange={onToggle} className="size-5 accent-foam" />
      </td>
      <td className="px-3 py-3">
        <p className="font-medium">{reason.customerName}</p>
        <p className="tabular-nums text-ink-muted">{reason.phone}</p>
      </td>
      <td className="px-3 py-3">
        <div className="flex flex-col items-start gap-1">
          {reason.plate && <Plate value={reason.plate} />}
          {reason.packageName && <span className="font-medium">{reason.packageName}</span>}
          <span className="text-ink-muted">{reminderReason(reason)}</span>
          {reason.lastError && <span className="text-stop">Lần trước gửi lỗi: {reason.lastError}</span>}
        </div>
      </td>
      <td className="max-w-md px-3 py-3 text-ink-muted">{item.message}</td>
      <td className="px-3 py-3 text-right">
        <Button variant="ghost" busy={optOut.isPending} title="Khách không muốn nhận tin nhắc nữa"
          onClick={() => optOut.mutate({ customerId: reason.customerId, accepts: false },
            { onSuccess: () => toast.success(`${reason.customerName} sẽ không nhận tin nhắc nữa`) })}>
          Ngừng nhắc
        </Button>
        <FormError message={optOut.error?.message} />
      </td>
    </tr>
  )
}

interface SendDialogProps {
  open: boolean
  kind: ReminderKind
  items: DueReminder[]
  onClose: () => void
  onSent: () => void
}

function SendDialog({ open, kind, items, onClose, onSent }: SendDialogProps) {
  const send = useSendReminders()

  function handleSend() {
    send.mutate({ kind, refIds: items.map((item) => item.reason.refId) }, {
      onSuccess: (result) => { reportResult(result); onSent() },
    })
  }

  return (
    <Dialog open={open} onClose={onClose} title={`Gửi ${items.length} tin nhắc`}
      footer={<>
        <Button variant="secondary" onClick={onClose}>Để sau</Button>
        <Button onClick={handleSend} busy={send.isPending}>Gửi {items.length} tin</Button>
      </>}>
      <div className="flex flex-col gap-3">
        <p>Mỗi khách nhận một tin. Ví dụ tin gửi cho <span className="font-medium">{items[0]?.reason.customerName}</span>:</p>
        <p className="rounded-(--radius-control) bg-concrete px-4 py-3">{items[0]?.message}</p>
        <p className="text-caption text-ink-muted">Khách đã gửi rồi hoặc không còn đến hạn sẽ được bỏ qua.</p>
        <FormError message={send.error?.message} />
      </div>
    </Dialog>
  )
}

function reportResult(result: SendRemindersResult) {
  const failed = result.failures.length
  const skipped = result.skipped > 0 ? `, bỏ qua ${result.skipped}` : ''
  if (failed === 0) toast.success(`Đã gửi ${result.sent} tin nhắc${skipped}`)
  else toast.error(`Đã gửi ${result.sent} tin, lỗi ${failed}${skipped}. Khách gửi lỗi vẫn ở danh sách để gửi lại.`)
}
