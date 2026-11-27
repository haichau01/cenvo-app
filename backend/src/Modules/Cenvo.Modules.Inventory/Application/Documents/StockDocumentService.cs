namespace Cenvo.Modules.Inventory.Application.Documents;

/// <summary>Lifecycle of manual stock documents: draft → post (write ledger) → cancel (reversal); transfers add a receive step.</summary>
public class StockDocumentService(
    AppDbContext db,
    StockService stock,
    IEnumerable<IStockPostingStrategy> postingStrategies,
    IUnitConverterFactory converters,
    IBranchStockPolicy branchPolicy,
    IDocumentNumberGenerator numbers,
    ICurrentUser user)
{
    public async Task<Guid> CreateDraft(StockDocumentInput input, CancellationToken ct)
    {
        if (!StockDocumentTypes.Manual.Contains(input.Type)) throw new BusinessException(InventoryErrors.NotManualType);
        var branchId = user.BranchId ?? throw new BusinessException(InventoryErrors.NoBranch);
        var date = input.Date?.ToUniversalTime() ?? DateTime.UtcNow;

        var doc = StockDocument.CreateDraft(branchId, await numbers.Next(branchId, input.Type.CodePrefix(), date, ct), input.Type);
        db.Add(doc);
        await Apply(doc, input, date, ct);
        await db.SaveChangesAsync(ct);
        return doc.Id;
    }

    public async Task UpdateDraft(Guid id, StockDocumentInput input, CancellationToken ct)
    {
        var doc = await Load(id, ct);
        if (doc.Type != input.Type) throw new BusinessException(InventoryErrors.TypeCannotChange);
        await Apply(doc, input, input.Date?.ToUniversalTime() ?? doc.Date, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDraft(Guid id, CancellationToken ct)
    {
        var doc = await Load(id, ct);
        if (doc.Status != DocumentStatus.Draft) throw new BusinessException(InventoryErrors.DeleteOnlyDraft);
        db.Remove(doc);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Posts the document → writes the ledger using the strategy for its document type.</summary>
    public async Task<List<StockWarning>> Post(Guid id, CancellationToken ct)
    {
        var doc = await Load(id, ct);
        doc.EnsureDraft();
        var strategy = postingStrategies.FirstOrDefault(s => s.Handles(doc.Type))
                       ?? throw new BusinessException(InventoryErrors.UnsupportedType);

        var allowNegative = await branchPolicy.AllowNegativeStock(doc.BranchId, ct);
        var movements = await strategy.Post(doc, allowNegative, ct);
        doc.MarkPosted();
        await db.SaveChangesAsync(ct);
        return StockWarning.From(movements);
    }

    /// <summary>Transfer step 2: the destination branch confirms and receives stock at the cost captured on issue.</summary>
    public async Task Receive(Guid id, CancellationToken ct)
    {
        var doc = await db.Set<StockDocument>()
                      .IgnoreQueryFilters() // document belongs to the sending branch; receive permission is checked right below
                      .Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id, ct)
                  ?? throw new NotFoundException("chứng từ", id);
        if (!doc.IsAwaitingReceipt) throw new BusinessException(InventoryErrors.NotAwaitingReceipt);

        var destination = await db.Set<Warehouse>().IgnoreQueryFilters().FirstAsync(w => w.Id == doc.ToWarehouseId, ct);
        if (!user.IsAllBranches && destination.BranchId != user.BranchId)
            throw new BusinessException(InventoryErrors.OnlyDestinationReceives);

        var issueCosts = await db.Set<StockMovement>().IgnoreQueryFilters()
            .Where(m => m.DocumentId == doc.Id && m.WarehouseId == doc.WarehouseId && !m.IsReversal)
            .GroupBy(m => m.ProductId).Select(g => new { g.Key, Cost = g.Max(m => m.UnitCost) })
            .ToDictionaryAsync(x => x.Key, x => x.Cost, ct);

        var receipts = doc.Lines.Select(l =>
            new MovementRequest(destination.Id, l.ProductId, l.BaseQuantity, issueCosts.GetValueOrDefault(l.ProductId)));
        await stock.Post(doc, receipts, allowNegative: true, isReversal: false, ct);
        doc.MarkReceived(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task Cancel(Guid id, string reason, CancellationToken ct)
    {
        var doc = await Load(id, ct);
        if (doc.Type == StockDocumentType.SaleIssue) throw new BusinessException(InventoryErrors.SaleIssueCancelFromInvoice);
        doc.MarkCancelled(reason);
        await stock.Reverse(doc, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task Apply(StockDocument doc, StockDocumentInput input, DateTime dateUtc, CancellationToken ct)
    {
        await EnsureWarehouses(input, ct);
        doc.SetHeader(input.WarehouseId, input.ToWarehouseId, input.SupplierId, dateUtc, input.Note);
        doc.ReplaceLines(await BuildLines(input.Lines, ct));
    }

    private async Task EnsureWarehouses(StockDocumentInput input, CancellationToken ct)
    {
        if (!await db.Set<Warehouse>().AnyAsync(w => w.Id == input.WarehouseId, ct))
            throw new BusinessException(InventoryErrors.WarehouseNotInBranch);
        if (input.Type == StockDocumentType.Transfer && input.ToWarehouseId != null &&
            !await db.Set<Warehouse>().IgnoreQueryFilters().AnyAsync(w => w.Id == input.ToWarehouseId && !w.IsDeleted, ct))
            throw new NotFoundException("kho đến", input.ToWarehouseId);
    }

    private async Task<List<StockDocumentLine>> BuildLines(StockDocumentLineInput[] lines, CancellationToken ct)
    {
        var converter = await converters.Create(lines.Select(l => l.ProductId), ct);
        return lines.Select(l =>
        {
            var product = converter.Product(l.ProductId);
            if (!product.TrackStock) throw new BusinessException(InventoryErrors.NotTracked(product.Name));
            var baseQuantity = converter.ToBase(l.ProductId, l.UnitId, l.Quantity);
            return StockDocumentLine.Create(l.ProductId, l.UnitId, l.Quantity, baseQuantity, l.UnitPrice, l.Note);
        }).ToList();
    }

    private async Task<StockDocument> Load(Guid id, CancellationToken ct) =>
        await db.Set<StockDocument>().Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new NotFoundException("chứng từ", id);
}
