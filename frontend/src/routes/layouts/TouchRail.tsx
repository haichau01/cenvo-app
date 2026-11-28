import { NavLink } from 'react-router'
import { useSession } from '@/lib/auth/session'
import { useRealtimeConnected } from '@/lib/realtime'

export interface RailItem {
  to: string
  label: string
  glyph: string
}

/** Narrow touch rail on the left of tablet screens, with the module stripe (colour comes from data-module). */
/** `activeClass`: background of the current screen, in the module's bright colour. */
export function TouchRail({ label, items, activeClass }: { label: string; items: RailItem[]; activeClass: string }) {
  const user = useSession()?.user
  return (
    <nav aria-label={label} className="flex flex-col items-center gap-2 border-l-4 border-(--module) bg-ink py-4 text-white">
      {items.map((item) => (
        <NavLink key={item.to} to={item.to} end
          className={({ isActive }) => `flex h-16 w-16 flex-col items-center justify-center gap-0.5 rounded-(--radius-panel)
            text-center text-caption ${isActive ? `${activeClass} text-white` : 'text-white/70 active:bg-white/10'}`}>
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

export function OfflineBanner() {
  const connected = useRealtimeConnected()
  if (connected) return null
  return (
    <div role="status" className="bg-lane px-4 py-2 text-center text-sm font-medium text-ink">
      Mất kết nối — dữ liệu có thể chưa mới. Đang thử kết nối lại…
    </div>
  )
}
