/** "Print after …" checkbox; the choice is remembered on this device. */
export function PrintToggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex h-12 items-center gap-3 font-medium">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)}
        className="size-5 accent-(--module)" />
      {label}
    </label>
  )
}
