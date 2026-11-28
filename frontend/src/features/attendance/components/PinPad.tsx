import { Button } from '@/components/ui/Button'

const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9']

/** Big on-screen digits for the shop tablet; the PIN itself is shown as dots only. */
export function PinPad({ value, onChange, maxLength = 6 }: { value: string; onChange: (value: string) => void; maxLength?: number }) {
  const press = (digit: string) => value.length < maxLength && onChange(value + digit)
  return (
    <div className="flex flex-col gap-4">
      <p aria-live="polite" aria-label={`Đã nhập ${value.length} số`} className="flex h-10 items-center justify-center gap-3">
        {Array.from({ length: maxLength }, (_, i) => (
          <span key={i} aria-hidden className={`size-4 rounded-full ${i < value.length ? 'bg-ink' : 'border-2 border-line'}`} />
        ))}
      </p>
      <div className="grid grid-cols-3 gap-3">
        {KEYS.map((key) => <Digit key={key} digit={key} onPress={press} />)}
        <Button type="button" variant="ghost" size="lg" aria-label="Xóa hết" onClick={() => onChange('')}>Xóa</Button>
        <Digit digit="0" onPress={press} />
        <Button type="button" variant="ghost" size="lg" aria-label="Xóa một số" onClick={() => onChange(value.slice(0, -1))}>⌫</Button>
      </div>
    </div>
  )
}

function Digit({ digit, onPress }: { digit: string; onPress: (digit: string) => void }) {
  return (
    <button type="button" onClick={() => onPress(digit)}
      className="h-16 rounded-(--radius-control) border border-line bg-surface font-display text-2xl font-semibold tabular-nums active:bg-line/60">
      {digit}
    </button>
  )
}
