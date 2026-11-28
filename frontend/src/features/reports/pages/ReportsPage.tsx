import { useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { PeriodPicker } from '../components/PeriodPicker'
import { todayInVietnam, type Period } from '../lib/period'
import { CafeReport } from '../sections/CafeReport'
import { CombinedReport } from '../sections/CombinedReport'
import { WashReport } from '../sections/WashReport'

type ReportTab = 'ca-phe' | 'rua-xe' | 'tong'

const TABS: { id: ReportTab; label: string; description: string; Report: (props: { period: Period }) => ReactNode }[] = [
  { id: 'ca-phe', label: 'Cà phê', description: 'Tính theo hóa đơn đã thanh toán, giờ Việt Nam.', Report: CafeReport },
  { id: 'rua-xe', label: 'Rửa xe', description: 'Tính theo phiếu đã thu tiền và gói bán ra, giờ Việt Nam.', Report: WashReport },
  { id: 'tong', label: 'Tổng', description: 'Cả quán: cà phê và rửa xe cộng lại.', Report: CombinedReport },
]

/** Sales report for managers: one period, three views. The tab lives in the URL so a reload or a shared link keeps it. */
export function ReportsPage() {
  const [today] = useState(() => todayInVietnam())
  const [period, setPeriod] = useState<Period>({ from: today, to: today })
  const [params, setParams] = useSearchParams()
  const tab = TABS.find((item) => item.id === params.get('phan-he')) ?? TABS[0]

  return (
    <>
      <PageHeader title="Báo cáo bán hàng" description={tab.description} />
      <div role="tablist" aria-label="Phân hệ" className="mb-4 flex gap-1 border-b border-line">
        {TABS.map((item) => (
          <button key={item.id} type="button" role="tab" aria-selected={item.id === tab.id}
            onClick={() => setParams({ 'phan-he': item.id }, { replace: true })}
            className={`-mb-px h-11 border-b-2 px-4 font-semibold ${item.id === tab.id ? tabColor(item.id) : 'border-transparent text-ink-muted'}`}>
            {item.label}
          </button>
        ))}
      </div>
      <PeriodPicker value={period} today={today} onChange={setPeriod} />
      <div role="tabpanel" aria-label={tab.label}><tab.Report period={period} /></div>
    </>
  )
}

/** Selected tab wears its module colour (design system): Cà phê = robusta, Rửa xe = foam, the whole shop = ink. */
function tabColor(id: ReportTab): string {
  if (id === 'ca-phe') return 'border-robusta text-ink'
  if (id === 'rua-xe') return 'border-foam text-ink'
  return 'border-ink text-ink'
}
