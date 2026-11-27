import type { ReactNode } from 'react'
import { Wordmark } from '@/components/cenvo/Wordmark'

/** Login frame: brand panel on the left (≥1024px), form on the right. Phones show only the form. */
export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-dvh bg-concrete lg:grid-cols-[5fr_7fr]">
      <BrandPanel />
      <main className="flex flex-col px-6 py-10 sm:px-10">
        <div className="lg:hidden">
          <Wordmark />
        </div>
        <div className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center py-10">{children}</div>
        <p className="text-center text-caption text-ink-muted">Cần hỗ trợ tài khoản? Liên hệ quản lý chi nhánh.</p>
      </main>
    </div>
  )
}

function BrandPanel() {
  return (
    <aside className="hidden flex-col justify-between bg-ink p-12 text-white lg:flex">
      <Wordmark tone="light" size="lg" />
      <div className="max-w-md">
        <p className="font-display text-[2rem] font-semibold leading-tight">
          Quầy pha chế và khoang rửa xe, chung một sổ.
        </p>
        <p className="mt-4 text-white/70">
          Bán một ly, kho trừ nguyên liệu theo công thức. Rửa một xe, kho trừ hóa chất và nhân viên có hoa hồng.
        </p>
      </div>
      <dl className="grid grid-cols-2 gap-6 text-sm">
        <ModuleNote stripe="bg-robusta-bright" title="Cà phê" text="POS, màn pha chế, ca thu ngân" />
        <ModuleNote stripe="bg-foam-bright" title="Rửa xe" text="Tiếp nhận, hàng chờ, hoa hồng" />
      </dl>
    </aside>
  )
}

function ModuleNote({ stripe, title, text }: { stripe: string; title: string; text: string }) {
  return (
    <div className="flex gap-3">
      <span aria-hidden className={`w-1 shrink-0 rounded-full ${stripe}`} />
      <div>
        <dt className="font-display text-base font-semibold">{title}</dt>
        <dd className="text-white/65">{text}</dd>
      </div>
    </div>
  )
}
