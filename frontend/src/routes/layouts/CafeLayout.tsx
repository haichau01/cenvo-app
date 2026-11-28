import { Outlet } from 'react-router'
import { OfflineBanner, TouchRail, type RailItem } from './TouchRail'

const NAV: RailItem[] = [
  { to: '/pos', label: 'Bán hàng', glyph: '☕' },
  { to: '/pha-che', label: 'Pha chế', glyph: '⏲' },
  { to: '/', label: 'Trang chủ', glyph: '⌂' },
]

/** Cafe module shell: robusta stripe + narrow touch rail on the left (tablet landscape). */
export function CafeLayout() {
  return (
    <div data-module="cafe" className="grid h-dvh grid-cols-[5rem_1fr] overflow-hidden bg-concrete">
      <TouchRail label="Phân hệ Cà phê" items={NAV} activeClass="bg-robusta-bright/30" />
      <div className="flex min-h-0 min-w-0 flex-col">
        <OfflineBanner />
        <Outlet />
      </div>
    </div>
  )
}
