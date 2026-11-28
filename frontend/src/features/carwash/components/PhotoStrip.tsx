import { useRef } from 'react'
import { Button } from '@/components/ui/Button'
import { toast } from '@/components/ui/toastStore'
import type { WashTicket } from '@/lib/api/types'
import { useDeletePhoto, usePhotoUrl, useUploadPhoto } from '../api/photos'

/** The server allows 8 photos per ticket. */
const MAX_PHOTOS = 8

/** Condition photos taken at intake (scratches, dents) — evidence, so they can no longer be removed once washed. */
export function PhotoStrip({ ticket }: { ticket: WashTicket }) {
  const input = useRef<HTMLInputElement>(null)
  const upload = useUploadPhoto()
  const canChange = ticket.status === 'Waiting' || ticket.status === 'Washing'
  const canAdd = ticket.status !== 'Cancelled' && ticket.photos.length < MAX_PHOTOS

  function handleFiles(files: FileList | null) {
    for (const file of Array.from(files ?? [])) {
      upload.mutate({ ticketId: ticket.id, file }, { onSuccess: () => toast.success('Đã lưu ảnh hiện trạng') })
    }
    if (input.current) input.current.value = ''
  }

  return (
    <div className="flex flex-wrap items-center gap-3">
      {ticket.photos.map((photo) => (
        <Thumbnail key={photo.id} ticketId={ticket.id} photoId={photo.id} canDelete={canChange} />
      ))}
      {canAdd && (
        <>
          <input ref={input} type="file" accept="image/*" capture="environment" multiple hidden
            onChange={(event) => handleFiles(event.target.files)} />
          <Button variant="secondary" onClick={() => input.current?.click()} busy={upload.isPending}>Chụp ảnh xe</Button>
        </>
      )}
      {ticket.photos.length === 0 && !canAdd && <p className="text-ink-muted">Không có ảnh hiện trạng.</p>}
    </div>
  )
}

function Thumbnail({ ticketId, photoId, canDelete }: { ticketId: string; photoId: string; canDelete: boolean }) {
  const url = usePhotoUrl(ticketId, photoId)
  const remove = useDeletePhoto()
  return (
    <figure className="relative size-24 overflow-hidden rounded-(--radius-control) bg-concrete">
      {url && (
        <a href={url} target="_blank" rel="noreferrer" aria-label="Xem ảnh lớn">
          <img src={url} alt="Ảnh hiện trạng xe" width={96} height={96} className="size-24 object-cover" />
        </a>
      )}
      {canDelete && (
        <button type="button" aria-label="Xóa ảnh" disabled={remove.isPending}
          onClick={() => remove.mutate({ ticketId, photoId })}
          className="absolute right-1 top-1 flex size-8 items-center justify-center rounded-full bg-ink/70 text-white">
          ×
        </button>
      )}
    </figure>
  )
}
