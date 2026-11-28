import { MutationCache, QueryClient } from '@tanstack/react-query'
import { toast } from '@/components/ui/toastStore'
import { ApiError } from '@/lib/api/client'

const CONFLICT = 'Dữ liệu vừa được máy khác cập nhật. Đã tải lại, kiểm tra rồi thử lại.'

declare module '@tanstack/react-query' {
  interface Register {
    /** `inlineError: true` = the form shows the error itself, don't toast it. */
    mutationMeta: { inlineError?: boolean }
  }
}

export const queryClient: QueryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false },
  },
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) => {
      if (mutation.meta?.inlineError) return
      // 409 = another device changed the same record → reload everything on screen.
      if (error instanceof ApiError && error.status === 409) {
        toast.error(CONFLICT)
        void queryClient.invalidateQueries()
        return
      }
      toast.error(error.message)
    },
  }),
})
