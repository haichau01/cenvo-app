import { sessionStore } from '@/lib/auth/session'
import type { TokenResponse } from './types'

/** API error. `message` is the backend's Vietnamese sentence — show it to the user verbatim. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

const NETWORK_ERROR = 'Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại.'
const SESSION_EXPIRED = 'Phiên đăng nhập đã hết hạn. Đăng nhập lại để tiếp tục.'

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

interface RequestOptions {
  method?: Method
  body?: unknown
  /** Do not attach a token / do not auto-refresh (used for login and refresh). */
  anonymous?: boolean
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  if (options.anonymous) return parse<T>(await send(path, options, null))

  let token = sessionStore.accessToken() ?? (await refreshAccessToken())
  let response = await send(path, options, token)
  if (response.status === 401) {
    token = await refreshAccessToken()
    response = await send(path, options, token)
  }
  return parse<T>(response)
}

async function send(path: string, { method = 'GET', body }: RequestOptions, token: string | null) {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`
  try {
    return await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) })
  } catch {
    throw new ApiError(0, NETWORK_ERROR)
  }
}

async function parse<T>(response: Response): Promise<T> {
  const text = await response.text()
  const data: unknown = text ? JSON.parse(text) : undefined
  if (response.ok) return data as T
  throw new ApiError(response.status, errorMessage(response.status, data))
}

/** The backend returns ProblemDetails `{ title }` with a Vietnamese sentence. */
function errorMessage(status: number, data: unknown): string {
  const title = (data as { title?: unknown } | undefined)?.title
  if (typeof title === 'string' && title) return title
  if (status === 403) return 'Tài khoản của bạn không có quyền làm việc này.'
  return 'Lỗi hệ thống, vui lòng thử lại.'
}

/** Current access token, refreshing it first if needed (used by SignalR). */
export function getAccessToken(): Promise<string> {
  return Promise.resolve(sessionStore.accessToken() ?? refreshAccessToken())
}

// Concurrent 401s refresh only once (refresh tokens rotate — refreshing twice would break the session).
let refreshing: Promise<string> | null = null

function refreshAccessToken(): Promise<string> {
  refreshing ??= doRefresh().finally(() => {
    refreshing = null
  })
  return refreshing
}

async function doRefresh(): Promise<string> {
  const refreshToken = sessionStore.get()?.refreshToken
  if (!refreshToken) throw new ApiError(401, SESSION_EXPIRED)
  try {
    const token = await apiFetch<TokenResponse>('/api/auth/refresh', {
      method: 'POST', body: { refreshToken }, anonymous: true,
    })
    sessionStore.save(token)
    return token.accessToken
  } catch (error) {
    if (error instanceof ApiError && error.status !== 0) sessionStore.clear()
    throw error
  }
}

/**
 * Call on app start: if a session is stored, exchange the refresh token for a new access token.
 * Expired session → clear it (back to login). Network error → keep it and retry on the next request.
 */
export async function restoreSession(): Promise<void> {
  if (!sessionStore.get()) return
  await refreshAccessToken().catch(() => {})
}
