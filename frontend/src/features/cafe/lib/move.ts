import type { TableDto } from '@/lib/api/types'

export type MoveDestination = { kind: 'table'; table: TableDto } | { kind: 'takeAway' }

/** One sentence saying what will happen, so staff can check before tapping "Chuyển". */
export function moveSummary(destination: MoveDestination, allLines: boolean, lineCount: number): string {
  const what = allLines ? 'Cả hóa đơn' : `${lineCount} món đã chọn`
  if (destination.kind === 'takeAway') return `${what} sẽ sang một hóa đơn mang đi mới.`
  if (destination.table.openOrderId != null) return `${what} sẽ gộp vào hóa đơn đang mở của ${destination.table.name}.`
  if (allLines) return `Khách chuyển sang ${destination.table.name}.`
  return `${what} sẽ tách sang hóa đơn mới ở ${destination.table.name}.`
}
