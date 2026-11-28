import { useEffect, useState } from 'react'

/** `value` once it has stopped changing for `delayMs` — e.g. look a plate up after the user stops typing. */
export function useDebounced<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])
  return debounced
}
