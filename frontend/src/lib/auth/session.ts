import { useSyncExternalStore } from 'react'
import type { TokenResponse, UserIdentity } from '@/lib/api/types'

/**
 * Login session. The access token lives in memory only (never exposed via localStorage);
 * the refresh token + user info live in localStorage so a page reload doesn't require signing in again.
 */
export interface Session {
  user: UserIdentity
  refreshToken: string
}

const STORAGE_KEY = 'cenvo.session'

let accessToken: string | null = null
let session: Session | null = readStored()
const listeners = new Set<() => void>()

function readStored(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as Session) : null
  } catch {
    return null
  }
}

function emit() {
  listeners.forEach((listener) => listener())
}

export const sessionStore = {
  get: () => session,
  accessToken: () => accessToken,

  save(token: TokenResponse) {
    accessToken = token.accessToken
    session = { user: token.user, refreshToken: token.refreshToken }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    emit()
  },

  clear() {
    accessToken = null
    session = null
    localStorage.removeItem(STORAGE_KEY)
    emit()
  },

  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => listeners.delete(listener)
  },
}

export function useSession(): Session | null {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.get)
}
