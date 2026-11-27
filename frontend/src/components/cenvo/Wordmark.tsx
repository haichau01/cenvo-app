/** Cenvo wordmark + two module stripes (Robusta brown = cafe, foam blue = car wash). */
export function Wordmark({ tone = 'ink', size = 'md' }: { tone?: 'ink' | 'light'; size?: 'md' | 'lg' }) {
  const text = tone === 'light' ? 'text-white' : 'text-ink'
  const big = size === 'lg'
  return (
    <span className={`inline-flex items-center gap-2.5 ${text}`}>
      <span aria-hidden className={`flex gap-1 ${big ? 'h-9' : 'h-6'}`}>
        <span className={`${big ? 'w-1.5' : 'w-1'} rounded-full bg-robusta-bright`} />
        <span className={`${big ? 'w-1.5' : 'w-1'} rounded-full bg-foam-bright`} />
      </span>
      <span className={`font-display font-bold tracking-[0.01em] ${big ? 'text-[2.5rem] leading-none' : 'text-2xl leading-none'}`}>
        Cenvo
      </span>
    </span>
  )
}
