import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo } from 'react'
import { apiBlob, apiFetch } from '@/lib/api/client'
import { washKeys } from './keys'
import { shrinkPhoto } from '../lib/photo'

/** Photo bytes need the access token, so they are fetched as a blob and shown through an object URL. */
export function usePhotoUrl(ticketId: string, photoId: string): string | null {
  const blob = useQuery({
    queryKey: washKeys.photo(ticketId, photoId),
    queryFn: () => apiBlob(`/api/carwash/tickets/${ticketId}/photos/${photoId}`),
    staleTime: Infinity, // a photo never changes
  }).data
  const url = useMemo(() => (blob ? URL.createObjectURL(blob) : null), [blob])
  // Free the object URL when the photo leaves the screen (or a new blob replaces it).
  useEffect(() => () => {
    if (url) URL.revokeObjectURL(url)
  }, [url])
  return url
}

function useRefreshTicket() {
  const queryClient = useQueryClient()
  return (ticketId: string) => {
    void queryClient.invalidateQueries({ queryKey: washKeys.ticket(ticketId) })
    void queryClient.invalidateQueries({ queryKey: washKeys.queue })
  }
}

/** Phone/tablet photos are several MB: shrink them on the device first (faster upload, under the 5 MB limit). */
export function useUploadPhoto() {
  const refresh = useRefreshTicket()
  return useMutation({
    scope: { id: 'carwash-ticket-edit' },
    mutationFn: async ({ ticketId, file }: { ticketId: string; file: File }) => {
      const form = new FormData()
      form.append('file', await shrinkPhoto(file), 'hien-trang.jpg')
      return apiFetch<{ id: string }>(`/api/carwash/tickets/${ticketId}/photos`, { method: 'POST', body: form })
    },
    onSuccess: (_photo, { ticketId }) => refresh(ticketId),
  })
}

export function useDeletePhoto() {
  const refresh = useRefreshTicket()
  return useMutation({
    scope: { id: 'carwash-ticket-edit' },
    mutationFn: ({ ticketId, photoId }: { ticketId: string; photoId: string }) =>
      apiFetch<void>(`/api/carwash/tickets/${ticketId}/photos/${photoId}`, { method: 'DELETE' }),
    onSuccess: (_result, { ticketId }) => refresh(ticketId),
  })
}
