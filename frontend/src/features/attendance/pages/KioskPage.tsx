import { useState } from 'react'
import { Link } from 'react-router'
import { Wordmark } from '@/components/cenvo/Wordmark'
import { FormError } from '@/components/ui/FormError'
import { currentBranchName } from '@/features/auth/lib/branches'
import type { StaffMember } from '@/lib/api/types'
import { useSession } from '@/lib/auth/session'
import { useKioskCode, useKioskStaff } from '../api/attendance'
import { PinPunchDialog } from '../components/PinPunchDialog'
import { QrCode } from '../components/QrCode'
import { punchUrl } from '../lib/attendance'

/**
 * Left on the shop tablet: a QR code that changes every 30 seconds (staff scan it with their own phone camera),
 * and their names for the PIN fallback.
 */
export function KioskPage() {
  const user = useSession()!.user
  const code = useKioskCode()
  const staff = useKioskStaff()
  const [picked, setPicked] = useState<StaffMember | null>(null)

  return (
    <div className="grid min-h-dvh grid-cols-[1fr_24rem] bg-concrete">
      <main className="flex flex-col items-center justify-center gap-6 p-8">
        <div className="text-center">
          <h1 className="font-display text-screen font-semibold">Chấm công · {currentBranchName(user)}</h1>
          <p className="text-ink-muted">Mở camera điện thoại, quét mã để vào ca hoặc ra ca.</p>
        </div>
        <div className="rounded-(--radius-panel) bg-white p-4">
          {code.data
            ? <QrCode value={punchUrl(window.location.origin, code.data.code)} label="Mã chấm công" className="size-[22rem]" />
            : <div className="size-[22rem] animate-pulse bg-concrete" aria-busy />}
        </div>
        <p className="text-caption text-ink-muted">Mã tự đổi mỗi 30 giây — ảnh chụp mã không dùng được ở nơi khác.</p>
        <FormError message={code.error?.message} />
      </main>
      <aside className="flex flex-col gap-4 border-l border-line bg-surface p-6">
        <div className="flex items-center justify-between">
          <Wordmark />
          <Link to="/" className="flex h-11 items-center rounded-(--radius-control) px-3 font-medium hover:bg-ink/5">Thoát</Link>
        </div>
        <div>
          <h2 className="font-display text-block font-semibold">Không quét được?</h2>
          <p className="text-ink-muted">Chọn tên bạn rồi nhập mã PIN.</p>
        </div>
        <FormError message={staff.error?.message} />
        <ul className="-mx-2 flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto px-2">
          {staff.data?.map((person) => (
            <li key={person.userId}>
              <button type="button" onClick={() => setPicked(person)}
                className="flex h-14 w-full items-center rounded-(--radius-control) px-4 text-left text-lg font-medium hover:bg-concrete active:bg-line/60">
                {person.fullName}
              </button>
            </li>
          ))}
        </ul>
      </aside>
      {picked && <PinPunchDialog staff={picked} onClose={() => setPicked(null)} />}
    </div>
  )
}
