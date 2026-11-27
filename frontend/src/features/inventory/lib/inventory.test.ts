import { describe, expect, it } from 'vitest'
import { documentDate, emptyDraft, lineAmount, toInput, validateDraft, vnDate, type DraftLine } from './documentForm'
import { documentStatusView } from './status'

const line = (over: Partial<DraftLine> = {}): DraftLine => ({
  key: 'k', productId: 'p1', productCode: 'SUA', productName: 'Sữa tươi', unitId: null,
  quantity: 2, unitPrice: 30_000, note: '', ...over,
})

describe('document form', () => {
  it('omits the date for today and pins other days to noon VN time', () => {
    expect(documentDate('2026-11-27', '2026-11-27')).toBeNull()
    expect(documentDate('2026-11-20', '2026-11-27')).toBe('2026-11-20T12:00:00+07:00')
  })

  it('reads dates in Vietnam time', () => {
    expect(vnDate('2026-11-26T18:30:00Z')).toBe('2026-11-27')
  })

  it('sends only the fields the document type uses', () => {
    const draft = { ...emptyDraft('DisposalIssue', 'w1', '2026-11-27'), supplierId: 's1', toWarehouseId: 'w2', lines: [line()] }
    const input = toInput(draft, '2026-11-27')
    expect(input.supplierId).toBeNull()
    expect(input.toWarehouseId).toBeNull()
    expect(input.lines[0].unitPrice).toBe(0) // disposal is valued at moving-average cost by the server
  })

  it('keeps the purchase price on receipts', () => {
    const draft = { ...emptyDraft('PurchaseReceipt', 'w1', '2026-11-27'), supplierId: 's1', lines: [line()] }
    expect(toInput(draft, '2026-11-27')).toMatchObject({ supplierId: 's1', lines: [{ unitPrice: 30_000 }] })
  })

  it('explains what is missing', () => {
    expect(validateDraft(emptyDraft('PurchaseReceipt', 'w1', 'd'))).toMatch(/chưa có dòng hàng/)
    expect(validateDraft({ ...emptyDraft('Transfer', 'w1', 'd'), lines: [line()] })).toBe('Chọn kho đến.')
    expect(validateDraft({ ...emptyDraft('Transfer', 'w1', 'd'), toWarehouseId: 'w1', lines: [line()] })).toMatch(/khác kho đi/)
    expect(validateDraft({ ...emptyDraft('InternalIssue', 'w1', 'd'), lines: [line({ quantity: 0 })] })).toMatch(/Sữa tươi/)
    expect(validateDraft({ ...emptyDraft('Stocktake', 'w1', 'd'), lines: [line(), line({ key: 'k2' })] })).toMatch(/trùng/)
    expect(validateDraft({ ...emptyDraft('PurchaseReceipt', 'w1', 'd'), lines: [line()] })).toBeNull()
  })

  it('rounds the displayed line amount to whole đồng', () => {
    expect(lineAmount({ quantity: 1.5, unitPrice: 33_333 })).toBe(50_000)
  })
})

describe('document status', () => {
  it('describes transfers by where the goods are', () => {
    expect(documentStatusView({ status: 'Posted', transferState: 'InTransit' })).toEqual({ label: 'Đang chuyển', tone: 'lane' })
    expect(documentStatusView({ status: 'Posted', transferState: 'Received' }).label).toBe('Đã nhận')
    expect(documentStatusView({ status: 'Posted', transferState: 'None' }).label).toBe('Đã ghi sổ')
    expect(documentStatusView({ status: 'Draft', transferState: 'None' }).label).toBe('Nháp')
    expect(documentStatusView({ status: 'Cancelled', transferState: 'InTransit' }).label).toBe('Đã hủy')
  })
})
