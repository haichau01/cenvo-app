namespace Cenvo.Modules.Inventory.Application.Documents;

public class StockDocumentQueryService(AppDbContext db, ICurrentUser user)
{
    private record Header(Guid Id, string Code, StockDocumentType Type, DocumentStatus Status, DateTime Date, Guid WarehouseId,
        Guid? ToWarehouseId, TransferState TransferState, Guid? SupplierId, string? Note, string? RefType, string? RefCode,
        string? CancelReason, decimal TotalAmount, DateTime CreatedAt);

    public async Task<List<StockDocumentDto>> List(StockDocumentFilter filter, CancellationToken ct)
    {
        var query = db.Set<StockDocument>().AsQueryable();
        if (filter.Type != null) query = query.Where(d => d.Type == filter.Type);
        if (filter.Status != null) query = query.Where(d => d.Status == filter.Status);
        if (filter.From != null)
        {
            var from = VnTime.StartOfDayUtc(filter.From.Value);
            query = query.Where(d => d.Date >= from);
        }
        if (filter.To != null)
        {
            var to = VnTime.StartOfDayUtc(filter.To.Value.AddDays(1));
            query = query.Where(d => d.Date < to);
        }
        return await ToDtos(query.OrderByDescending(d => d.Date).Take(Limits.MaxListSize), ct);
    }

    /// <summary>Transfer documents waiting for the current branch to confirm receipt.</summary>
    public async Task<List<StockDocumentDto>> IncomingTransfers(CancellationToken ct)
    {
        var myWarehouses = await MyWarehouseIds(ct);
        var query = db.Set<StockDocument>()
            .IgnoreQueryFilters() // documents belong to the sending branch; limited to our own destination warehouses
            .Where(d => d.Type == StockDocumentType.Transfer && d.Status == DocumentStatus.Posted
                        && d.TransferState == TransferState.InTransit && myWarehouses.Contains(d.ToWarehouseId!.Value))
            .OrderBy(d => d.Date);
        return await ToDtos(query, ct);
    }

    public async Task<StockDocumentDto> Get(Guid id, CancellationToken ct)
    {
        var myWarehouses = await MyWarehouseIds(ct);
        var doc = await db.Set<StockDocument>()
                      .IgnoreQueryFilters() // the receiving branch can also view transfers addressed to it
                      .AsNoTracking().Include(d => d.Lines)
                      .Where(d => d.Id == id && (user.IsAllBranches || d.BranchId == user.BranchId
                                  || (d.ToWarehouseId != null && myWarehouses.Contains(d.ToWarehouseId.Value))))
                      .FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("chứng từ", id);

        var header = (await ToDtos(db.Set<StockDocument>().IgnoreQueryFilters().Where(d => d.Id == doc.Id), ct)).Single();
        return header with { Lines = await LineDtos(doc.Lines, ct) };
    }

    private async Task<List<StockDocumentDto>> ToDtos(IQueryable<StockDocument> query, CancellationToken ct)
    {
        var headers = await query.AsNoTracking().Select(d => new Header(d.Id, d.Code, d.Type, d.Status, d.Date, d.WarehouseId,
            d.ToWarehouseId, d.TransferState, d.SupplierId, d.Note, d.RefType, d.RefCode, d.CancelReason,
            d.Lines.Sum(l => l.Amount), d.CreatedAt)).ToListAsync(ct);

        var warehouseNames = await WarehouseNames(headers.SelectMany(h => new[] { h.WarehouseId, h.ToWarehouseId ?? Guid.Empty }), ct);
        var supplierIds = headers.Where(h => h.SupplierId != null).Select(h => h.SupplierId!.Value).Distinct().ToList();
        var supplierNames = await db.Set<Supplier>().IgnoreQueryFilters().Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return headers.Select(h => new StockDocumentDto(h.Id, h.Code, h.Type, h.Status, h.Date, h.WarehouseId,
            warehouseNames.GetValueOrDefault(h.WarehouseId, ""), h.ToWarehouseId,
            h.ToWarehouseId == null ? null : warehouseNames.GetValueOrDefault(h.ToWarehouseId.Value),
            h.TransferState, h.SupplierId, h.SupplierId == null ? null : supplierNames.GetValueOrDefault(h.SupplierId.Value),
            h.Note, h.RefType, h.RefCode, h.CancelReason, h.TotalAmount, h.CreatedAt, null)).ToList();
    }

    private async Task<List<StockDocumentLineDto>> LineDtos(List<StockDocumentLine> lines, CancellationToken ct)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Set<Product>().IgnoreQueryFilters().Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new { p.Code, p.Name, p.BaseUnitId }, ct);
        var unitIds = lines.Select(l => l.UnitId ?? products[l.ProductId].BaseUnitId).Distinct().ToList();
        var unitNames = await db.Set<Unit>().IgnoreQueryFilters().Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return lines.Select(l =>
        {
            var product = products[l.ProductId];
            return new StockDocumentLineDto(l.Id, l.ProductId, product.Code, product.Name, l.UnitId,
                unitNames.GetValueOrDefault(l.UnitId ?? product.BaseUnitId, ""), l.Quantity, l.BaseQuantity,
                l.SystemQuantity, l.UnitPrice, l.Amount, l.Note);
        }).ToList();
    }

    private Task<List<Guid>> MyWarehouseIds(CancellationToken ct) =>
        db.Set<Warehouse>().Select(w => w.Id).ToListAsync(ct);

    private Task<Dictionary<Guid, string>> WarehouseNames(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        return db.Set<Warehouse>().IgnoreQueryFilters() // other branches' warehouses (transfers) — names only
            .Where(w => list.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, ct);
    }
}
