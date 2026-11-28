import type { PunchResult } from '@/lib/api/types'
import { formatTime } from '@/lib/format'
import { formatMinutes } from './attendance'

/** One sentence the person reads right after punching. */
export function punchMessage(result: PunchResult): string {
  const at = formatTime(result.at)
  if (result.action === 'CheckOut') return `${result.employeeName} đã ra ca lúc ${at} — làm ${formatMinutes(result.workedMinutes)}.`
  if (result.needsApproval) return `${result.employeeName} đã vào lúc ${at}. Không có ca được xếp: chờ quản lý duyệt.`
  const late = result.lateMinutes > 0 ? `, muộn ${formatMinutes(result.lateMinutes)}` : ''
  return `${result.employeeName} đã vào ca ${result.shiftName ?? ''} lúc ${at}${late}.`
}
