import { Link, useNavigate } from 'react-router'
import { Money } from '@/components/cenvo/Money'
import { StatusPill } from '@/components/cenvo/StatusPill'
import type { StockDocumentDto } from '@/lib/api/types'
import { formatDate } from '@/lib/format'
import { DOCUMENT_TYPES } from '../lib/documentTypes'
import { documentStatusView } from '../lib/status'

const th = 'px-4 py-3 text-left text-caption font-medium text-ink-muted'

export function DocumentTable({ documents }: { documents: StockDocumentDto[] }) {
  const navigate = useNavigate()
  return (
    <div className="overflow-x-auto rounded-(--radius-panel) bg-surface">
      <table className="w-full text-table">
        <thead className="border-b border-line">
          <tr>
            <th className={th}>Mã phiếu</th>
            <th className={th}>Ngày</th>
            <th className={th}>Loại</th>
            <th className={th}>Kho</th>
            <th className={th}>Nhà cung cấp / tham chiếu</th>
            <th className={`${th} text-right`}>Tổng tiền</th>
            <th className={th}>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          {documents.map((doc) => {
            const status = documentStatusView(doc)
            return (
              <tr key={doc.id} onClick={() => navigate(`/kho/phieu/${doc.id}`)}
                className="h-11 cursor-pointer border-b border-line last:border-0 hover:bg-concrete">
                <td className="px-4 font-semibold tabular-nums">
                  <Link to={`/kho/phieu/${doc.id}`} onClick={(event) => event.stopPropagation()} className="hover:underline">{doc.code}</Link>
                </td>
                <td className="px-4 tabular-nums">{formatDate(doc.date)}</td>
                <td className="px-4">{DOCUMENT_TYPES[doc.type].title}</td>
                <td className="px-4">{doc.warehouseName}{doc.toWarehouseName && <span className="text-ink-muted"> → {doc.toWarehouseName}</span>}</td>
                <td className="px-4 text-ink-muted">{doc.supplierName ?? doc.refCode ?? ''}</td>
                <td className="px-4 text-right tabular-nums">{doc.totalAmount > 0 ? <Money value={doc.totalAmount} /> : '—'}</td>
                <td className="px-4"><StatusPill label={status.label} tone={status.tone} /></td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
