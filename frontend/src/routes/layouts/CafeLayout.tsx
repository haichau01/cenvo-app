import { NavLink, Outlet } from 'react-router'
import { useSession } from '@/lib/auth/session'
import { useRealtimeConnected } from '@/lib/realtime'

const NAV = [
  { to: '/pos', label: 'Bán hàng', glyph: '☕' },
  { to: '/', label: 'Trang chủ', glyph: '⌂' },
]

/** Cafe module shell: robusta stripe + narrow touch rail on the left (tablet landscape). */
export function CafeLayout() {
  return (
    <div data-module="cafe" className="grid h-dvh grid-cols-[5rem_1fr] overflow-hidden bg-concrete">
      <CafeRail />
      <div className="flex min-h-0 min-w-0 flex-col">
        <OfflineBanner />
        <Outlet />
      </div>
    </div>
  )
}

function CafeRail() {
  const user = useSession()?.user
  return (
    <nav aria-label="Phân hệ Cà phê" className="flex flex-col items-center gap-2 border-l-4 border-(--module) bg-ink py-4 text-white">
      {NAV.map((item) => (
        <NavLink key={item.to} to={item.to} end
          className={({ isActive }) => `flex h-16 w-16 flex-col items-center justify-center gap-0.5 rounded-(--radius-panel) text-caption
            ${isActive ? 'bg-robusta-bright/30 text-white' : 'text-white/70 active:bg-white/10'}`}>
          <span aria-hidden className="text-xl leading-none">{item.glyph}</span>
          {item.label}
        </NavLink>
      ))}
      <span title={user?.fullName} className="mt-auto flex size-10 items-center justify-center rounded-full bg-white/15 font-semibold">
        {user?.fullName.trim().split(/\s+/).at(-1)?.[0] ?? '?'}
      </span>
    </nav>
  )
}

function OfflineBanner() {
  const connected = useRealtimeConnected()
  if (connected) return null
  return (
    <div role="status" className="bg-lane px-4 py-2 text-center text-sm font-medium text-ink">
      Mất kết nối — dữ liệu có thể chưa mới. Đang thử kết nối lại…
    </div>
  )
}
