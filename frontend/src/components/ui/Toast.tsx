import { useToasts, type Tone } from './toastStore'

const tones: Record<Tone, string> = {
  success: 'border-l-go',
  error: 'border-l-stop',
}

/** Render once per layout. Errors are announced assertively, confirmations politely. */
export function Toaster() {
  const list = useToasts()
  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-6 z-50 flex flex-col items-center gap-2 px-4">
      {list.map((item) => (
        <div key={item.id} role={item.tone === 'error' ? 'alert' : 'status'}
          className={`max-w-lg rounded-(--radius-panel) border-l-4 bg-ink px-4 py-3 text-base font-medium text-white shadow-overlay ${tones[item.tone]}`}>
          {item.message}
        </div>
      ))}
    </div>
  )
}
