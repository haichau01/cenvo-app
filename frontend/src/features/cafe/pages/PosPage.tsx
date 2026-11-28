import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { toast } from '@/components/ui/toastStore'
import { currentBranchName } from '@/features/auth/lib/branches'
import type { AddLineInput, MenuItem, TableDto, UserIdentity } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'
import { useRealtimeEvent } from '@/lib/realtime'
import { cafeKeys } from '../api/keys'
import { useAddLine, useCreateOrder } from '../api/orders'
import { useCurrentShift } from '../api/shifts'
import { canCheckout } from '@/lib/auth/roles'
import { CloseShiftDialog } from '../components/CloseShiftDialog'
import { ItemOptionsDialog } from '../components/ItemOptionsDialog'
import { MenuBoard } from '../components/MenuBoard'
import { OpenShiftDialog } from '../components/OpenShiftDialog'
import { OrderPanel } from '../components/OrderPanel'
import { PosTopBar, type PosView } from '../components/PosTopBar'
import { ReprintDialog } from '../components/ReprintDialog'
import { TableMap } from '../components/TableMap'

const ORDER_PARAM = 'hd'
const CASHIER_ONLY = 'Chỉ thu ngân hoặc quản lý được thu tiền. Nhờ thu ngân thanh toán hóa đơn này.'

export function PosPage() {
  const user = useSession()!.user
  if (user.branchId == null) {
    return <EmptyState className="flex-1" title="Chọn chi nhánh để bán hàng"
      description="Đang xem toàn chuỗi. Màn bán hàng cần làm việc tại một chi nhánh cụ thể."
      action={<Link to="/chon-chi-nhanh" state={{ from: '/pos' }} className="font-semibold text-(--module) underline">Chọn chi nhánh</Link>} />
  }
  return <PosScreen user={user} />
}

function PosScreen({ user }: { user: UserIdentity }) {
  const queryClient = useQueryClient()
  const [params, setParams] = useSearchParams()
  const orderId = params.get(ORDER_PARAM)
  const [view, setView] = useState<PosView>(orderId ? 'menu' : 'tables')
  const [optionsItem, setOptionsItem] = useState<MenuItem | null>(null)
  const [shiftDialog, setShiftDialog] = useState<'open' | 'close' | null>(null)
  const [reprinting, setReprinting] = useState(false)
  const cashier = canCheckout(user)
  const shift = useCurrentShift(cashier)
  const createOrder = useCreateOrder()
  const { mutate: addLine } = useAddLine()

  // Another device changed an order → refresh the floor plan and bills on this screen.
  useRealtimeEvent('ordersChanged', () => {
    void queryClient.invalidateQueries({ queryKey: cafeKeys.orders })
    void queryClient.invalidateQueries({ queryKey: cafeKeys.tables })
  })

  function selectOrder(id: string | null) {
    setParams(id ? { [ORDER_PARAM]: id } : {}, { replace: true })
    setView(id ? 'menu' : 'tables')
  }

  function handleTable(table: TableDto) {
    if (table.openOrderId) return selectOrder(table.openOrderId)
    createOrder.mutate({ type: 'DineIn', tableId: table.id }, { onSuccess: (order) => selectOrder(order.id) })
  }

  function handleNewTakeAway() {
    createOrder.mutate({ type: 'TakeAway' }, { onSuccess: (order) => selectOrder(order.id) })
  }

  const handleAdd = useCallback((item: MenuItem) => {
    if (!orderId) return
    addLine({ orderId, item, input: { productId: item.productId, quantity: 1, toppings: [], note: null } })
  }, [orderId, addLine])

  function handleOptionsConfirm(item: MenuItem, input: AddLineInput) {
    if (orderId) addLine({ orderId, item, input })
    setOptionsItem(null)
  }

  function handleCloseShift() {
    void shift.refetch() // the cached summary dates from when the shift was opened
    setShiftDialog('close')
  }

  function handleRequestShift() {
    if (cashier) setShiftDialog('open')
    else toast.error(CASHIER_ONLY)
  }

  return (
    <div className="grid min-h-0 flex-1 grid-cols-[1fr_minmax(22rem,26rem)]">
      <main className="flex min-h-0 flex-col">
        <PosTopBar view={view} onView={setView} hasOrder={orderId !== null} branchName={currentBranchName(user)}
          canCheckout={cashier} shift={shift.data} onOpenShift={() => setShiftDialog('open')}
          onCloseShift={handleCloseShift} onReprint={() => setReprinting(true)} />
        <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
          {view === 'tables'
            ? <TableMap selectedOrderId={orderId} onTable={handleTable} onOrder={selectOrder}
                onNewTakeAway={handleNewTakeAway} creating={createOrder.isPending} />
            : <MenuBoard onAdd={handleAdd} onOptions={setOptionsItem} disabled={orderId === null} />}
        </div>
      </main>
      <OrderPanel orderId={orderId} shiftOpen={cashier && shift.data != null} onRequestShift={handleRequestShift}
        onFinished={() => selectOrder(null)} onSelectOrder={selectOrder} />

      <ItemOptionsDialog item={optionsItem} onClose={() => setOptionsItem(null)} onConfirm={handleOptionsConfirm} />
      <OpenShiftDialog open={shiftDialog === 'open'} onClose={() => setShiftDialog(null)} />
      <ReprintDialog open={reprinting} onClose={() => setReprinting(false)} />
      <CloseShiftDialog shift={shiftDialog === 'close' ? (shift.data ?? null) : null} onClose={() => setShiftDialog(null)} />
    </div>
  )
}
