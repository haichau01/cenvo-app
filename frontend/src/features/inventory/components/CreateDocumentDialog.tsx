import { Link } from 'react-router'
import { Dialog } from '@/components/ui/Dialog'
import { CREATABLE_TYPES, DOCUMENT_TYPES } from '../lib/documentTypes'

/** Pick what kind of document to create — each option says what it does. */
export function CreateDocumentDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog open={open} onClose={onClose} title="Tạo phiếu kho" className="w-[min(40rem,calc(100vw-2rem))]">
      <ul className="flex flex-col gap-2">
        {CREATABLE_TYPES.map((type) => {
          const config = DOCUMENT_TYPES[type]
          return (
            <li key={type}>
              <Link to={`/kho/phieu/moi?loai=${type}`} onClick={onClose}
                className="flex flex-col gap-0.5 rounded-(--radius-panel) border border-line px-4 py-3 hover:bg-concrete">
                <span className="font-semibold">{config.title}</span>
                <span className="text-sm text-ink-muted">{config.hint}</span>
              </Link>
            </li>
          )
        })}
      </ul>
    </Dialog>
  )
}
