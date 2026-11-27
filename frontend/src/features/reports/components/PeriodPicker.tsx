import { controlClass } from '@/components/ui/fieldStyles'
import { matchingPreset, PRESETS, type Period } from '../lib/period'

interface PeriodPickerProps {
  value: Period
  today: string
  onChange: (period: Period) => void
}

/** One row above the report: quick presets + exact dates (both ends included). */
export function PeriodPicker({ value, today, onChange }: PeriodPickerProps) {
  const active = matchingPreset(value, today)
  return (
    <div className="mb-6 flex flex-wrap items-end gap-3">
      <div role="group" aria-label="Khoảng thời gian" className="flex rounded-(--radius-control) bg-surface p-1">
        {PRESETS.map((preset) => (
          <button key={preset.id} type="button" aria-pressed={preset.id === active} onClick={() => onChange(preset.period(today))}
            className={`h-10 rounded-[4px] px-4 font-semibold ${preset.id === active ? 'bg-ink text-white' : 'text-ink-muted hover:text-ink'}`}>
            {preset.label}
          </button>
        ))}
      </div>
      <label className="flex flex-col gap-1.5 text-sm font-medium">Từ ngày
        <input type="date" value={value.from} max={value.to} className={controlClass}
          onChange={(event) => event.target.value && onChange({ ...value, from: event.target.value })} />
      </label>
      <label className="flex flex-col gap-1.5 text-sm font-medium">Đến ngày
        <input type="date" value={value.to} min={value.from} max={today} className={controlClass}
          onChange={(event) => event.target.value && onChange({ ...value, to: event.target.value })} />
      </label>
    </div>
  )
}
