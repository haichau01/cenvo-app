namespace Cenvo.Modules.Cafe.Domain;

/// <summary>Cafe POS order. Every change goes through a method to keep totals and status consistent.</summary>
public class CafeOrder : BaseEntity, IBranchScoped
{
    /// <summary>RefType used when posting stock (the sale issue document references the order).</summary>
    public const string StockRefType = "CafeOrder";
    public const string CodePrefix = "HD";

    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public OrderType Type { get; set; }
    public Guid? TableId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public Guid? ShiftId { get; set; }
    public string? CustomerName { get; set; }
    public string? Note { get; set; }
    public decimal Subtotal { get; set; }
    /// <summary>Order-level discount (as an amount of money).</summary>
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<CafeOrderLine> Lines { get; set; } = [];
    public List<CafePayment> Payments { get; set; } = [];
    /// <summary>Optimistic concurrency — prevents two terminals from paying/editing the same order.</summary>
    [System.ComponentModel.DataAnnotations.Timestamp] public uint Version { get; set; }

    public static CafeOrder Open(Guid branchId, string code, OrderType type, Guid? tableId, string? customerName)
    {
        if (type == OrderType.DineIn && tableId == null) throw new BusinessException(CafeErrors.TableRequired);
        return new CafeOrder
        {
            BranchId = branchId, Code = code, Type = type, CustomerName = customerName,
            TableId = type == OrderType.DineIn ? tableId : null,
        };
    }

    // ---------------- Editing items ----------------
    public void AddLine(MenuPrice item, decimal quantity, IReadOnlyList<CafeOrderLineTopping> toppings, string? note)
    {
        EnsureOpen();
        EnsurePositive(quantity);
        var mergeable = Lines.FirstOrDefault(l => l.CanMergeWith(item.ProductId, toppings, note));
        if (mergeable != null) mergeable.Quantity += quantity;
        else Lines.Add(CafeOrderLine.Create(Id, item, quantity, toppings, note, NextSortOrder()));
        Recalculate();
    }

    public void UpdateLine(Guid lineId, decimal quantity, string? note, decimal discountAmount)
    {
        EnsureOpen();
        EnsurePositive(quantity);
        if (discountAmount < 0) throw new BusinessException(CafeErrors.InvalidDiscount);
        var line = FindLine(lineId);
        line.Quantity = quantity;
        line.Note = note;
        line.DiscountAmount = discountAmount;
        Recalculate();
    }

    public void RemoveLine(Guid lineId, bool byManager)
    {
        EnsureOpen();
        var line = FindLine(lineId);
        if (line.IsBeingPrepared && !byManager) throw new BusinessException(CafeErrors.PreparingNeedsManager);
        Lines.Remove(line);
        Recalculate();
    }

    public void ApplyDiscount(decimal? amount, decimal? percent)
    {
        EnsureOpen();
        if (percent is < 0 or > 100) throw new BusinessException(CafeErrors.InvalidDiscountPercent);
        Recalculate();
        DiscountAmount = percent != null ? Math.Round(Subtotal * percent.Value / 100, 0) : Math.Max(0, amount ?? 0);
        Recalculate();
    }

    public void UpdateInfo(string? customerName, string? note)
    {
        EnsureOpen();
        CustomerName = customerName;
        Note = note;
    }

    // ---------------- Tables ----------------
    public void MoveToTable(Guid tableId)
    {
        EnsureOpen();
        TableId = tableId;
        Type = OrderType.DineIn;
    }

    public bool IncludesAllLines(IReadOnlyCollection<Guid>? lineIds) =>
        lineIds == null || lineIds.Count == 0 || Lines.All(l => lineIds.Contains(l.Id));

