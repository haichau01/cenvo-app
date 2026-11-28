import type { ReactNode } from 'react'
import { flushSync } from 'react-dom'
import { createRoot } from 'react-dom/client'

/** Thermal receipt paper widths. 80mm is the common counter printer; 58mm the small/portable one. */
export type PaperWidth = '80mm' | '58mm'

const PAPER_KEY = 'cenvo.printer.paper'
const HOST_ID = 'print-root'

export function paperWidth(): PaperWidth {
  return localStorage.getItem(PAPER_KEY) === '58mm' ? '58mm' : '80mm'
}

export function setPaperWidth(width: PaperWidth) {
  localStorage.setItem(PAPER_KEY, width)
}

/**
 * Prints `content` alone on receipt paper with the browser print dialog (or silently with kiosk printing).
 * Rendered into a temporary element of this page — not an iframe of the app: a second app instance would
 * rotate the refresh token and break this window's session.
 */
export async function printContent(content: ReactNode, width: PaperWidth = paperWidth()): Promise<void> {
  document.getElementById(HOST_ID)?.remove() // leftover from a print whose afterprint never fired

  // Rendered off-screen (not display:none) so its real height can be measured before printing.
  const host = document.createElement('div')
  host.id = HOST_ID
  host.style.cssText = `position: fixed; left: -10000px; top: 0; width: ${width};`
  document.body.append(host)
  const root = createRoot(host)
  flushSync(() => root.render(content))
  await document.fonts.ready // Barlow must be loaded or the first receipt prints in a fallback font

  // "size: 80mm auto" is invalid CSS and browsers drop it — give the roll its exact length instead.
  const style = document.createElement('style')
  style.textContent = pageCss(width, Math.ceil(host.getBoundingClientRect().height / PX_PER_MM))
  document.head.append(style)

  const cleanup = () => {
    root.unmount()
    host.remove()
    style.remove()
  }
  window.addEventListener('afterprint', cleanup, { once: true })
  window.print()
}

const PX_PER_MM = 96 / 25.4

export function pageCss(width: PaperWidth, heightMm: number): string {
  return `
    @page { size: ${width} ${heightMm}mm; margin: 0; }
    @media print {
      html, body { background: #fff !important; }
      body > *:not(#${HOST_ID}) { display: none !important; }
      #${HOST_ID} { position: static !important; }
    }`
}
