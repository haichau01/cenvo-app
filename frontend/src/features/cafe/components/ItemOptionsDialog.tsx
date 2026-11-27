import { useMemo, useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { Button } from '@/components/ui/Button'
import { Dialog } from '@/components/ui/Dialog'
import { QuantityStepper } from '@/components/ui/QuantityStepper'
import type { AddLineInput, MenuItem } from '@/lib/api/types'
import { useMenu } from '../api/setup'
import { splitMenu } from '../lib/menu'

interface ItemOptionsDialogProps {
  item: MenuItem | null
  onClose: () => void
  onConfirm: (item: MenuItem, input: AddLineInput) => void
}

/** Quantity, toppings (per cup) and a note for the barista. Mounted fresh per item so the form starts clean. */
export function ItemOptionsDialog({ item, onClose, onConfirm }: ItemOptionsDialogProps) {
  return (
    <Dialog open={item !== null} onClose={onClose} title={item?.name ?? ''}>
      {item && <OptionsForm key={item.productId} item={item} onCancel={onClose} onConfirm={onConfirm} />}
    </Dialog>
  )
}

interface OptionsFormProps {
  item: MenuItem
  onCancel: () => void
  onConfirm: (item: MenuItem, input: AddLineInput) => void
}

function OptionsForm({ item, onCancel, onConfirm }: OptionsFormProps) {
  const menu = useMenu()
  const toppings = useMemo(() => splitMenu(menu.data ?? []).toppings, [menu.data])
  const [quantity, setQuantity] = useState(1)
  const [picked, setPicked] = useState<Set<string>>(new Set())
  const [note, setNote] = useState('')

  function handleToggle(productId: string) {
    setPicked((current) => {
      const next = new Set(current)
      if (!next.delete(productId)) next.add(productId)
      return next
    })
  }

  function handleConfirm() {
    onConfirm(item, {
      productId: item.productId,
      quantity,
      toppings: [...picked].map((productId) => ({ productId, quantity: 1 })),
      note: note.trim() || null,
    })
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <span className="font-medium">Số lượng</span>
        <QuantityStepper value={quantity} onChange={setQuantity} label={item.name} min={1} />
      </div>

      {toppings.length > 0 && (
        <fieldset>
          <legend className="mb-2 font-medium">Topping (mỗi ly)</legend>
          <div className="grid grid-cols-2 gap-2">
            {toppings.map((topping) => (
              <ToppingChoice key={topping.productId} topping={topping} checked={picked.has(topping.productId)}
                onToggle={handleToggle} />
            ))}
          </div>
        </fieldset>
      )}

      <label className="flex flex-col gap-1.5">
        <span className="font-medium">Ghi chú cho pha chế</span>
        <input value={note} onChange={(event) => setNote(event.target.value)} maxLength={200}
          placeholder="Ít đá, ít ngọt…"
          className="h-12 rounded-(--radius-control) border border-line bg-surface px-3 outline-none
            focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-foam" />
      </label>

      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onCancel}>Bỏ qua</Button>
        <Button size="lg" onClick={handleConfirm}>Thêm vào hóa đơn</Button>
      </div>
    </div>
  )
}

interface ToppingChoiceProps {
  topping: MenuItem
  checked: boolean
  onToggle: (productId: string) => void
}

function ToppingChoice({ topping, checked, onToggle }: ToppingChoiceProps) {
  return (
    <label className={`flex h-12 cursor-pointer items-center justify-between gap-2 rounded-(--radius-control) border-2 px-3
      ${checked ? 'border-(--module) bg-surface' : 'border-line bg-concrete'}`}>
      <span className="flex items-center gap-2">
        <input type="checkbox" checked={checked} onChange={() => onToggle(topping.productId)}
          className="size-5 accent-(--module)" />
        {topping.name}
      </span>
      <Money value={topping.price} className="text-sm text-ink-muted" />
    </label>
  )
}
