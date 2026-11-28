namespace Cenvo.Modules.CarWash.Domain;

/// <summary>One service on a ticket. Name, price and commission rule are captured at intake (old tickets never change).</summary>
public class WashTicketLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid OfferingId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>List price for the vehicle type.</summary>
    public decimal Price { get; set; }
    /// <summary>Membership that covers this service (one use) — the customer then pays nothing for it.</summary>
    public Guid? MembershipId { get; set; }
    /// <summary>Amount the customer pays: the price, or 0 when covered by a membership.</summary>
    public decimal Amount { get; set; }
    public int DurationMinutes { get; set; }
    public CommissionType CommissionType { get; set; }
    public decimal CommissionValue { get; set; }
    public int SortOrder { get; set; }

    public static WashTicketLine Create(Guid ticketId, WashOffering offering, decimal price, Guid? membershipId, int sortOrder) =>
        new()
        {
            TicketId = ticketId, OfferingId = offering.Id, Name = offering.Name, Price = price,
            MembershipId = membershipId, Amount = membershipId == null ? price : 0,
            DurationMinutes = offering.DurationMinutes,
            CommissionType = offering.CommissionType, CommissionValue = offering.CommissionValue,
            SortOrder = sortOrder,
        };

    /// <summary>Commission is earned on the list price, also when a membership covers the service (the work is the same).</summary>
    public decimal Commission() => CommissionCalculator.ForService(Price, CommissionType, CommissionValue);
}
