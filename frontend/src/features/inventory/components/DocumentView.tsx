import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/toastStore'
import type { StockDocumentDto } from '@/lib/api/types'
import { formatDateTime } from '@/lib/format'
import { isManager } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { useIncomingTransfers, useReceiveTransfer } from '../api/documents'
import { DOCUMENT_TYPES } from '../lib/documentTypes'
import { documentStatusView } from '../lib/status'
import { CancelDocumentDialog } from './CancelDocumentDialog'
import { DocumentLinesTable } from './DocumentLinesTable'

/** A posted or cancelled document: read-only, with the actions still possible (cancel, receive). */
export function DocumentView({ doc }: { doc: StockDocumentDto }) {
  const user = useSession()!.user
  const incoming = useIncomingTransfers()
  const receive = useReceiveTransfer()
  const [cancelling, setCancelling] = useState(false)
  const config = DOCUMENT_TYPES[doc.type]
  const status = documentStatusView(doc)
  const canCancel = doc.status === 'Posted' && config.manual && isManager(user)
  const canReceive = (incoming.data ?? []).some((d) => d.id === doc.id)

  function handleReceive() {
    receive.mutate(doc.id, { onSuccess: () => toast.success(`Đã nhận hàng phiếu ${doc.code}`) })
  }

  return (
    <div className="flex flex-col gap-6">
      <section className="grid grid-cols-[repeat(auto-fill,minmax(14rem,1fr))] gap-x-6 gap-y-3 rounded-(--radius-panel) bg-surface p-5">
        <Info label="Trạng thái"><StatusPill label={status.label} tone={status.tone} /></Info>
        <Info label="Ngày chứng từ">{formatDateTime(doc.date)}</Info>
        <Info label={doc.toWarehouseName ? 'Kho đi' : 'Kho'}>{doc.warehouseName}</Info>
        {doc.toWarehouseName && <Info label="Kho đến">{doc.toWarehouseName}</Info>}
        {doc.supplierName && <Info label="Nhà cung cấp">{doc.supplierName}</Info>}
        {doc.refCode && <Info label="Tham chiếu">{doc.refCode}</Info>}
        {doc.totalAmount > 0 && <Info label="Tổng tiền"><Money value={doc.totalAmount} className="font-semibold" /></Info>}
        {doc.note && <Info label="Ghi chú">{doc.note}</Info>}
        {doc.cancelReason && <Info label="Lý do hủy"><span className="text-stop">{doc.cancelReason}</span></Info>}
      </section>
      <section className="rounded-(--radius-panel) bg-surface p-5" aria-label="Dòng hàng">
        <DocumentLinesTable doc={doc} config={config} />
      </section>
      <div className="flex justify-end gap-3">
        {canCancel && <Button variant="danger" onClick={() => setCancelling(true)}>Hủy phiếu</Button>}
        {canReceive && <Button size="lg" onClick={handleReceive} busy={receive.isPending}>Xác nhận nhận hàng</Button>}
      </div>
      <CancelDocumentDialog doc={cancelling ? doc : null} onClose={() => setCancelling(false)} />
    </div>
  )
}

function Info({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-caption text-ink-muted">{label}</span>
      <span>{children}</span>
    </div>
  )
}
