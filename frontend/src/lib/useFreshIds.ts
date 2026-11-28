import { useState } from 'react'

interface FreshState<T> {
  source: T[] | undefined
  /** null until the first load: items already there when the screen opens are not "new". */
  seen: ReadonlySet<string> | null
  fresh: ReadonlySet<string>
}

/** Ids that arrived after the first load — highlighted once so new work catches the eye (kitchen, wash queue). */
export function useFreshIds<T>(items: T[] | undefined, idOf: (item: T) => string): ReadonlySet<string> {
  const [state, setState] = useState<FreshState<T>>({ source: undefined, seen: null, fresh: new Set() })
  // Adjusting state while rendering when the input changes (React's recommended alternative to an effect).
  if (items && items !== state.source) setState(trackArrivals(state, items, idOf))
  return state.fresh
}

function trackArrivals<T>(state: FreshState<T>, items: T[], idOf: (item: T) => string): FreshState<T> {
  const seen = new Set(state.seen)
  const fresh = new Set(state.fresh)
  for (const item of items) {
    const id = idOf(item)
    if (seen.has(id)) continue
    seen.add(id)
    if (state.seen !== null) fresh.add(id)
  }
  return { source: items, seen, fresh }
}
