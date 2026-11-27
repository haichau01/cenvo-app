/** Lowercase without Vietnamese diacritics, so "sua tuoi" finds "Sữa tươi" and "đ" matches "d". */
export function foldText(text: string): string {
  return text.normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd').replace(/Đ/g, 'd').toLowerCase().trim()
}

/** Items whose fields contain every word of the query (any order), in their original order, at most `limit`. */
export function searchBy<T>(items: T[], query: string, fields: (item: T) => string[], limit: number): T[] {
  const words = foldText(query).split(/\s+/).filter(Boolean)
  if (words.length === 0) return []
  const matches: T[] = []
  for (const item of items) {
    const haystack = foldText(fields(item).join(' '))
    if (words.every((word) => haystack.includes(word))) matches.push(item)
    if (matches.length === limit) break
  }
  return matches
}
