import { useSyncExternalStore } from 'react'

export type Tone = 'success' | 'error'

export interface ToastItem {
  id: number
  tone: Tone
  message: string
}

const DURATION_MS = { success: 3_000, error: 6_000 } satisfies Record<Tone, number>

let items: ToastItem[] = []
let nextId = 1
const listeners = new Set<() => void>()

function emit() {
  listeners.forEach((listener) => listener())
}

function push(tone: Tone, message: string) {
  const id = nextId++
  items = [...items, { id, tone, message }]
  emit()
  setTimeout(() => {
    items = items.filter((item) => item.id !== id)
    emit()
  }, DURATION_MS[tone])
}

export const toast = {
  success: (message: string) => push('success', message),
  error: (message: string) => push('error', message),
}

/** Current toasts — re-renders when one is added or expires. */
export function useToasts(): ToastItem[] {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    () => items,
  )
}
