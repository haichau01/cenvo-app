import { memo } from 'react'
import { encode } from 'uqr'

/** QR code as crisp SVG squares (scales to any size, prints and photographs well). */
export const QrCode = memo(function QrCode({ value, label, className = '' }: { value: string; label: string; className?: string }) {
  const { data } = encode(value, { ecc: 'M', border: 2 })
  const size = data.length
  const path = data.flatMap((row, y) => row.map((dark, x) => (dark ? `M${x} ${y}h1v1h-1z` : ''))).join('')
  return (
    <svg viewBox={`0 0 ${size} ${size}`} role="img" aria-label={label} shapeRendering="crispEdges" className={`bg-white ${className}`}>
      <path d={path} fill="#18263A" />
    </svg>
  )
})
