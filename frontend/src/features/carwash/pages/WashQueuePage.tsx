import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { QueueTicket, WashTicket } from '@/lib/api/types'
import { canCheckout } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { Hub, useRealtimeEvent } from '@/lib/realtime'
import { useFreshIds } from '@/lib/useFreshIds'
import { useStoredFlag } from '@/lib/useStoredFlag'
import { washKeys } from '../api/keys'
import { useMoveTicket, useWashQueue } from '../api/tickets'
import { CancelTicketDialog } from '../components/CancelTicketDialog'
import { QueueBoard } from '../components/QueueBoard'
import { RequireBranch } from '../components/RequireBranch'
import { TicketSheet } from '../components/TicketSheet'
import { WashPayDialog, type PayableTicket } from '../components/WashPayDialog'
import type { CardAction } from '../lib/status'

const DARK_MODE_KEY = 'cenvo.wash.dark'
/** ?phieu=<id> opens that ticket (intake sends the user here to take condition photos). */
const TICKET_PARAM = 'phieu'
const idOf = (ticket: QueueTicket) => ticket.id

export function WashQueuePage() {
  return <RequireBranch from="/rua-xe"><WashQueueScreen /></RequireBranch>
}

/** Bay tablet / wall screen: every car at the shop, moved along with one tap. Dark mode for a wall-mounted screen. */
function WashQueueScreen() {
  const queryClient = useQueryClient()
  const user = useSession()!.user
  const cashier = canCheckout(user)
  const queue = useWashQueue()
  const { mutate: move } = useMoveTicket()
  const [dark, setDark] = useStoredFlag(DARK_MODE_KEY)
  const [params, setParams] = useSearchParams()
  const openId = params.get(TICKET_PARAM)
  const setOpenId = useCallback((id: string | null) => setParams(id ? { [TICKET_PARAM]: id } : {}, { replace: true }), [setParams])
  const [paying, setPaying] = useState<PayableTicket | null>(null)
  const [cancelling, setCancelling] = useState<WashTicket | null>(null)
  const freshIds = useFreshIds(queue.data, idOf)

  // Another tablet changed a ticket → reload the board (and the open ticket).
  useRealtimeEvent('ticketsChanged', () => void queryClient.invalidateQueries({ queryKey: washKeys.tickets }), Hub.carWash)

  const handleOpen = useCallback((ticket: QueueTicket) => setOpenId(ticket.id), [setOpenId])
  const handleAction = useCallback((ticket: QueueTicket, action: NonNullable<CardAction>) => {
    if (action.kind === 'step') move({ id: ticket.id, step: action.step })
    if (action.kind === 'pay') setPaying(ticket)
    if (action.kind === 'open') setOpenId(ticket.id)
  }, [move, setOpenId])

  return (
    <div data-module="wash" className={`flex min-h-0 flex-1 flex-col bg-concrete text-ink ${dark ? 'dark' : ''}`}>
      <header className="flex h-16 shrink-0 items-center gap-4 border-b border-line bg-surface px-6">
        <h1 className="font-display text-screen font-semibold">Hàng chờ rửa xe</h1>
        <Button variant="secondary" className="ml-auto" onClick={() => setDark(!dark)} aria-pressed={dark}>
          {dark ? 'Giao diện sáng' : 'Giao diện tối'}
        </Button>
        <Link to="/rua-xe/tiep-nhan"
          className="inline-flex h-12 items-center rounded-(--radius-control) bg-(--module) px-5 font-semibold text-(--primary-foreground)">
          Tiếp nhận xe
        </Link>
      </header>
      <QueueContent queue={queue} freshIds={freshIds} canTakePayment={cashier} onOpen={handleOpen} onAction={handleAction} />
      <TicketSheet ticketId={openId} canTakePayment={cashier} onClose={() => setOpenId(null)}
        onPay={(ticket) => setPaying(ticket)} onCancel={(ticket) => setCancelling(ticket)} />
      <WashPayDialog ticket={paying} onClose={() => setPaying(null)} />
      <CancelTicketDialog ticket={cancelling} onClose={() => setCancelling(null)}
        onCancelled={() => { setCancelling(null); setOpenId(null) }} />
    </div>
  )
}

interface QueueContentProps {
  queue: ReturnType<typeof useWashQueue>
  freshIds: ReadonlySet<string>
  canTakePayment: boolean
  onOpen: (ticket: QueueTicket) => void
  onAction: (ticket: QueueTicket, action: NonNullable<CardAction>) => void
}

function QueueContent({ queue, freshIds, canTakePayment, onOpen, onAction }: QueueContentProps) {
  if (queue.isPending) return <div className="m-4 h-40 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (queue.isError) {
    return <EmptyState className="flex-1" title="Không tải được hàng chờ" description={queue.error.message}
      action={<Button variant="secondary" onClick={() => queue.refetch()}>Tải lại</Button>} />
  }
  if (queue.data.length === 0) {
    return <EmptyState className="flex-1" title="Chưa có xe nào đang chờ"
      action={<Link to="/rua-xe/tiep-nhan" className="font-semibold text-(--module) underline">Tiếp nhận xe mới</Link>} />
  }
  return <QueueBoard tickets={queue.data} freshIds={freshIds} canTakePayment={canTakePayment} onOpen={onOpen} onAction={onAction} />
}
