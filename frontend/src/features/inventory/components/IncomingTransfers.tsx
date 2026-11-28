import { Link } from 'react-router'
import { useIncomingTransfers } from '../api/documents'

/** Transfers sent to this branch and not yet received — the receiving side must act, so it uses `lane`. */
export function IncomingTransfers() {
  const incoming = useIncomingTransfers()
  if (!incoming.data?.length) return null
  return (
    <section aria-label="Hàng chuyển đến" className="mb-4 rounded-(--radius-panel) border-l-4 border-lane bg-surface px-4 py-3">
      <p className="font-semibold">Có {incoming.data.length} phiếu chuyển kho đang chờ bạn xác nhận nhận hàng</p>
      <ul className="mt-1 flex flex-wrap gap-x-4 gap-y-1">
        {incoming.data.map((doc) => (
          <li key={doc.id}>
            <Link to={`/kho/phieu/${doc.id}`} className="font-medium underline">{doc.code}</Link>
            <span className="text-ink-muted"> từ {doc.warehouseName}</span>
          </li>
        ))}
      </ul>
    </section>
  )
}
