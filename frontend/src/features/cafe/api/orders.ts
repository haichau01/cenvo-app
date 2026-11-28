import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api/client'
import type { AddLineInput, CreateOrderInput, MenuItem, OrderDto, TenderedPayment } from '@/lib/api/types'
import { cafeKeys } from './keys'

export const openOrdersQuery = {
  queryKey: cafeKeys.openOrders,
  queryFn: () => apiFetch<OrderDto[]>('/api/cafe/orders/open'),
  staleTime: 0,
}

export const useOpenOrders = () => useQuery(openOrdersQuery)

export function useOrder(id: string | null) {
  return useQuery({
    queryKey: cafeKeys.order(id ?? ''),
    queryFn: () => apiFetch<OrderDto>(`/api/cafe/orders/${id}`),
    enabled: id !== null,
    staleTime: 0,
  })
}

/**
 * All bill edits run one after another (same scope): the order row has an optimistic-concurrency version,
 * so two parallel requests on one bill would fail with 409 and lose a tapped item.
 */
const ORDER_EDIT_KEY = ['cafe', 'order-edit'] as const
const orderEdit = { mutationKey: ORDER_EDIT_KEY, scope: { id: 'cafe-order-edit' } }

/**
 * Every order mutation returns the full OrderDto: store it, then refresh the lists around it.
 * While more edits are queued, skip it — an intermediate result would hide their optimistic lines.
 */
function applyOrder(queryClient: QueryClient, order: OrderDto) {
  if (queryClient.isMutating({ mutationKey: ORDER_EDIT_KEY }) > 1) return
  queryClient.setQueryData(cafeKeys.order(order.id), order)
  void queryClient.invalidateQueries({ queryKey: cafeKeys.openOrders })
  void queryClient.invalidateQueries({ queryKey: cafeKeys.tables })
}

/** `inlineError`: the mutation runs inside a dialog that shows the error itself (toasts sit under the dialog). */
function useOrderMutation<TVariables>(request: (variables: TVariables) => Promise<OrderDto>, inlineError = false) {
  const queryClient = useQueryClient()
  return useMutation({
    ...orderEdit,
    mutationFn: request,
    onSuccess: (order) => applyOrder(queryClient, order),
    meta: { inlineError },
  })
}

export const useCreateOrder = () =>
  useOrderMutation((input: CreateOrderInput) => apiFetch<OrderDto>('/api/cafe/orders', { method: 'POST', body: input }))

interface AddLineVariables {
  orderId: string
  item: MenuItem
  input: AddLineInput
}

/** Adding an item is optimistic: the line shows up instantly (even while queued) and is replaced by the server's numbers. */
export function useAddLine() {
  const queryClient = useQueryClient()
  return useMutation({
    ...orderEdit,
    mutationFn: ({ orderId, input }: AddLineVariables) =>
      apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/lines`, { method: 'POST', body: input }),
    onMutate: async ({ orderId, item, input }) => {
      const key = cafeKeys.order(orderId)
      await queryClient.cancelQueries({ queryKey: key })
      queryClient.setQueryData<OrderDto>(key, (current) => current && withPendingLine(current, item, input.quantity))
    },
    // Other queued edits may have changed the bill meanwhile: reload the truth instead of restoring a snapshot.
    onError: (_error, { orderId }) => queryClient.invalidateQueries({ queryKey: cafeKeys.order(orderId) }),
    onSuccess: (order) => applyOrder(queryClient, order),
  })
}

export const PENDING_LINE_PREFIX = 'pending-'

function withPendingLine(order: OrderDto, item: MenuItem, quantity: number): OrderDto {
  const amount = item.price * quantity
  const line = {
    id: `${PENDING_LINE_PREFIX}${crypto.randomUUID()}`, productId: item.productId, name: item.name,
    unitPrice: item.price, quantity, discountAmount: 0, amount, note: null, kitchenStatus: 'New' as const,
    sentAt: null, toppings: [],
  }
  return { ...order, lines: [...order.lines, line], subtotal: order.subtotal + amount, total: order.total + amount }
}

interface LineVariables {
  orderId: string
  lineId: string
}

export const useUpdateLineQuantity = () =>
  useOrderMutation(({ orderId, lineId, quantity, note, discountAmount }: LineVariables & {
    quantity: number; note?: string | null; discountAmount: number
  }) => apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/lines/${lineId}`, {
    method: 'PUT', body: { quantity, note, discountAmount },
  }))

export const useRemoveLine = () =>
  useOrderMutation(({ orderId, lineId }: LineVariables) =>
    apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/lines/${lineId}`, { method: 'DELETE' }))

export const useSendToKitchen = () =>
  useOrderMutation((orderId: string) => apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/send`, { method: 'POST' }))

export const usePayOrder = () =>
  useOrderMutation(({ orderId, payments }: { orderId: string; payments: TenderedPayment[] }) =>
    apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/pay`, { method: 'POST', body: { payments } }), true)

export const useCancelOrder = () =>
  useOrderMutation(({ orderId, reason }: { orderId: string; reason: string }) =>
    apiFetch<OrderDto>(`/api/cafe/orders/${orderId}/cancel`, { method: 'POST', body: { reason } }), true)