    /// <summary>Split/merge tables: move lines to another order. An order left without lines cancels itself.</summary>
    public void MoveLinesTo(CafeOrder target, IReadOnlyCollection<Guid>? lineIds, DateTime now)
    {
        EnsureOpen();
        target.EnsureOpen();
        if (target.Id == Id) throw new BusinessException(CafeErrors.MoveToSameOrder);

        var moving = IncludesAllLines(lineIds) ? Lines.ToList() : Lines.Where(l => lineIds!.Contains(l.Id)).ToList();
        foreach (var line in moving)
        {
            Lines.Remove(line);
            line.OrderId = target.Id;
            target.Lines.Add(line);
        }
        Recalculate();
        target.Recalculate();
        if (Lines.Count == 0) MarkCancelled($"Gộp vào {target.Code}", now);
    }

    // ---------------- Kitchen ----------------
    public void SendToKitchen(DateTime now)
    {
        EnsureOpen();
        foreach (var line in Lines.Where(l => l.KitchenStatus == KitchenStatus.New))
        {
            line.KitchenStatus = KitchenStatus.Sent;
            line.SentAt = now;
        }
    }

    // ---------------- Payment & cancellation ----------------
    /// <summary>Takes payment → raises <see cref="CafeOrderPaid"/> so inventory deducts ingredients in the same transaction.</summary>
    public void Pay(Guid shiftId, Guid? paidBy, IReadOnlyCollection<TenderedPayment> tendered, DateTime now)
    {
        EnsureOpen();
        if (Lines.Count == 0) throw new BusinessException(CafeErrors.EmptyOrder);
        Recalculate();

        var result = PaymentCalculator.Calculate(Total, tendered);
        Payments.Clear();
        Payments.AddRange(result.Recorded.Select(p =>
            new CafePayment { OrderId = Id, Method = p.Method, Amount = p.Amount, Reference = p.Reference }));
        CashReceived = result.CashReceived;
        ChangeAmount = result.Change;

        Status = OrderStatus.Paid;
        PaidAt = now;
        PaidBy = paidBy;
        ShiftId = shiftId;
        foreach (var line in Lines.Where(l => l.KitchenStatus == KitchenStatus.New)) line.KitchenStatus = KitchenStatus.Served;

        Raise(new CafeOrderPaid(Id, BranchId, Code, Lines.SelectMany(l => l.SoldItems()).ToList()));
    }

    /// <summary>Cancels the order. A paid order needs a manager and raises <see cref="CafeOrderCancelled"/> to restock.</summary>
    public void Cancel(string reason, bool byManager, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(CafeErrors.CancelReasonRequired);
        if (Status == OrderStatus.Cancelled) throw new BusinessException(CafeErrors.OrderAlreadyCancelled);

        if (Status == OrderStatus.Paid)
        {
            if (!byManager) throw new BusinessException(CafeErrors.PaidCancelNeedsManager);
            Raise(new CafeOrderCancelled(Id, Code, reason));
        }
        else if (Lines.Any(l => l.IsBeingPrepared) && !byManager)
        {
            throw new BusinessException(CafeErrors.PreparingNeedsManager);
        }
        MarkCancelled(reason, now);
    }

    public void Recalculate()
    {
        foreach (var line in Lines) line.Recalculate();
        Subtotal = Lines.Sum(l => l.Amount);
        DiscountAmount = Math.Min(DiscountAmount, Subtotal);
        Total = Subtotal - DiscountAmount;
    }

    private void MarkCancelled(string reason, DateTime now)
    {
        Status = OrderStatus.Cancelled;
        CancelledAt = now;
        CancelReason = reason;
    }

    private void EnsureOpen()
    {
        if (Status != OrderStatus.Open) throw new BusinessException(CafeErrors.OrderClosed);
    }

    private static void EnsurePositive(decimal quantity)
    {
        if (quantity <= 0) throw new BusinessException(CafeErrors.QuantityMustBePositive);
    }

    private CafeOrderLine FindLine(Guid lineId) =>
        Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new NotFoundException("dòng món", lineId);

    private int NextSortOrder() => Lines.Count == 0 ? 1 : Lines.Max(l => l.SortOrder) + 1;
}
