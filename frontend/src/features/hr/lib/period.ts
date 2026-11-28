/** "2026-02" → first and last day of that month (yyyy-MM-dd), for month-based HR lists. */
export function monthRange(yyyyMm: string): { from: string; to: string } {
  const [year, month] = yyyyMm.split('-').map(Number)
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate()
  return { from: `${yyyyMm}-01`, to: `${yyyyMm}-${String(lastDay).padStart(2, '0')}` }
}
