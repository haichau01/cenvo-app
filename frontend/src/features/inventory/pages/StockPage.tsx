import { useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { Button } from '@/components/ui/Button'
import { controlClass } from '@/components/ui/fieldStyles'
import { SelectField } from '@/components/ui/SelectField'
import type { BalanceDto } from '@/lib/api/types'
import { searchBy } from '@/lib/search'
import { useWarehouses } from '../api/references'
import { useBalances } from '../api/stock'
import { BalanceTable } from '../components/BalanceTable'
import { StockCardDialog } from '../components/StockCardDialog'

export function StockPage() {
  const warehouses = useWarehouses()
  const [warehouseId, setWarehouseId] = useState('')
  const [search, setSearch] = useState('')
  const [belowMinOnly, setBelowMinOnly] = useState(false)
  const balances = useBalances({ warehouseId: warehouseId || null, belowMinOnly })
  // Typing stays responsive: filtering follows the deferred value.
  const deferredSearch = useDeferredValue(search)
  const rows = useMemo(() => filterRows(balances.data, deferredSearch), [balances.data, deferredSearch])
  const [selected, setSelected] = useState<BalanceDto | null>(null)

  return (
    <>
      <PageHeader title="Tồn kho" description="Số lượng tính theo đơn vị cơ sở của từng sản phẩm."
        actions={<Link to="/kho/phieu/moi?loai=PurchaseReceipt" className="inline-flex h-12 items-center rounded-(--radius-control) bg-(--module) px-4 font-semibold text-(--primary-foreground)">Nhập hàng</Link>} />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <SelectField label="Kho" value={warehouseId} onChange={setWarehouseId} placeholder="Tất cả kho" className="w-56"
          options={(warehouses.data ?? []).map((w) => ({ value: w.id, label: w.name }))} />
        <label className="flex w-72 flex-col gap-1.5">
          <span className="text-sm font-medium">Tìm sản phẩm</span>
          <input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Mã hoặc tên"
            className={controlClass} />
        </label>
        <label className="flex h-12 items-center gap-2 font-medium">
          <input type="checkbox" checked={belowMinOnly} onChange={(event) => setBelowMinOnly(event.target.checked)} className="size-5 accent-ink" />
          Chỉ hàng dưới mức tối thiểu
        </label>
      </div>

      <StockContent balances={balances} rows={rows} onSelect={setSelected} filtered={Boolean(search || warehouseId || belowMinOnly)} />
      <StockCardDialog balance={selected} onClose={() => setSelected(null)} />
    </>
  )
}

/** Diacritic-insensitive search on code, name and warehouse ("sua tuoi" finds "Sữa tươi"). */
function filterRows(rows: BalanceDto[] | undefined, search: string): BalanceDto[] {
  if (!rows || !search.trim()) return rows ?? []
  return searchBy(rows, search, (row) => [row.productCode, row.productName, row.warehouseName], rows.length)
}

interface StockContentProps {
  balances: ReturnType<typeof useBalances>
  rows: BalanceDto[]
  onSelect: (balance: BalanceDto) => void
  filtered: boolean
}

function StockContent({ balances, rows, onSelect, filtered }: StockContentProps) {
  if (balances.isPending) return <div className="h-64 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (balances.isError) {
    return <EmptyState title="Không tải được tồn kho" description={balances.error.message}
      action={<Button variant="secondary" onClick={() => balances.refetch()}>Tải lại</Button>} />
  }
  if (rows.length === 0) {
    return filtered
      ? <EmptyState title="Không có sản phẩm khớp bộ lọc" description="Đổi kho, từ khóa hoặc bỏ chọn “dưới mức tối thiểu”." />
      : <EmptyState title="Kho chưa có hàng" description="Tạo phiếu nhập mua hoặc phiếu tồn đầu kỳ để bắt đầu theo dõi tồn kho."
          action={<Link to="/kho/phieu/moi?loai=PurchaseReceipt" className="font-semibold underline">Tạo phiếu nhập mua</Link>} />
  }
  return <BalanceTable rows={rows} onSelect={onSelect} />
}
