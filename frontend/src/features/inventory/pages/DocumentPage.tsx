import { Link, useParams, useSearchParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { Button } from '@/components/ui/Button'
import type { StockDocumentType } from '@/lib/api/types'
import { useDocument } from '../api/documents'
import { useWarehouses } from '../api/references'
import { DocumentEditor } from '../components/DocumentEditor'
import { DocumentView } from '../components/DocumentView'
import { useToday } from '../hooks/useToday'
import { draftFromDto, emptyDraft } from '../lib/documentForm'
import { DOCUMENT_TYPES, isDocumentType } from '../lib/documentTypes'

const backLink = <Link to="/kho/phieu" className="font-medium underline">Về danh sách phiếu</Link>

/** /kho/phieu/moi?loai=… (new) or /kho/phieu/:id — drafts open in the editor, everything else read-only. */
export function DocumentPage() {
  const { id } = useParams()
  return id ? <ExistingDocument id={id} /> : <NewDocument />
}

function NewDocument() {
  const [params] = useSearchParams()
  const type = params.get('loai')
  const warehouses = useWarehouses()
  if (!isDocumentType(type) || !DOCUMENT_TYPES[type].manual) {
    return <EmptyState title="Loại phiếu không hợp lệ" description="Chọn lại loại phiếu từ danh sách phiếu kho." action={backLink} />
  }
  if (warehouses.isPending) return <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  const defaultWarehouse = warehouses.data?.[0]?.id ?? '' // the backend lists the default warehouse first
  return <NewDocumentForm type={type} warehouseId={defaultWarehouse} />
}

function NewDocumentForm({ type, warehouseId }: { type: StockDocumentType; warehouseId: string }) {
  const today = useToday()
  return (
    <>
      <PageHeader title={`${DOCUMENT_TYPES[type].title} mới`} />
      <DocumentEditor key={type} documentId={null} initial={emptyDraft(type, warehouseId, today)} today={today} />
    </>
  )
}

function ExistingDocument({ id }: { id: string }) {
  const doc = useDocument(id)
  const today = useToday()
  if (doc.isPending) return <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (doc.isError) {
    return <EmptyState title="Không mở được phiếu" description={doc.error.message}
      action={<Button variant="secondary" onClick={() => doc.refetch()}>Tải lại</Button>} />
  }
  const title = `${DOCUMENT_TYPES[doc.data.type].title} ${doc.data.code}`
  return (
    <>
      <PageHeader title={title} actions={backLink} />
      {doc.data.status === 'Draft'
        ? <DocumentEditor key={doc.data.id} documentId={doc.data.id} initial={draftFromDto(doc.data)} today={today} />
        : <DocumentView doc={doc.data} />}
    </>
  )
}
