import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import type { KitchenTicket } from '@/lib/api/types'
import { useRealtimeEvent } from '@/lib/realtime'
import { useStoredFlag } from '@/lib/useStoredFlag'
import { useKitchenQueue, useMoveTicket } from '../api/kitchen'
import { cafeKeys } from '../api/keys'
import { KitchenBoard } from '../components/KitchenBoard'
import { isQueueStatus, nextStatus, previousStatus } from '../lib/kitchen'

const DARK_MODE_KEY = 'cenvo.kitchen.dark'

/** Bar screen: drinks sent from the POS, moved along by the barista. Dark mode for evening shifts. */
export function KitchenPage() {
  const queryClient = useQueryClient()
  const queue = useKitchenQueue()
  const { mutate: move } = useMoveTicket()
  const [dark, setDark] = useStoredFlag(DARK_MODE_KEY)
  const freshIds = useFreshIds(queue.data)

  useRealtimeEvent('ordersChanged', () => void queryClient.invalidateQueries({ queryKey: cafeKeys.kitchen }))

  const handleForward = useCallback((ticket: KitchenTicket) => {
    if (isQueueStatus(ticket.status)) move({ lineId: ticket.lineId, status: nextStatus(ticket.status) })
  }, [move])

  const handleBack = useCallback((ticket: KitchenTicket) => {
    const previous = isQueueStatus(ticket.status) ? previousStatus(ticket.status) : null
    if (previous) move({ lineId: ticket.lineId, status: previous })
  }, [move])

  return (
    // data-module is repeated here so the module color re-resolves with the dark palette.
    <div data-module="cafe" className={`flex min-h-0 flex-1 flex-col bg-concrete text-ink ${dark ? 'dark' : ''}`}>
      <header className="flex h-16 shrink-0 items-center gap-4 border-b border-line bg-surface px-6">
        <h1 className="font-display text-screen font-semibold">Pha chế</h1>
        {queue.data && <span className="text-ink-muted tabular-nums">{queue.data.length} món đang chờ xử lý</span>}
        <Button variant="secondary" className="ml-auto" onClick={() => setDark(!dark)} aria-pressed={dark}>
          {dark ? 'Giao diện sáng' : 'Giao diện tối'}
        </Button>
      </header>
      <KitchenContent queue={queue} freshIds={freshIds} onForward={handleForward} onBack={handleBack} />
    </div>
  )
}

interface KitchenContentProps {
  queue: ReturnType<typeof useKitchenQueue>
  freshIds: ReadonlySet<string>
  onForward: (ticket: KitchenTicket) => void
  onBack: (ticket: KitchenTicket) => void
}

function KitchenContent({ queue, freshIds, onForward, onBack }: KitchenContentProps) {
  if (queue.isPending) return <div className="m-4 h-40 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />
  if (queue.isError) {
    return <EmptyState className="flex-1" title="Không tải được hàng chờ" description={queue.error.message}
      action={<Button variant="secondary" onClick={() => queue.refetch()}>Tải lại</Button>} />
  }
  if (queue.data.length === 0) {
    return <EmptyState className="flex-1" title="Chưa có món nào cần pha"
      description="Món thu ngân bấm “Gửi pha chế” sẽ hiện ở đây ngay lập tức." />
  }
  return <KitchenBoard tickets={queue.data} freshIds={freshIds} onForward={onForward} onBack={onBack} />
}

interface FreshState {
  source: KitchenTicket[] | undefined
  /** null until the first load: drinks already waiting when the screen opens are not "new". */
  seen: ReadonlySet<string> | null
  fresh: ReadonlySet<string>
}

/** Line ids that arrived after the first load — highlighted once so new orders catch the eye. */
function useFreshIds(tickets: KitchenTicket[] | undefined): ReadonlySet<string> {
  const [state, setState] = useState<FreshState>({ source: undefined, seen: null, fresh: new Set() })
  // Adjusting state while rendering when the input changes (React's recommended alternative to an effect).
  if (tickets && tickets !== state.source) setState(trackArrivals(state, tickets))
  return state.fresh
}

function trackArrivals(state: FreshState, tickets: KitchenTicket[]): FreshState {
  const seen = new Set(state.seen)
  const fresh = new Set(state.fresh)
  for (const ticket of tickets) {
    if (seen.has(ticket.lineId)) continue
    seen.add(ticket.lineId)
    if (state.seen !== null) fresh.add(ticket.lineId)
  }
  return { source: tickets, seen, fresh }
}
