import { useState } from 'react'
import { useSearchParams } from 'react-router'
import { EmptyState } from '@/components/cenvo/EmptyState'
import { Plate } from '@/components/cenvo/Plate'
import { Button } from '@/components/ui/Button'
import { TextField } from '@/components/ui/TextField'
import { canCheckout, isManager } from '@/lib/auth/roles'
import { useSession } from '@/lib/auth/session'
import { useDebounced } from '@/lib/useDebounced'
import { useCustomerSearch } from '../api/customers'
import { CustomerDetail } from '../components/CustomerDetail'
import { RequireBranch } from '../components/RequireBranch'
import { SellMembershipDialog, type MembershipBuyer } from '../components/SellMembershipDialog'

const CUSTOMER_PARAM = 'kh'

export function CustomersPage() {
  return <RequireBranch from="/rua-xe/khach-hang"><CustomersScreen /></RequireBranch>
}

/** Find a customer by name, phone or plate; see their cars, packages and visits; sell a package. */
function CustomersScreen() {
  const user = useSession()!.user
  const [params, setParams] = useSearchParams()
  const selectedId = params.get(CUSTOMER_PARAM)
  const [query, setQuery] = useState('')
  const search = useCustomerSearch(useDebounced(query.trim()))
  const [buyer, setBuyer] = useState<MembershipBuyer | null>(null)
  const cashier = canCheckout(user)

  return (
    <div className="grid min-h-0 flex-1 grid-cols-[22rem_1fr] gap-6 p-6">
      <section aria-label="Danh sách khách" className="flex min-h-0 flex-col gap-3">
        <div className="flex items-center justify-between gap-3">
          <h1 className="font-display text-screen font-semibold">Khách hàng</h1>
          {cashier && <Button variant="secondary" onClick={() => setBuyer({ phone: '', name: '' })}>Bán gói</Button>}
        </div>
        <TextField label="Tìm theo tên, số điện thoại, biển số" type="search" value={query}
          onChange={(event) => setQuery(event.target.value)} />
        <ul className="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto">
          {search.data?.map((customer) => (
            <li key={customer.id}>
              <button type="button" aria-current={customer.id === selectedId}
                onClick={() => setParams({ [CUSTOMER_PARAM]: customer.id }, { replace: true })}
                className={`flex w-full flex-col gap-1 rounded-(--radius-control) px-3 py-2 text-left
                  ${customer.id === selectedId ? 'bg-ink text-white' : 'hover:bg-surface'}`}>
                <span className="font-semibold">{customer.name}</span>
                <span className="text-sm opacity-80">{customer.phone}</span>
                <span className="flex flex-wrap gap-1">{customer.plates.map((plate) => <Plate key={plate} value={plate} />)}</span>
              </button>
            </li>
          ))}
          {search.data?.length === 0 && <li className="py-6 text-center text-ink-muted">Không có khách phù hợp.</li>}
        </ul>
      </section>
      <section aria-label="Chi tiết khách" className="min-h-0 overflow-y-auto rounded-(--radius-panel) bg-surface p-6">
        {selectedId
          ? <CustomerDetail key={selectedId} customerId={selectedId} canSell={cashier} canEdit={cashier} canCancelMembership={isManager(user)}
              onSell={(customer) => setBuyer({ phone: customer.phone, name: customer.name })} />
          : <EmptyState title="Chọn một khách để xem" description="Khách được lưu khi tiếp nhận xe có số điện thoại hoặc khi mua gói." />}
      </section>
      <SellMembershipDialog buyer={buyer} onClose={() => setBuyer(null)} />
    </div>
  )
}
