const NOTES = [10_000, 20_000, 50_000, 100_000, 200_000, 500_000]
const MAX_SUGGESTIONS = 4

/**
 * Quick cash buttons: the exact amount, then the amounts a customer is likely to hand over
 * (rounded up to common bank notes). Display aid only — the backend computes the real change.
 */
export function suggestCash(total: number): number[] {
  if (total <= 0) return []
  const amounts = new Set<number>([total])
  for (const note of NOTES) {
    const rounded = Math.ceil(total / note) * note
    if (rounded > total) amounts.add(rounded)
  }
  return [...amounts].sort((a, b) => a - b).slice(0, MAX_SUGGESTIONS)
}

/** Preview of the change to hand back; negative = customer still owes. */
export function previewChange(total: number, given: number): number {
  return given - total
}
