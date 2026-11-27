export type PillTone = 'go' | 'lane' | 'stop' | 'muted'

const dots: Record<PillTone, string> = {
  go: 'bg-go',
  lane: 'bg-lane',
  stop: 'bg-stop',
  muted: 'bg-ink-muted',
}

/** Small status pill: colored dot + text (never color alone). */
export function StatusPill({ label, tone }: { label: string; tone: PillTone }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full bg-concrete px-2.5 py-0.5 text-caption font-medium text-ink">
      <span aria-hidden className={`size-2 rounded-full ${dots[tone]}`} />
      {label}
    </span>
  )
}
