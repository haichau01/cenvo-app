import type { PillTone } from '@/components/cenvo/StatusPill'
import type { AdjustmentKind, ContractType, Department, EmployeeListItem } from '@/lib/api/types'

export const DEPARTMENTS: { value: Department; label: string }[] = [
  { value: 'Cafe', label: 'Cà phê' },
  { value: 'CarWash', label: 'Rửa xe' },
  { value: 'General', label: 'Chung' },
]

export const CONTRACT_TYPES: { value: ContractType; label: string }[] = [
  { value: 'FullTime', label: 'Toàn thời gian' },
  { value: 'PartTime', label: 'Bán thời gian' },
  { value: 'Seasonal', label: 'Thời vụ' },
]

/** Account roles, in the words staff use. Mirrors backend `Roles.All`. */
export const ROLES: { value: string; label: string }[] = [
  { value: 'Admin', label: 'Chủ quán' },
  { value: 'BranchManager', label: 'Quản lý chi nhánh' },
  { value: 'Cashier', label: 'Thu ngân' },
  { value: 'Barista', label: 'Pha chế' },
  { value: 'Storekeeper', label: 'Thủ kho' },
  { value: 'Washer', label: 'Rửa xe' },
]

/** Sign shows whether the amount adds to or comes out of pay. */
export const ADJUSTMENT_KINDS: { value: AdjustmentKind; label: string; sign: 1 | -1 }[] = [
  { value: 'Advance', label: 'Tạm ứng', sign: -1 },
  { value: 'Bonus', label: 'Thưởng', sign: 1 },
  { value: 'Penalty', label: 'Phạt', sign: -1 },
]

const labelOf = <T extends string>(list: { value: T; label: string }[], value: T | null | undefined) =>
  list.find((item) => item.value === value)?.label ?? ''

export const departmentLabel = (value: Department | null | undefined) => labelOf(DEPARTMENTS, value)
export const contractLabel = (value: ContractType | null | undefined) => labelOf(CONTRACT_TYPES, value)
export const roleLabel = (value: string) => labelOf(ROLES, value) || value
export const adjustmentLabel = (value: AdjustmentKind) => labelOf(ADJUSTMENT_KINDS, value)
export const adjustmentSign = (value: AdjustmentKind) => ADJUSTMENT_KINDS.find((k) => k.value === value)?.sign ?? 1

/** One status per row of the staff list: left, no profile yet, contract ending, or working. */
export function employeeStatus(e: Pick<EmployeeListItem, 'leaveDate' | 'hasProfile' | 'contractEndsSoon' | 'isActive'>): {
  label: string
  tone: PillTone
} {
  if (e.leaveDate) return { label: 'Đã nghỉ', tone: 'muted' }
  if (!e.isActive) return { label: 'Tài khoản khóa', tone: 'stop' }
  if (!e.hasProfile) return { label: 'Chưa có hồ sơ', tone: 'lane' }
  if (e.contractEndsSoon) return { label: 'Sắp hết hợp đồng', tone: 'lane' }
  return { label: 'Đang làm', tone: 'go' }
}

/** First day of a month in yyyy-MM-dd (salary rates start on the 1st). */
export function firstOfMonth(yyyyMm: string): string {
  return `${yyyyMm}-01`
}

/** "2026-11-28" → "2026-12" (month after the given date). */
export function nextMonth(isoDate: string): string {
  const [year, month] = isoDate.split('-').map(Number)
  return month === 12 ? `${year + 1}-01` : `${year}-${String(month + 1).padStart(2, '0')}`
}

/** "2026-12-01" → "Tháng 12/2026" */
export function monthLabel(isoDate: string): string {
  const [year, month] = isoDate.split('-')
  return `Tháng ${Number(month)}/${year}`
}
