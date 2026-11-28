import { useEffect, useRef } from 'react'
import { Link, useSearchParams } from 'react-router'
import { usePunchByCode } from '../api/attendance'
import { PhoneShell } from '../components/PhoneShell'
import { punchMessage } from '../lib/punchMessage'

/** Opened by the phone camera from the QR code on the shop tablet: punches once, then says what was recorded. */
export function PunchPage() {
  const [params] = useSearchParams()
  const code = params.get('ma')
  const punch = usePunchByCode()
  const sent = useRef(false) // one punch per scan, also under StrictMode's double effect

  useEffect(() => {
    if (!code || sent.current) return
    sent.current = true
    punch.mutate(code)
  }, [code, punch])

  return (
    <PhoneShell>
      <h1 className="font-display text-screen font-semibold">Chấm công</h1>
      {!code && <Outcome tone="stop" title="Thiếu mã chấm công" text="Quét mã QR đang hiện trên máy ở quán bằng camera điện thoại." />}
      {punch.isPending && <div className="h-40 animate-pulse rounded-(--radius-panel) bg-surface" aria-busy />}
      {punch.isError && <Outcome tone="stop" title="Chưa chấm được" text={punch.error.message} />}
      {punch.data && (
        <Outcome tone="go" title={punch.data.action === 'CheckIn' ? 'Đã vào ca' : 'Đã ra ca'} text={punchMessage(punch.data)} />
      )}
      <Link to="/cham-cong" className="flex h-14 items-center justify-center rounded-(--radius-control) bg-ink text-lg font-semibold text-white">
        Xem chấm công của tôi
      </Link>
    </PhoneShell>
  )
}

function Outcome({ tone, title, text }: { tone: 'go' | 'stop'; title: string; text: string }) {
  return (
    <section role="status" className={`rounded-(--radius-panel) border-l-4 bg-surface p-5 ${tone === 'go' ? 'border-go' : 'border-stop'}`}>
      <p className="font-display text-block font-semibold">{tone === 'go' ? '✓ ' : ''}{title}</p>
      <p className="mt-1">{text}</p>
    </section>
  )
}
