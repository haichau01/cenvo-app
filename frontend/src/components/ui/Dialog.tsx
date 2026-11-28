import { useEffect, useId, useRef, type ReactNode } from 'react'

interface DialogProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Action row at the bottom (buttons). */
  footer?: ReactNode
  className?: string
}

/** Modal built on the native <dialog>: focus trap, Esc to close and a backdrop for free. */
export function Dialog({ open, onClose, title, children, footer, className = '' }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(event) => event.target === ref.current && onClose()}
      aria-labelledby={titleId}
      className={`m-auto w-[min(32rem,calc(100vw-2rem))] rounded-(--radius-sheet) bg-surface p-0 text-ink
        shadow-overlay backdrop:bg-ink/40 ${className}`}
    >
      {open && (
        <div className="flex max-h-[calc(100dvh-4rem)] flex-col">
          <header className="flex items-center justify-between gap-4 border-b border-line px-6 py-4">
            <h2 id={titleId} className="font-display text-block font-semibold">{title}</h2>
            <button type="button" onClick={onClose} aria-label="Đóng"
              className="-mr-3 flex size-12 items-center justify-center rounded-(--radius-control) text-2xl text-ink-muted hover:bg-ink/5">
              ×
            </button>
          </header>
          <div className="overflow-y-auto px-6 py-5">{children}</div>
          {footer && <footer className="flex justify-end gap-3 border-t border-line px-6 py-4">{footer}</footer>}
        </div>
      )}
    </dialog>
  )
}
