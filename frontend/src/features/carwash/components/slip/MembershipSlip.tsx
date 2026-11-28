import { Slip, SlipDivider, SlipFacts, SlipFooter, SlipPayments, SlipRow } from '@/components/cenvo/Slip'
import type { MembershipReceipt } from '@/lib/api/types'
import { formatDate, formatDateTime, formatVnd } from '@/lib/format'
import { vehicleLabel } from '@/lib/vehicle'

/** Receipt of a sold membership package: what it covers, how many washes, until when. */
export function MembershipSlip({ receipt, printedAt }: { receipt: MembershipReceipt; printedAt: string }) {
  const { membership } = receipt
  const cancelled = membership.status === 'Cancelled'
  return (
    <Slip store={receipt.store} title={cancelled ? 'Gói thành viên đã hủy' : 'Hóa đơn bán gói thành viên'}>
      <SlipFacts code={membership.code} facts={[
        ['Khách', `${receipt.customerName} · ${receipt.customerPhone}`],
        ['Giờ', formatDateTime(membership.paidAt)],
        ['Thu ngân', receipt.cashierName],
      ]} />

      <SlipDivider />
      <p className="font-semibold">{membership.packageName}</p>
      <p>
        {membership.totalUses} lượt {membership.offeringName}
        {membership.vehicleType && ` · xe ${vehicleLabel(membership.vehicleType)}`}
      </p>
      <p>Hạn dùng: {formatDate(membership.expiresAt)}</p>
      <SlipDivider />

      <SlipRow label="Tổng cộng" value={formatVnd(membership.price)} strong />
      <SlipPayments payments={receipt.payments} cashReceived={receipt.cashReceived} changeAmount={receipt.changeAmount} />

      <SlipFooter printedAt={printedAt}>
        <p>Đọc số điện thoại khi rửa xe để trừ lượt, ở mọi chi nhánh.</p>
      </SlipFooter>
    </Slip>
  )
}
