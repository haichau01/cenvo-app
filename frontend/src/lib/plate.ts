/** Vietnamese car plate: province (2 digits) + series (1–2 letters, optional digit) + 4–5 digit number. */
// The series digit is lazy so a 5-digit number wins: 51A12345 → 51A + 12345, not 51A1 + 2345.
const PLATE = /^(\d{2}[A-Z]{1,2}\d??)(\d{4,5})$/

/** Same rule as the backend `PlateNumber.Normalize`: upper case, letters and digits only. */
export function normalizePlate(value: string): string {
  return value.toUpperCase().replace(/[^A-Z0-9]/g, '')
}

/** "51A12345" → "51A-123.45", "30K1234" → "30K-1234". Unknown shapes are shown as stored. */
export function formatPlate(value: string): string {
  const plate = normalizePlate(value)
  const match = PLATE.exec(plate)
  if (!match) return plate
  const [, series, number] = match
  const shown = number.length === 5 ? `${number.slice(0, 3)}.${number.slice(3)}` : number
  return `${series}-${shown}`
}

/** Backend accepts 5–12 letters/digits. */
export function isPlausiblePlate(value: string): boolean {
  const plate = normalizePlate(value)
  return plate.length >= 5 && plate.length <= 12
}
