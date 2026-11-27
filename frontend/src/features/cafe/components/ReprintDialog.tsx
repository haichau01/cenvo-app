import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { controlClass } from '@/components/ui/fieldStyles'
import { formatTime } from '@/lib/format'
import { searchBy } from '@/lib/search'
import { usePrintReceipt, useTodayPaidOrders } from '../api/receipts'
import { orderTitle } from '../lib/order'

/** Reprint a receipt paid today (customer asks for one later, printer jammed…). */
export function ReprintDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog open={open} onClose={onClose} title="In lại hóa đơn hôm nay" className="w-[min(40rem,calc(100vw-2rem))]">
      {open && <ReprintList />}
    </Dialog>
  )
}

function ReprintList() {
  const orders = useTodayPaidOrders(true)
  const printReceipt = usePrintReceipt()
  const [search, setSearch] = useState('')
  const visible = useMemo(() => {
    const list = orders.data ?? []
    return search.trim() ? searchBy(list, search, (o) => [o.code, orderTitle(o)], list.length) : list
  }, [orders.data, search])

  if (orders.isPending) return <div className="h-40 animate-pulse rounded-(--radius-panel) bg-concrete" aria-busy />
  if (orders.isError) return <p role="alert" className="text-stop">{orders.error.message}</p>
  return (
    <div className="flex flex-col gap-4">
      <input type="search" value={search} onChange={(event) => setSearch(event.target.value)} aria-label="Tìm hóa đơn"
        placeholder="Mã hóa đơn hoặc bàn" className={controlClass} />
      {visible.length === 0 && <p className="py-6 text-center text-ink-muted">Hôm nay chưa có hóa đơn đã thanh toán nào khớp.</p>}
      <ul className="flex flex-col divide-y divide-line">
        {visible.map((order) => (
          <li key={order.id} className="flex items-center gap-4 py-2">
            <div className="flex-1">
              <p className="font-semibold tabular-nums">{order.code}</p>
              <p className="text-caption text-ink-muted">{orderTitle(order)} · {order.paidAt && formatTime(order.paidAt)}</p>
            </div>
            <Money value={order.total} className="font-semibold" />
            <Link to={`/in/hoa-don/${order.id}`} className="px-2 font-medium underline">Xem</Link>
            <Button variant="secondary" onClick={() => printReceipt.mutate(order.id)}>In</Button>
          </li>
        ))}
      </ul>
    </div>
  )
}
