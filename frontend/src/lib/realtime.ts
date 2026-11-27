import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useSyncExternalStore } from 'react'
import { getAccessToken } from '@/lib/api/client'

/**
 * One SignalR connection for the whole app. The server only sends "something changed" signals
 * (e.g. `ordersChanged`); screens react by invalidating their queries.
 */
let connection: HubConnection | null = null
let connected = true
const statusListeners = new Set<() => void>()
const reconnectHandlers = new Set<() => void>()

function setConnected(value: boolean) {
  connected = value
  statusListeners.forEach((listener) => listener())
}

function ensureConnection(): HubConnection {
  if (connection) return connection
  const hub = new HubConnectionBuilder()
    .withUrl('/hubs/kitchen', { accessTokenFactory: getAccessToken })
    .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
    .configureLogging(LogLevel.Warning)
    .build()
  hub.onreconnecting(() => setConnected(false))
  hub.onreconnected(() => {
    setConnected(true)
    reconnectHandlers.forEach((handler) => handler())
  })
  // A connection stopped by stopRealtime() is no longer current and must not flip the status.
  hub.onclose(() => hub === connection && setConnected(false))
  connection = hub
  void start(hub)
  return hub
}

async function start(hub: HubConnection) {
  try {
    await hub.start()
    setConnected(true)
  } catch {
    setConnected(false)
    setTimeout(() => hub === connection && hub.state === HubConnectionState.Disconnected && void start(hub), 5_000)
  }
}

/** Stop the connection — call on logout / branch switch so the next connection joins the right branch group. */
export async function stopRealtime() {
  const hub = connection
  connection = null
  setConnected(true)
  await hub?.stop()
}

/** Runs `handler` whenever the server sends `event`, and once after reconnecting (signals may have been missed). */
export function useRealtimeEvent(event: string, handler: () => void) {
  const handlerRef = useRef(handler)
  useEffect(() => {
    handlerRef.current = handler
  })

  useEffect(() => {
    const hub = ensureConnection()
    const listener = () => handlerRef.current()
    hub.on(event, listener)
    reconnectHandlers.add(listener)
    return () => {
      hub.off(event, listener)
      reconnectHandlers.delete(listener)
    }
  }, [event])
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
