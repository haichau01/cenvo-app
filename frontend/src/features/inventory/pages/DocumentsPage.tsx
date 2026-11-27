import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { Button } from '@/components/ui/Button'
import { SelectField } from '@/components/ui/SelectField'
import type { DocumentStatus, StockDocumentType } from '@/lib/api/types'
import { useDocuments } from '../api/documents'
import { CreateDocumentDialog } from '../components/CreateDocumentDialog'
import { DocumentTable } from '../components/DocumentTable'
import { IncomingTransfers } from '../components/IncomingTransfers'
import { DOCUMENT_TYPES } from '../lib/documentTypes'

const TYPE_OPTIONS = Object.values(DOCUMENT_TYPES).map((config) => ({ value: config.type, label: config.title }))
const STATUS_OPTIONS: { value: DocumentStatus; label: string }[] = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Posted', label: 'Đã ghi sổ' },
  { value: 'Cancelled', label: 'Đã hủy' },
]

export function DocumentsPage() {
  const [type, setType] = useState('')
  const [status, setStatus] = useState('')
  const [creating, setCreating] = useState(false)
  const documents = useDocuments({ type: (type || null) as StockDocumentType | null, status: (status || null) as DocumentStatus | null })

  return (
    <>
      <PageHeader title="Phiếu kho" description="Nhập, xuất, chuyển kho và kiểm kê. Phiếu đã ghi sổ chỉ hủy được, không sửa."
        actions={<Button onClick={() => setCreating(true)}>Tạo phiếu</Button>} />
      <IncomingTransfers />
      <div className="mb-4 flex flex-wrap gap-3">
        <SelectField label="Loại phiếu" value={type} onChange={setType} placeholder="Tất cả loại" options={TYPE_OPTIONS} className="w-64" />
        <SelectField label="Trạng thái" value={status} onChange={setStatus} placeholder="Tất cả trạng thái" options={STATUS_OPTIONS} className="w-52" />
      </div>
      <DocumentsContent documents={documents} onCreate={() => setCreating(true)} />
      <CreateDocumentDialog open={creating} onClose={() => setCreating(false)} />
    </>
  )
}

function DocumentsContent({ documents, onCreate }: { documents: ReturnType<typeof useDocuments>; onCreate: () => void }) {
  if (documents.isPending) return <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (documents.isError) {
    return <EmptyState title="Không tải được phiếu kho" description={documents.error.message}
      action={<Button variant="secondary" onClick={() => documents.refetch()}>Tải lại</Button>} />
  }
  if (documents.data.length === 0) {
    return <EmptyState title="Chưa có phiếu nào khớp bộ lọc" description="Tạo phiếu nhập mua khi hàng về, hoặc phiếu kiểm kê cuối ngày."
      action={<Button onClick={onCreate}>Tạo phiếu</Button>} />
  }
  return <DocumentTable documents={documents.data} />
}
