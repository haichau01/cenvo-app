namespace Cenvo.Modules.Cafe.Domain;

public class CafeOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    /// <summary>Name and price captured at order time — reprinting an old receipt is unaffected by menu changes.</summary>
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
    [Precision(18, 3)] public decimal Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public KitchenStatus KitchenStatus { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int SortOrder { get; set; }
    public List<CafeOrderLineTopping> Toppings { get; set; } = [];

    public bool IsBeingPrepared => KitchenStatus >= KitchenStatus.Preparing;

    public static CafeOrderLine Create(Guid orderId, MenuPrice item, decimal quantity,
        IEnumerable<CafeOrderLineTopping> toppings, string? note, int sortOrder)
    {
        var line = new CafeOrderLine
        {
            OrderId = orderId, ProductId = item.ProductId, Name = item.Name, UnitPrice = item.Price,
            Quantity = quantity, Note = note, SortOrder = sortOrder,
        };
        foreach (var topping in toppings)
        {
            topping.LineId = line.Id;
            line.Toppings.Add(topping);
        }
        return line;
    }

    /// <summary>Mergeable when same item, same toppings, same note, not yet sent to the kitchen and no line discount.</summary>
    public bool CanMergeWith(Guid productId, IEnumerable<CafeOrderLineTopping> toppings, string? note) =>
        ProductId == productId && KitchenStatus == KitchenStatus.New && DiscountAmount == 0 && Note == note
        && ToppingKey(Toppings) == ToppingKey(toppings);

    /// <summary>Items sold, for inventory: the item + toppings (toppings multiplied by the number of cups).</summary>
    public IEnumerable<SoldItem> SoldItems() =>
        Toppings.Select(t => new SoldItem(t.ProductId, t.Quantity * Quantity)).Prepend(new SoldItem(ProductId, Quantity));

    public void Recalculate()
    {
        var gross = (UnitPrice + Toppings.Sum(t => t.UnitPrice * t.Quantity)) * Quantity;
        DiscountAmount = Math.Clamp(DiscountAmount, 0, gross);
        Amount = gross - DiscountAmount;
    }

    // ---------------- Kitchen ----------------
    internal void SendToKitchen(DateTime now)
    {
        if (KitchenStatus != KitchenStatus.New) return; // already sent earlier
        KitchenStatus = KitchenStatus.Sent;
        SentAt = now;
    }

    /// <summary>Paid without ever being sent (e.g. bottled water): nothing left to prepare.</summary>
    internal void MarkServedWithoutKitchen()
    {
        if (KitchenStatus == KitchenStatus.New) KitchenStatus = KitchenStatus.Served;
    }

    internal void MoveInKitchen(KitchenStatus target)
    {
        if (!KitchenFlow.CanMove(KitchenStatus, target)) throw new BusinessException(CafeErrors.KitchenMoveNotAllowed(KitchenStatus, target));
        KitchenStatus = target;
    }

    private static string ToppingKey(IEnumerable<CafeOrderLineTopping> toppings) =>
        string.Join(",", toppings.OrderBy(t => t.ProductId).Select(t => $"{t.ProductId}:{t.Quantity}"));
}
