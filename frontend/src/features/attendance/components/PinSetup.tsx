import { useState } from 'react'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import { useHasPin, useSetPin } from '../api/attendance'
import { isValidPin } from '../lib/attendance'

/** The person sets their own PIN for the shop tablet; asks for the password so a borrowed phone cannot change it. */
export function PinSetup() {
  const hasPin = useHasPin()
  const save = useSetPin()
  const [open, setOpen] = useState(false)
  const [password, setPassword] = useState('')
  const [pin, setPin] = useState('')

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    save.mutate({ currentPassword: password, pin }, {
      onSuccess: () => { toast.success('Đã lưu mã PIN'); setOpen(false); setPassword(''); setPin('') },
    })
  }

  return (
    <section className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="font-display text-block font-semibold">Mã PIN chấm công</h2>
          <p className="text-caption text-ink-muted">
            {hasPin.data?.hasPin ? 'Đã đặt. Dùng khi điện thoại không quét được mã.' : 'Chưa đặt. Dùng khi điện thoại không quét được mã.'}
          </p>
        </div>
        {!open && <Button variant="secondary" className="shrink-0 whitespace-nowrap" onClick={() => setOpen(true)}>{hasPin.data?.hasPin ? 'Đổi PIN' : 'Đặt PIN'}</Button>}
      </div>
      {open && (
        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <TextField label="Mật khẩu đăng nhập" type="password" autoComplete="current-password" required value={password}
            onChange={(e) => setPassword(e.target.value)} />
          <TextField label="Mã PIN mới (4–6 số)" inputMode="numeric" autoComplete="off" maxLength={6} required value={pin}
            onChange={(e) => setPin(e.target.value.replace(/\D/g, ''))} />
          <FormError message={save.error?.message} />
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>Hủy</Button>
            <Button type="submit" disabled={!isValidPin(pin)} busy={save.isPending}>Lưu PIN</Button>
          </div>
        </form>
      )}
    </section>
  )
}
