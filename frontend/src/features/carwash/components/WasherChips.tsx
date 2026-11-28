import type { Washer } from '@/lib/api/types'

/** Pick who washes the car now (can also be done later on the queue). */
export function WasherChips({ washers, chosen, onToggle }: { washers: Washer[]; chosen: string[]; onToggle: (userId: string) => void }) {
  if (washers.length === 0) return <p className="text-ink-muted">Chi nhánh chưa có nhân viên rửa xe.</p>
  return (
    <div role="group" aria-label="Người rửa" className="flex flex-wrap gap-2">
      {washers.map((washer) => (
        <button key={washer.userId} type="button" aria-pressed={chosen.includes(washer.userId)} onClick={() => onToggle(washer.userId)}
          className={`h-12 rounded-full border-2 px-4 font-semibold
            ${chosen.includes(washer.userId) ? 'border-(--module) bg-(--module) text-white' : 'border-line bg-surface text-ink'}`}>
          {washer.fullName}
        </button>
      ))}
    </div>
  )
}
