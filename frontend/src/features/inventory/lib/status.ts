import type { StockDocumentDto } from '@/lib/api/types'
import type { PillTone } from '@/components/cenvo/StatusPill'

export interface StatusView {
  label: string
  tone: PillTone
}

/** Document status for people: a posted transfer still on the road reads "Đang chuyển", not "Đã ghi sổ". */
export function documentStatusView(doc: Pick<StockDocumentDto, 'status' | 'transferState'>): StatusView {
  if (doc.status === 'Draft') return { label: 'Nháp', tone: 'muted' }
  if (doc.status === 'Cancelled') return { label: 'Đã hủy', tone: 'stop' }
  if (doc.transferState === 'InTransit') return { label: 'Đang chuyển', tone: 'lane' }
  if (doc.transferState === 'Received') return { label: 'Đã nhận', tone: 'go' }
  return { label: 'Đã ghi sổ', tone: 'go' }
}
