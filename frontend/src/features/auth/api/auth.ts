import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ApiError, apiFetch } from '@/lib/api/client'
import type { LoginRequest, TokenResponse } from '@/lib/api/types'
import { sessionStore } from '@/lib/auth/session'
import { stopRealtime } from '@/lib/realtime'
import { hasNoBranch } from '../lib/branches'

const NO_BRANCH = 'Tài khoản chưa được gán chi nhánh nào. Nhờ quản lý gán chi nhánh rồi đăng nhập lại.'

export function useLogin() {
  return useMutation({
    mutationFn: async (input: LoginRequest) => {
      const token = await apiFetch<TokenResponse>('/api/auth/login', { method: 'POST', body: input, anonymous: true })
      // Check before saving the session: without a branch the user cannot use any screen.
      if (hasNoBranch(token.user)) {
        await revoke(token.refreshToken)
        throw new ApiError(403, NO_BRANCH)
      }
      return token
    },
    onSuccess: (token) => sessionStore.save(token),
    meta: { inlineError: true },
  })
}

export function useSwitchBranch() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (branchId: string) =>
      apiFetch<TokenResponse>('/api/auth/switch-branch', { method: 'POST', body: { branchId } }),
    onSuccess: (token) => {
      sessionStore.save(token)
      void stopRealtime() // reconnect with the new token → joins the new branch group
      // Cached data belongs to the previous branch — drop all of it.
      queryClient.clear()
    },
    meta: { inlineError: true },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const refreshToken = sessionStore.get()?.refreshToken
      if (refreshToken) await revoke(refreshToken)
    },
    onSettled: () => {
      void stopRealtime()
      sessionStore.clear()
      queryClient.clear()
    },
  })
}

/** Server-side revocation is best effort: logging out on this device still works offline. */
function revoke(refreshToken: string) {
  return apiFetch('/api/auth/logout', { method: 'POST', body: { refreshToken }, anonymous: true }).catch(() => {})
}
