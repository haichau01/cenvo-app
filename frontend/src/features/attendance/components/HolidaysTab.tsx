import { useState } from 'react'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { toast } from '@/components/ui/toastStore'
import type { Holiday } from '@/lib/api/types'
import { formatDate } from '@/lib/format'
import { useDeleteHoliday, useHolidays, useSaveHoliday } from '../api/attendance'

/** Chain-wide holidays: the owner edits them, managers only read (they change pay). */
export function HolidaysTab({ today, canEdit }: { today: string; canEdit: boolean }) {
  const [year, setYear] = useState(() => Number(today.slice(0, 4)))
  const holidays = useHolidays(year)
  const remove = useDeleteHoliday()

  return (
    <div className="flex max-w-3xl flex-col gap-4">
      <div className="flex items-center gap-2">
        <Button variant="ghost" onClick={() => setYear(year - 1)}>Năm trước</Button>
        <p className="w-24 text-center font-display text-block font-semibold tabular-nums">{year}</p>
        <Button variant="ghost" onClick={() => setYear(year + 1)}>Năm sau</Button>
      </div>
      <p className="text-ink-muted">Đi làm ngày lễ được tính số công bằng hệ số; nghỉ ngày lễ vẫn được tính 1 công.</p>
      <FormError message={holidays.error?.message ?? remove.error?.message} />
      {holidays.data && holidays.data.length === 0 && <EmptyState title={`Chưa có ngày lễ nào trong năm ${year}`} />}
      {holidays.data && holidays.data.length > 0 && (
        <ul className="divide-y divide-line rounded-(--radius-panel) bg-surface">
          {holidays.data.map((h) => (
            <li key={h.id} className="flex h-12 items-center gap-4 px-4">
              <span className="w-28 tabular-nums">{formatDate(h.date)}</span>
              <span className="flex-1 font-medium">{h.name}</span>
              <span className="tabular-nums">× {h.multiplier}</span>
              {canEdit && (
                <Button variant="ghost" busy={remove.isPending}
                  onClick={() => remove.mutate(h.id, { onSuccess: () => toast.success(`Đã bỏ ${h.name}`) })}>Bỏ</Button>
              )}
            </li>
          ))}
        </ul>
      )}
      {canEdit && <AddHoliday year={year} existing={holidays.data ?? []} />}
    </div>
  )
}

function AddHoliday({ year, existing }: { year: number; existing: Holiday[] }) {
  const save = useSaveHoliday()
  const [date, setDate] = useState('')
  const [name, setName] = useState('')
  const [multiplier, setMultiplier] = useState(3)

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    save.mutate({ id: null, input: { date, name, multiplier } }, {
      onSuccess: () => { toast.success(`Đã thêm ${name}`); setDate(''); setName('') },
    })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-(--radius-panel) bg-surface p-4">
      <h2 className="font-display text-block font-semibold">Thêm ngày lễ</h2>
      <div className="flex flex-wrap items-end gap-3">
        <TextField label="Ngày" type="date" required min={`${year}-01-01`} max={`${year}-12-31`} value={date}
          onChange={(e) => setDate(e.target.value)} className="w-48" />
        <TextField label="Tên" required maxLength={100} placeholder="Ví dụ: Quốc khánh" value={name}
          onChange={(e) => setName(e.target.value)} className="flex-1" />
        <TextField label="Hệ số" type="number" required min={1} max={5} step={0.5} value={multiplier}
          onChange={(e) => setMultiplier(Number(e.target.value))} className="w-28" />
        <Button type="submit" busy={save.isPending} disabled={existing.some((h) => h.date === date)}>Thêm</Button>
      </div>
      <FormError message={save.error?.message} />
    </form>
  )
}
