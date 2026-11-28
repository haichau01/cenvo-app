import { Outlet } from 'react-router'
import { OfflineBanner, TouchRail, type RailItem } from './TouchRail'

const NAV: RailItem[] = [
  { to: '/rua-xe', label: 'Hàng chờ', glyph: '≡' },
  { to: '/rua-xe/tiep-nhan', label: 'Tiếp nhận', glyph: '+' },
  { to: '/rua-xe/khach-hang', label: 'Khách hàng', glyph: '☺' },
  { to: '/', label: 'Trang chủ', glyph: '⌂' },
]

/** Car wash module shell: foam stripe + touch rail (tablet at the bay or wall screen). */
export function WashLayout() {
  return (
    <div data-module="wash" className="grid h-dvh grid-cols-[5rem_1fr] overflow-hidden bg-concrete">
      <TouchRail label="Phân hệ Rửa xe" items={NAV} activeClass="bg-foam-bright/30" />
      <div className="flex min-h-0 min-w-0 flex-col">
        <OfflineBanner />
        <Outlet />
      </div>
    </div>
  )
}
