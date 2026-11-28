import { useState } from 'react'
import { Money } from '@/components/cenvo/Money'
import { PageHeader } from '@/components/cenvo/PageHeader'
import { StatusPill } from '@/components/cenvo/StatusPill'
import { Button } from '@/components/ui/Button'
import type { WashOffering, WashPackage } from '@/lib/api/types'
import { formatDuration } from '@/lib/format'
import { useOfferings, usePackages } from '../api/setup'
import { OfferingDialog } from '../components/OfferingDialog'
import { PackageDialog } from '../components/PackageDialog'
import { priceFor } from '../lib/intake'
import { vehicleLabel, VEHICLE_TYPES } from '@/lib/vehicle'

const th = 'px-3 py-2 text-left text-caption font-medium text-ink-muted'
const td = 'px-3 py-2.5'

/** Managers: the car wash price list (per vehicle type), commission rules, supplies per car, and packages on sale. */
export function WashSetupPage() {
  const offerings = useOfferings(true).data ?? []
  const packages = usePackages(true).data ?? []
  const [editingOffering, setEditingOffering] = useState<WashOffering | 'new' | null>(null)
  const [editingPackage, setEditingPackage] = useState<WashPackage | 'new' | null>(null)

  return (
    <div data-module="wash" className="flex flex-col gap-8">
      <PageHeader title="Dịch vụ rửa xe" description="Giá theo loại xe, hoa hồng chia đều cho người rửa, hóa chất trừ kho mỗi xe."
        actions={<Button onClick={() => setEditingOffering('new')}>Thêm dịch vụ</Button>} />
      <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
        <thead className="border-b border-line">
          <tr>
            <th className={th}>Dịch vụ</th><th className={th}>Thời gian</th>
            {VEHICLE_TYPES.map((type) => <th key={type.value} className={`${th} text-right`}>{type.label}</th>)}
            <th className={th}>Hoa hồng</th><th className={th}>Hóa chất</th><th className={th}>Trạng thái</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {offerings.map((offering) => (
            <tr key={offering.id} onClick={() => setEditingOffering(offering)} className="h-11 cursor-pointer hover:bg-concrete">
              <td className={td}><span className="font-semibold">{offering.name}</span> <span className="text-ink-muted">{offering.code}</span></td>
              <td className={td}>{formatDuration(offering.durationMinutes)}</td>
              {VEHICLE_TYPES.map((type) => <td key={type.value} className={`${td} text-right`}><PriceCell price={priceFor(offering, type.value)} /></td>)}
              <td className={td}><Commission offering={offering} /></td>
              <td className={td}>{offering.supplies.map((supply) => `${supply.productName} ${supply.quantity} ${supply.unitName}`).join(', ') || '—'}</td>
              <td className={td}><StatusPill label={offering.isActive ? 'Đang cung cấp' : 'Ngừng'} tone={offering.isActive ? 'go' : 'muted'} /></td>
            </tr>
          ))}
        </tbody>
      </table>

      <PageHeader title="Gói thành viên" actions={<Button variant="secondary" onClick={() => setEditingPackage('new')}>Thêm gói</Button>} />
      <table className="w-full overflow-hidden rounded-(--radius-panel) bg-surface text-table">
        <thead className="border-b border-line">
          <tr><th className={th}>Gói</th><th className={th}>Dịch vụ</th><th className={th}>Loại xe</th>
            <th className={`${th} text-right`}>Giá</th><th className={`${th} text-right`}>Lượt</th><th className={`${th} text-right`}>Hạn</th><th className={th}>Trạng thái</th></tr>
        </thead>
        <tbody className="divide-y divide-line">
          {packages.map((item) => (
            <tr key={item.id} onClick={() => setEditingPackage(item)} className="h-11 cursor-pointer hover:bg-concrete">
              <td className={td}><span className="font-semibold">{item.name}</span> <span className="text-ink-muted">{item.code}</span></td>
              <td className={td}>{item.offeringName}</td>
              <td className={td}>{item.vehicleType ? vehicleLabel(item.vehicleType) : 'Mọi loại'}</td>
              <td className={`${td} text-right`}><Money value={item.price} /></td>
              <td className={`${td} text-right tabular-nums`}>{item.uses}</td>
              <td className={`${td} text-right tabular-nums`}>{item.validDays} ngày</td>
              <td className={td}><StatusPill label={item.isActive ? 'Đang bán' : 'Ngừng'} tone={item.isActive ? 'go' : 'muted'} /></td>
            </tr>
          ))}
        </tbody>
      </table>
      <OfferingDialog offering={editingOffering} onClose={() => setEditingOffering(null)} />
      <PackageDialog item={editingPackage} offerings={offerings} onClose={() => setEditingPackage(null)} />
    </div>
  )
}

function PriceCell({ price }: { price: number | null }) {
  if (price === null) return <span className="text-ink-muted">—</span>
  return <Money value={price} />
}

function Commission({ offering }: { offering: WashOffering }) {
  if (offering.commissionType === 'Percent') return <span className="tabular-nums">{offering.commissionValue}%</span>
  return <Money value={offering.commissionValue} />
}
