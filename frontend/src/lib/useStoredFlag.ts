import { useCallback, useState } from 'react'

/** A boolean UI preference remembered on this device (e.g. dark mode of the kitchen screen). */
export function useStoredFlag(key: string, initial = false): [boolean, (value: boolean) => void] {
  const [value, setValue] = useState(() => {
    const stored = localStorage.getItem(key)
    return stored === null ? initial : stored === '1'
  })
  const update = useCallback((next: boolean) => {
    localStorage.setItem(key, next ? '1' : '0')
    setValue(next)
  }, [key])
  return [value, update]
}
