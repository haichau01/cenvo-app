import { useEffect, useState } from 'react'

/** Current time, refreshed every `intervalMs` — for "waiting N minutes" counters without re-rendering every second. */
export function useNow(intervalMs = 30_000): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), intervalMs)
    return () => clearInterval(timer)
  }, [intervalMs])
  return now
}
