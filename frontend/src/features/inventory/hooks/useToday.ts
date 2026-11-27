import { useState } from 'react'
import { vnDate } from '@/lib/format'

/** Today's date in Vietnam (yyyy-MM-dd), fixed for the lifetime of the screen. */
export function useToday(): string {
  return useState(() => vnDate(Date.now()))[0]
}
