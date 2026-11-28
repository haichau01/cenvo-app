import { useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { FormError } from '@/components/ui/FormError'
import { isOwner } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { vnDate } from '@/lib/format'
import { useToReview } from '../api/attendance'
import { DayTab } from '../components/DayTab'
import { HolidaysTab } from '../components/HolidaysTab'
import { RecordTable } from '../components/RecordTable'
import { TimesheetTab } from '../components/TimesheetTab'

type Tab = 'can-xu-ly' | 'theo-ngay' | 'bang-cong' | 'ngay-le'

const TABS: { id: Tab; label: string }[] = [
  { id: 'can-xu-ly', label: 'Cần xử lý' },
  { id: 'theo-ngay', label: 'Theo ngày' },
  { id: 'bang-cong', label: 'Bảng công tháng' },
  { id: 'ngay-le', label: 'Ngày lễ' },
]

/** Branch managers: approve unplanned work and overtime, fix forgotten check-outs, read the monthly timesheet. */
export function AttendancePage() {
  const user = useSession()!.user
  const [today] = useState(() => vnDate(Date.now()))
  const [params, setParams] = useSearchParams()
  const tab = TABS.find((item) => item.id === params.get('tab')) ?? TABS[0]
  const panels: Record<Tab, ReactNode> = {
    'can-xu-ly': <ToReviewTab />,
    'theo-ngay': <DayTab today={today} />,
    'bang-cong': <TimesheetTab today={today} />,
    'ngay-le': <HolidaysTab today={today} canEdit={isOwner(user)} />,
  }

  return (
    <>
      <PageHeader title="Chấm công" description="Máy chấm công ở quán: mở màn Máy chấm công trên tablet thu ngân." />
      <div role="tablist" aria-label="Chấm công" className="mb-4 flex gap-1 border-b border-line">
        {TABS.map((item) => (
          <button key={item.id} type="button" role="tab" aria-selected={item.id === tab.id}
            onClick={() => setParams({ tab: item.id }, { replace: true })}
            className={`-mb-px h-11 border-b-2 px-4 font-semibold ${item.id === tab.id ? 'border-ink text-ink' : 'border-transparent text-ink-muted'}`}>
            {item.label}
          </button>
        ))}
      </div>
      <div role="tabpanel" aria-label={tab.label}>{panels[tab.id]}</div>
    </>
  )
}

function ToReviewTab() {
  const rows = useToReview()
  return (
    <div className="flex flex-col gap-4">
      <p className="text-ink-muted">Lượt ngoài lịch chờ duyệt, quên chấm ra và tăng ca chưa duyệt trong 2 tháng gần đây.</p>
      <FormError message={rows.error?.message} />
      {rows.isPending && <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {rows.data && rows.data.length === 0 && <EmptyState title="Không còn gì cần xử lý" description="Bảng công tháng đã sẵn sàng cho tính lương." />}
      {rows.data && rows.data.length > 0 && <RecordTable rows={rows.data} showDate />}
    </div>
  )
}
