/** Longest side after shrinking — enough to see a scratch, small enough to upload fast on shop Wi-Fi. */
export const MAX_PHOTO_SIDE = 1600
const JPEG_QUALITY = 0.82

/** Size that fits inside `max` × `max` keeping the aspect ratio (never enlarges). Pure, unit-tested. */
export function fitWithin(width: number, height: number, max = MAX_PHOTO_SIDE): { width: number; height: number } {
  const scale = Math.min(1, max / Math.max(width, height))
  return { width: Math.round(width * scale), height: Math.round(height * scale) }
}

/** Re-encodes a camera photo as a smaller JPEG in the browser. Falls back to the original file if decoding fails. */
export async function shrinkPhoto(file: File): Promise<Blob> {
  try {
    const bitmap = await createImageBitmap(file)
    const size = fitWithin(bitmap.width, bitmap.height)
    const canvas = document.createElement('canvas')
    canvas.width = size.width
    canvas.height = size.height
    canvas.getContext('2d')!.drawImage(bitmap, 0, 0, size.width, size.height)
    bitmap.close()
    return await new Promise<Blob>((resolve, reject) =>
      canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('encode'))), 'image/jpeg', JPEG_QUALITY))
  } catch {
    return file
  }
}
