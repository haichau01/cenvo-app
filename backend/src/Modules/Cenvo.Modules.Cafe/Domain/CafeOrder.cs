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
    /// <summary>Set when the order discount was given as a percentage: it follows the subtotal when items are added, removed or split off.</summary>
    public decimal? DiscountPercent { get; set; }
    public string? DiscountReason { get; set; }
    /// <summary>Who last changed a discount on this order.</summary>
    public Guid? DiscountBy { get; set; }
    public string? DiscountByName { get; set; }
    /// <summary>Manager who approved the discounts above the cashier's limit (null = within the limit).</summary>
    public Guid? DiscountApprovedBy { get; set; }
    public string? DiscountApprovedByName { get; set; }
    /// <summary>Gross and total discount the manager saw when approving: the approval covers that share of the bill.</summary>
    public decimal? DiscountApprovedGross { get; set; }
    public decimal? DiscountApprovedTotal { get; set; }
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

    public void UpdateLine(Guid lineId, decimal quantity, string? note)
    {
        EnsureOpen();
        EnsurePositive(quantity);
        var line = FindLine(lineId);
        line.Quantity = quantity;
        line.Note = note;
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

    // ---------------- Discounts ----------------
    /// <summary>Whole-order discount by amount or percent of the subtotal. A reason is required; above the cashier's limit a manager must approve.</summary>
    public void ApplyDiscount(decimal? amount, decimal? percent, string? reason, Guid? byUserId, string byName, DiscountApproval? approval)
    {
        EnsureOpen();
        if (percent is < 0 or > 100) throw new BusinessException(CafeErrors.InvalidDiscountPercent);
        if (amount < 0) throw new BusinessException(CafeErrors.InvalidDiscount);
        Recalculate();
        DiscountPercent = percent > 0 ? percent : null;
        DiscountAmount = percent != null ? DiscountPolicy.PercentOf(Subtotal, percent.Value) : amount ?? 0;
        DiscountReason = DiscountAmount > 0 ? Clean(reason) : null;
        Recalculate();
        RecordDiscount(DiscountAmount, reason, byUserId, byName, approval);
    }

    /// <summary>Discount on one item (e.g. a drink made wrong). Same rules as the order discount.</summary>
    public void DiscountLine(Guid lineId, decimal amount, string? reason, Guid? byUserId, string byName, DiscountApproval? approval)
    {
        EnsureOpen();
        if (amount < 0) throw new BusinessException(CafeErrors.InvalidDiscount);
        FindLine(lineId).SetDiscount(amount, Clean(reason));
        Recalculate();
        RecordDiscount(amount, reason, byUserId, byName, approval);
    }

    /// <summary>Total of every discount on the order (order-level + items).</summary>
    public decimal TotalDiscount => DiscountAmount + Lines.Sum(l => l.DiscountAmount);

    /// <summary>Price of every item before any discount.</summary>
    public decimal GrossAmount => Lines.Sum(l => l.GrossAmount);

    /// <summary>Above the cashier's limit with no manager approval (checked again at payment).</summary>
    public bool DiscountNeedsApproval => DiscountPolicy.NeedsManager(GrossAmount, TotalDiscount) && !ApprovalCoversDiscount;

    private bool ApprovalCoversDiscount => DiscountApprovedBy != null
        && DiscountPolicy.WithinApproved(GrossAmount, TotalDiscount, DiscountApprovedGross!.Value, DiscountApprovedTotal!.Value);

    /// <summary>
    /// Every discount change re-checks the whole order against the cashier's limit: an earlier approval does not
    /// cover a bigger share of the bill given later (or left after removing items), but still covers a smaller one.
    /// </summary>
    private void RecordDiscount(decimal amount, string? reason, Guid? byUserId, string byName, DiscountApproval? approval)
    {
        if (amount > 0 && string.IsNullOrWhiteSpace(reason)) throw new BusinessException(CafeErrors.DiscountReasonRequired);
        DiscountBy = byUserId;
        DiscountByName = byName;

        if (!DiscountPolicy.NeedsManager(GrossAmount, TotalDiscount))
        {
            SetApprover(null);
            return;
        }
        if (approval != null) SetApprover(approval);
        else if (!ApprovalCoversDiscount)
            throw new BusinessException(CafeErrors.DiscountNeedsManager(DiscountPolicy.CashierLimit(GrossAmount)));
    }

    private void SetApprover(DiscountApproval? approval)
    {
        DiscountApprovedBy = approval?.ManagerId;
        DiscountApprovedByName = approval?.ManagerName;
        DiscountApprovedGross = approval == null ? null : GrossAmount;
        DiscountApprovedTotal = approval == null ? null : TotalDiscount;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

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
        foreach (var line in Lines) line.SendToKitchen(now);
    }

    /// <summary>The kitchen moves one item along (Preparing, Done, Served). Paid orders are still prepared.</summary>
    public void ChangeKitchenStatus(Guid lineId, KitchenStatus status)
    {
        if (Status == OrderStatus.Cancelled) throw new BusinessException(CafeErrors.OrderAlreadyCancelled);
        FindLine(lineId).MoveInKitchen(status);
    }

    // ---------------- Payment & cancellation ----------------
    /// <summary>Takes payment → raises <see cref="CafeOrderPaid"/> so inventory deducts ingredients in the same transaction.</summary>
    public void Pay(Guid shiftId, Guid? paidBy, IReadOnlyCollection<TenderedPayment> tendered, DateTime now)
    {
        EnsureOpen();
        if (Lines.Count == 0) throw new BusinessException(CafeErrors.EmptyOrder);
        // Items removed or merged after a discount can push it over the limit without anyone approving.
        if (DiscountNeedsApproval) throw new BusinessException(CafeErrors.DiscountNeedsManager(DiscountPolicy.CashierLimit(GrossAmount)));
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
        foreach (var line in Lines) line.MarkServedWithoutKitchen();

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
        if (DiscountPercent != null) DiscountAmount = DiscountPolicy.PercentOf(Subtotal, DiscountPercent.Value);
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
