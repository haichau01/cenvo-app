import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import { paperWidth, setPaperWidth, type PaperWidth } from '@/lib/print'
import { usePrintReceipt, useReceipt } from '../api/receipts'
import { Receipt } from '../components/receipt/Receipt'

const PAPERS: { value: PaperWidth; label: string }[] = [
  { value: '80mm', label: 'Giấy 80 mm' },
  { value: '58mm', label: 'Giấy 58 mm' },
]

/** /in/hoa-don/:id — see the receipt at real paper width, choose this device's paper size, print or reprint. */
export function ReceiptPreviewPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const receipt = useReceipt(id!)
  const printReceipt = usePrintReceipt()
  const [paper, setPaper] = useState(paperWidth)
  const [printedAt] = useState(() => new Date().toISOString())

  function handlePaper(value: PaperWidth) {
    setPaperWidth(value)
    setPaper(value)
  }

  return (
    <div className="min-h-dvh bg-concrete px-6 py-6">
      <header className="mx-auto mb-6 flex max-w-3xl flex-wrap items-center gap-3">
        <Button variant="ghost" onClick={() => navigate(-1)}>Quay lại</Button>
        <h1 className="font-display text-screen font-semibold">Xem trước hóa đơn</h1>
        <div role="group" aria-label="Khổ giấy" className="ml-auto flex rounded-(--radius-control) bg-surface p-1">
          {PAPERS.map((option) => (
            <button key={option.value} type="button" aria-pressed={paper === option.value} onClick={() => handlePaper(option.value)}
              className={`h-10 rounded-[4px] px-4 font-semibold ${paper === option.value ? 'bg-ink text-white' : 'text-ink-muted'}`}>
              {option.label}
            </button>
          ))}
        </div>
        <Button onClick={() => printReceipt.mutate(id!)} busy={printReceipt.isPending} disabled={!receipt.data}>In</Button>
      </header>
      {receipt.isPending && <div className="mx-auto h-96 w-[80mm] animate-pulse bg-surface" aria-busy />}
      {receipt.isError && <EmptyState title="Không mở được hóa đơn" description={receipt.error.message}
        action={<Link to="/pos" className="font-semibold underline">Về màn bán hàng</Link>} />}
      {receipt.data && (
        <div className="mx-auto shadow-overlay" style={{ width: paper }}>
          <Receipt receipt={receipt.data} printedAt={printedAt} />
        </div>
      )}
    </div>
  )
}
