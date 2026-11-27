import type { ProductDto, StockDocumentDto, StockDocumentInput, StockDocumentType } from '@/lib/api/types'
import { vnDate } from '@/lib/format'
import { DOCUMENT_TYPES } from './documentTypes'

/** One editable line. `key` is a client id so React keeps rows stable while editing. */
export interface DraftLine {
  key: string
  productId: string
  productCode: string
  productName: string
  /** null = the product's base unit. */
  unitId: string | null
  quantity: number
  unitPrice: number
  note: string
}

export interface DraftDocument {
  type: StockDocumentType
  warehouseId: string
  toWarehouseId: string | null
  supplierId: string | null
  /** yyyy-MM-dd in Vietnam time. */
  date: string
  note: string
  lines: DraftLine[]
}


export function emptyDraft(type: StockDocumentType, warehouseId: string, today: string): DraftDocument {
  return { type, warehouseId, toWarehouseId: null, supplierId: null, date: today, note: '', lines: [] }
}

export function draftFromDto(doc: StockDocumentDto): DraftDocument {
  return {
    type: doc.type,
    warehouseId: doc.warehouseId,
    toWarehouseId: doc.toWarehouseId ?? null,
    supplierId: doc.supplierId ?? null,
    date: vnDate(doc.date),
    note: doc.note ?? '',
    lines: (doc.lines ?? []).map((line) => ({
      key: line.id, productId: line.productId, productCode: line.productCode, productName: line.productName,
      unitId: line.unitId ?? null, quantity: line.quantity, unitPrice: line.unitPrice, note: line.note ?? '',
    })),
  }
}

export function lineFromProduct(product: ProductDto, key: string): DraftLine {
  return {
    key, productId: product.id, productCode: product.code, productName: product.name,
    unitId: null, quantity: 1, unitPrice: 0, note: '',
  }
}

/**
 * Today → no date (the server stamps the exact time, keeping documents in real order);
 * another day → noon Vietnam time of that day, so time zones can never shift it to a neighbouring date.
 */
export function documentDate(date: string, today: string): string | null {
  return date === today ? null : `${date}T12:00:00+07:00`
}

export function toInput(draft: DraftDocument, today: string): StockDocumentInput {
  const config = DOCUMENT_TYPES[draft.type]
  return {
    type: draft.type,
    warehouseId: draft.warehouseId,
    toWarehouseId: config.destination ? draft.toWarehouseId : null,
    supplierId: config.supplier ? draft.supplierId : null,
    date: documentDate(draft.date, today),
    note: draft.note.trim() || null,
    lines: draft.lines.map((line) => ({
      productId: line.productId,
      unitId: line.unitId,
      quantity: line.quantity,
      unitPrice: config.priceLabel ? line.unitPrice : 0,
      note: line.note.trim() || null,
    })),
  }
}

/** First problem that would make the server refuse the draft, in Vietnamese; null when it looks fine. */
export function validateDraft(draft: DraftDocument): string | null {
  const config = DOCUMENT_TYPES[draft.type]
  if (!draft.warehouseId) return 'Chọn kho cho phiếu.'
  if (config.destination && !draft.toWarehouseId) return 'Chọn kho đến.'
  if (config.destination && draft.toWarehouseId === draft.warehouseId) return 'Kho đến phải khác kho đi.'
  if (draft.lines.length === 0) return 'Phiếu chưa có dòng hàng. Gõ tên sản phẩm vào ô “Thêm sản phẩm” để thêm.'
  const zero = draft.lines.find((line) => !(line.quantity > 0))
  if (zero) return `Số lượng của “${zero.productName}” phải lớn hơn 0.`
  if (draft.type === 'Stocktake') {
    const ids = draft.lines.map((line) => line.productId)
    if (new Set(ids).size !== ids.length) return 'Phiếu kiểm kê không được trùng sản phẩm.'
  }
  return null
}

/** Display-only line total; the server stores the real amount. */
export function lineAmount(line: Pick<DraftLine, 'quantity' | 'unitPrice'>): number {
  return Math.round(line.quantity * line.unitPrice)
}
