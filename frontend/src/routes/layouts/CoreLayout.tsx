import { NavLink, Outlet } from 'react-router'
import type { UserIdentity } from '@/lib/api/types'
import { canManageStock, isManager } from '@/lib/auth/roles'
import { Wordmark } from '@/components/cenvo/Wordmark'
import { currentBranchName } from '@/features/auth/lib/branches'
import { useSession } from '@/lib/auth/session'

interface NavItem {
  to: string
  label: string
  end?: boolean
}

interface NavSection {
  title: string
  items: NavItem[]
  /** Hide sections the user's role cannot open (UI only). */
  visible?: (user: UserIdentity) => boolean
}

const SECTIONS: NavSection[] = [
  { title: 'Báo cáo', items: [{ to: '/bao-cao', label: 'Doanh thu' }], visible: isManager },
  { title: 'Kho', items: [{ to: '/kho/ton-kho', label: 'Tồn kho' }, { to: '/kho/phieu', label: 'Phiếu kho' }], visible: canManageStock },
  { title: 'Cà phê', items: [{ to: '/pos', label: 'Bán hàng' }, { to: '/pha-che', label: 'Pha chế' }] },
  { title: '', items: [{ to: '/', label: 'Trang chủ', end: true }] },
]

/** Desktop back-office shell: 240px sidebar with the module stripe, content aligned left. */
export function CoreLayout() {
  const user = useSession()!.user
  return (
    <div data-module="core" className="grid min-h-dvh grid-cols-[15rem_1fr] bg-concrete">
      <aside className="sticky top-0 flex h-dvh flex-col gap-6 border-l-4 border-(--module) bg-surface px-4 py-5">
        <Wordmark />
        <nav aria-label="Điều hướng" className="flex flex-1 flex-col gap-5">
          {SECTIONS.filter((section) => section.visible?.(user) ?? true).map((section) => (
            <div key={section.title || 'other'} className="flex flex-col gap-1">
              {section.title && <p className="px-3 text-caption text-ink-muted">{section.title}</p>}
              {section.items.map((item) => <SideLink key={item.to} item={item} />)}
            </div>
          ))}
        </nav>
        <div className="border-t border-line pt-4 text-sm">
          <p className="font-semibold">{user.fullName}</p>
          <p className="text-ink-muted">{currentBranchName(user)}</p>
        </div>
      </aside>
      <main className="min-w-0 px-8 py-6">
        <Outlet />
      </main>
    </div>
  )
}

function SideLink({ item }: { item: NavItem }) {
  return (
    <NavLink to={item.to} end={item.end}
      className={({ isActive }) => `flex h-11 items-center rounded-(--radius-control) px-3 font-medium
        ${isActive ? 'bg-ink text-white' : 'text-ink hover:bg-concrete'}`}>
      {item.label}
    </NavLink>
  )
}
