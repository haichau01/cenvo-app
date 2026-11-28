import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useSyncExternalStore } from 'react'
import { getAccessToken } from '@/lib/api/client'

/** SignalR hubs of the API; each joins the user's branch group on the server. */
export const Hub = {
  kitchen: '/hubs/kitchen',
  carWash: '/hubs/carwash',
} as const
export type HubPath = (typeof Hub)[keyof typeof Hub]

/**
 * One SignalR connection per hub, shared by the whole app. The server only sends "something changed" signals
 * (e.g. `ordersChanged`, `ticketsChanged`); screens react by invalidating their queries.
 */
const connections = new Map<HubPath, HubConnection>()
const disconnected = new Set<HubPath>()
let connected = true
const statusListeners = new Set<() => void>()
const reconnectHandlers = new Map<HubPath, Set<() => void>>()

function setHubConnected(path: HubPath, value: boolean) {
  if (value) disconnected.delete(path)
  else disconnected.add(path)
  connected = disconnected.size === 0
  statusListeners.forEach((listener) => listener())
}

function ensureConnection(path: HubPath): HubConnection {
  const existing = connections.get(path)
  if (existing) return existing
  const hub = new HubConnectionBuilder()
    .withUrl(path, { accessTokenFactory: getAccessToken })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
    .configureLogging(LogLevel.Warning)
    .build()
  const isCurrent = () => connections.get(path) === hub
  hub.onreconnecting(() => setHubConnected(path, false))
  hub.onreconnected(() => {
    setHubConnected(path, true)
    reconnectHandlers.get(path)?.forEach((handler) => handler())
  })
  // A connection stopped by stopRealtime() is no longer current and must not flip the status.
  hub.onclose(() => isCurrent() && setHubConnected(path, false))
  connections.set(path, hub)
  void start(path, hub, isCurrent)
  return hub
}

async function start(path: HubPath, hub: HubConnection, isCurrent: () => boolean) {
  try {
    await hub.start()
    setHubConnected(path, true)
  } catch {
    setHubConnected(path, false)
    setTimeout(() => isCurrent() && hub.state === HubConnectionState.Disconnected && void start(path, hub, isCurrent), 5_000)
  }
}

/** Stop every connection — call on logout / branch switch so the next connections join the right branch group. */
export async function stopRealtime() {
  const hubs = [...connections.values()]
  connections.clear()
  disconnected.clear()
  connected = true
  statusListeners.forEach((listener) => listener())
  await Promise.all(hubs.map((hub) => hub.stop()))
}

/** Runs `handler` whenever the hub sends `event`, and once after reconnecting (signals may have been missed). */
export function useRealtimeEvent(event: string, handler: () => void, path: HubPath = Hub.kitchen) {
  const handlerRef = useRef(handler)
  useEffect(() => {
    handlerRef.current = handler
  })

  useEffect(() => {
    const hub = ensureConnection(path)
    const listener = () => handlerRef.current()
    hub.on(event, listener)
    const handlers = reconnectHandlers.get(path) ?? new Set()
    reconnectHandlers.set(path, handlers.add(listener))
    return () => {
      hub.off(event, listener)
      handlers.delete(listener)
    }
  }, [event, path])
}

export function useRealtimeConnected(): boolean {
  return useSyncExternalStore(
    (listener) => {
      statusListeners.add(listener)
      return () => statusListeners.delete(listener)
    },
    () => connected,
  )
}
