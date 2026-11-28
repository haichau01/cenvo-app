import { useState } from 'react'
import { vnDate } from '@/lib/format'
import { Link } from 'react-router'
import { Money } from '@/components/cenvo/Money'
import { Dialog } from '@/components/ui/Dialog'
import { controlClass } from '@/components/ui/fieldStyles'
import type { BalanceDto, StockCardDto } from '@/lib/api/types'
import { formatDate, formatQuantity } from '@/lib/format'
import { useStockCard } from '../api/stock'
import { DOCUMENT_TYPES } from '../lib/documentTypes'

const DEFAULT_DAYS = 30
const DAY_MS = 86_400_000

/** Stock card: opening balance, every movement in the period with a running balance, closing balance. */
export function StockCardDialog({ balance, onClose }: { balance: BalanceDto | null; onClose: () => void }) {
  return (
    <Dialog open={balance !== null} onClose={onClose} className="w-[min(60rem,calc(100vw-2rem))]"
      title={balance ? `Thẻ kho · ${balance.productName} · ${balance.warehouseName}` : ''}>
      {balance && <StockCard key={`${balance.warehouseId}-${balance.productId}`} balance={balance} />}
    </Dialog>
  )
}

function StockCard({ balance }: { balance: BalanceDto }) {
  const [from, setFrom] = useState(() => vnDate(Date.now() - DEFAULT_DAYS * DAY_MS))
  const [to, setTo] = useState(() => vnDate(Date.now()))
  const card = useStockCard({ productId: balance.productId, warehouseId: balance.warehouseId, from, to })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex gap-3">
        <label className="flex flex-col gap-1.5 text-sm font-medium">Từ ngày
          <input type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} className={controlClass} />
        </label>
        <label className="flex flex-col gap-1.5 text-sm font-medium">Đến ngày
          <input type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} className={controlClass} />
        </label>
      </div>
      {card.isPending && <div className="h-40 animate-pulse rounded-(--radius-panel) bg-concrete" aria-busy />}
      {card.isError && <p role="alert" className="text-stop">{card.error.message}</p>}
      {card.data && <StockCardTable card={card.data} />}
    </div>
  )
}

const num = 'px-3 text-right tabular-nums'

function StockCardTable({ card }: { card: StockCardDto }) {
  return (
    <table className="w-full text-table">
      <thead className="border-b border-line text-caption text-ink-muted">
        <tr className="h-10">
          <th className="px-3 text-left font-medium">Ngày</th>
          <th className="px-3 text-left font-medium">Chứng từ</th>
          <th className={`${num} font-medium`}>Nhập</th>
          <th className={`${num} font-medium`}>Xuất</th>
          <th className={`${num} font-medium`}>Đơn giá</th>
          <th className={`${num} font-medium`}>Tồn ({card.unitName})</th>
        </tr>
      </thead>
      <tbody>
        <SummaryRow label="Tồn đầu kỳ" balance={card.opening} />
        {card.rows.map((row, index) => (
          <tr key={`${row.documentId}-${index}`} className="h-10 border-b border-line">
            <td className="px-3 tabular-nums">{formatDate(row.date)}</td>
            <td className="px-3">
              <Link to={`/kho/phieu/${row.documentId}`} className="font-medium hover:underline">{row.documentCode}</Link>
              <span className="text-ink-muted"> · {DOCUMENT_TYPES[row.documentType].title}{row.isReversal ? ' (đảo)' : ''}</span>
            </td>
            <td className={num}>{row.in > 0 ? formatQuantity(row.in) : ''}</td>
            <td className={num}>{row.out > 0 ? formatQuantity(row.out) : ''}</td>
            <td className={num}><Money value={row.unitCost} /></td>
            <td className={`${num} font-medium`}>{formatQuantity(row.balance)}</td>
          </tr>
        ))}
        {card.rows.length === 0 && <tr><td colSpan={6} className="px-3 py-6 text-center text-ink-muted">Không có phát sinh trong khoảng này.</td></tr>}
        <SummaryRow label="Tồn cuối kỳ" balance={card.closing} totalIn={card.totalIn} totalOut={card.totalOut} />
      </tbody>
    </table>
  )
}

function SummaryRow({ label, balance, totalIn, totalOut }: { label: string; balance: number; totalIn?: number; totalOut?: number }) {
  return (
    <tr className="h-10 border-b border-line bg-concrete font-semibold">
      <td className="px-3" colSpan={2}>{label}</td>
      <td className={num}>{totalIn !== undefined && formatQuantity(totalIn)}</td>
      <td className={num}>{totalOut !== undefined && formatQuantity(totalOut)}</td>
      <td />
      <td className={num}>{formatQuantity(balance)}</td>
    </tr>
  )
}
