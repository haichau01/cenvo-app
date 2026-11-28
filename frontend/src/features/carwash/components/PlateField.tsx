import { useId } from 'react'
import { Plate } from '@/components/cenvo/Plate'
import { isPlausiblePlate } from '@/lib/plate'

/** Big plate input: type freely ("51a 12345"), see it as a real plate chip right away. */
export function PlateField({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-ink">Biển số</label>
      <div className="flex items-center gap-4">
        <input id={id} value={value} onChange={(event) => onChange(event.target.value)} autoFocus autoComplete="off"
          autoCapitalize="characters" spellCheck={false} placeholder="51A-123.45" maxLength={16}
          className="h-16 w-64 rounded-(--radius-control) border border-line bg-surface px-4 font-display text-3xl font-bold
            uppercase tabular-nums text-ink outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam" />
        {isPlausiblePlate(value) && <Plate value={value} size="lg" />}
      </div>
    </div>
  )
}
