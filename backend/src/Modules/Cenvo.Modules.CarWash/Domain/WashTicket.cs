using System.ComponentModel.DataAnnotations;

namespace Cenvo.Modules.CarWash.Domain;

/// <summary>
/// Car wash ticket (phiếu rửa xe): Waiting → Washing → Done → Delivered, paid at the POS inside a cashier shift.
/// Every change goes through a method so totals, status and commission stay consistent.
/// </summary>
public class WashTicket : BaseEntity, IBranchScoped
{
    /// <summary>RefType used when posting stock (the sale issue document references the ticket).</summary>
    public const string StockRefType = "WashTicket";
    public const string CodePrefix = "RX";
    public const int MaxPhotos = 8;

    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public Guid VehicleId { get; set; }
    public string Plate { get; set; } = "";
    public VehicleType VehicleType { get; set; }
    /// <summary>Make / model / colour as noted at intake.</summary>
    public string? VehicleDescription { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Note { get; set; }
    public WashStatus Status { get; set; } = WashStatus.Waiting;
    /// <summary>Sum of list prices.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Part of the subtotal covered by memberships.</summary>
    public decimal MembershipCovered { get; set; }
    public decimal Total { get; set; }
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<WashTicketLine> Lines { get; set; } = [];
    public List<WashTicketStaff> Staff { get; set; } = [];
    public List<WashPayment> Payments { get; set; } = [];
    public List<WashTicketPhoto> Photos { get; set; } = [];
    /// <summary>Optimistic concurrency — two tablets must not move or pay the same ticket at once.</summary>
    [Timestamp] public uint Version { get; set; }

    public bool IsPaid => PaidAt != null;

    /// <summary>Services and staff can change only before the wash is done and before payment.</summary>
    public bool IsEditable => (Status is WashStatus.Waiting or WashStatus.Washing) && !IsPaid;

    /// <param name="walkInName">Name typed for a customer without a phone number (no customer record is kept).</param>
    public static WashTicket Open(Guid branchId, string code, Vehicle vehicle, Customer? customer, string? walkInName, string? note) =>
        new()
        {
            BranchId = branchId, Code = code, VehicleId = vehicle.Id, Plate = vehicle.Plate, VehicleType = vehicle.VehicleType,
            VehicleDescription = vehicle.Description,
            CustomerId = customer?.Id, CustomerName = customer?.Name ?? Clean(walkInName), CustomerPhone = customer?.Phone,
            Note = Clean(note),
        };

    // ---------------- Services & staff ----------------
    public void AddService(WashOffering offering, Guid? membershipId)
    {
        EnsureEditable();
        if (!offering.IsActive) throw new BusinessException(CarWashErrors.ServiceUnavailable);
        if (Lines.Any(l => l.OfferingId == offering.Id)) throw new BusinessException(CarWashErrors.ServiceAlreadyOnTicket);
        if (membershipId != null && CustomerId == null) throw new BusinessException(CarWashErrors.MembershipNeedsCustomer);

        Lines.Add(WashTicketLine.Create(Id, offering, offering.PriceFor(VehicleType), membershipId, NextSortOrder()));
        Recalculate();
    }

    /// <returns>The removed line — its membership use must be given back by the caller.</returns>
    public WashTicketLine RemoveService(Guid lineId)
    {
        EnsureEditable();
        var line = Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new NotFoundException("dịch vụ trên phiếu", lineId);
        Lines.Remove(line);
        Recalculate();
        return line;
    }

    public void AssignStaff(IReadOnlyCollection<StaffMember> staff)
    {
        EnsureEditable();
        Staff.RemoveAll(s => staff.All(w => w.UserId != s.UserId));
        Staff.AddRange(staff.Where(w => Staff.All(s => s.UserId != w.UserId))
            .Select(w => new WashTicketStaff { TicketId = Id, UserId = w.UserId, FullName = w.FullName }));
    }

    // ---------------- Washing flow ----------------
    public void Start(DateTime now)
    {
        EnsureNotCancelled();
        if (Status != WashStatus.Waiting) throw new BusinessException(CarWashErrors.OnlyWaitingCanStart);
        if (Lines.Count == 0) throw new BusinessException(CarWashErrors.ServicesRequired);
        if (Staff.Count == 0) throw new BusinessException(CarWashErrors.StaffRequired);
        Status = WashStatus.Washing;
        StartedAt = now;
    }

    /// <summary>Undo a mistaken "start" — nothing has been posted yet.</summary>
    public void ReturnToWaiting()
    {
        if (Status != WashStatus.Washing) throw new BusinessException(CarWashErrors.OnlyWashingCanReturn);
        Status = WashStatus.Waiting;
        StartedAt = null;
    }

    /// <summary>Wash done → commission is split equally between the staff, and <see cref="WashTicketCompleted"/> deducts supplies.</summary>
    public void Complete(DateTime now)
    {
        if (Status != WashStatus.Washing) throw new BusinessException(CarWashErrors.OnlyWashingCanComplete);
        if (Staff.Count == 0) throw new BusinessException(CarWashErrors.StaffRequired);

        var shares = CommissionCalculator.SplitEqually(Lines.Sum(l => l.Commission()), Staff.Count);
        foreach (var (member, share) in Staff.Zip(shares)) member.Commission = share;

        Status = WashStatus.Done;
        CompletedAt = now;
        Raise(new WashTicketCompleted(Id, BranchId, Code, VehicleType, Lines.Select(l => l.OfferingId).ToList(),
            StartedAt ?? now, now, Staff.Select(s => new StaffCommission(s.UserId, s.FullName, s.Commission)).ToList()));
    }

    public void Deliver(DateTime now)
    {
        if (Status != WashStatus.Done) throw new BusinessException(CarWashErrors.OnlyDoneCanDeliver);
        if (!IsPaid) throw new BusinessException(CarWashErrors.DeliverNeedsPayment);
        Status = WashStatus.Delivered;
        DeliveredAt = now;
    }

    // ---------------- Payment & cancellation ----------------
    /// <summary>Takes payment inside the cashier's shift. Allowed before or after washing (some customers pay first).</summary>
    public void Pay(Guid shiftId, Guid? paidBy, IReadOnlyCollection<TenderedPayment> tendered, DateTime now)
    {
        EnsureNotCancelled();
        if (IsPaid) throw new BusinessException(CarWashErrors.AlreadyPaid);
        if (Lines.Count == 0) throw new BusinessException(CarWashErrors.ServicesRequired);
        Recalculate();

        var result = PaymentCalculator.Calculate(Total, tendered);
        Payments.Clear();
        Payments.AddRange(result.Recorded.Select(p =>
            new WashPayment { TicketId = Id, Method = p.Method, Amount = p.Amount, Reference = p.Reference }));
        CashReceived = result.CashReceived;
        ChangeAmount = result.Change;
        ShiftId = shiftId;
        PaidAt = now;
        PaidBy = paidBy;
    }

    /// <summary>
    /// Cancels the ticket. Once washed or paid a manager is needed; a washed ticket raises
    /// <see cref="WashTicketCancelled"/> so supplies go back to stock. Membership uses are given back by the caller.
    /// </summary>
    public void Cancel(string reason, bool byManager, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(CarWashErrors.CancelReasonRequired);
        EnsureNotCancelled();
        var wasCompleted = Status is WashStatus.Done or WashStatus.Delivered;
        if ((wasCompleted || IsPaid) && !byManager) throw new BusinessException(CarWashErrors.CancelNeedsManager);

        Status = WashStatus.Cancelled;
        CancelledAt = now;
        CancelReason = reason.Trim();
        Raise(new WashTicketCancelled(Id, Code, CancelReason, wasCompleted));
    }

    // ---------------- Photos ----------------
    public void AddPhoto(WashTicketPhoto photo)
    {
        EnsureNotCancelled();
        if (Photos.Count >= MaxPhotos) throw new BusinessException(CarWashErrors.TooManyPhotos);
        photo.TicketId = Id;
        Photos.Add(photo);
    }

    /// <summary>Condition photos are evidence: they can only be removed while the vehicle is still being handled.</summary>
    public WashTicketPhoto RemovePhoto(Guid photoId)
    {
        if (Status is not (WashStatus.Waiting or WashStatus.Washing)) throw new BusinessException(CarWashErrors.PhotoLocked);
        var photo = Photos.FirstOrDefault(p => p.Id == photoId) ?? throw new NotFoundException("ảnh", photoId);
        Photos.Remove(photo);
        return photo;
    }

    public void Recalculate()
    {
        Subtotal = Lines.Sum(l => l.Price);
        Total = Lines.Sum(l => l.Amount);
        MembershipCovered = Subtotal - Total;
    }

    private void EnsureEditable()
    {
        EnsureNotCancelled();
        if (!IsEditable) throw new BusinessException(CarWashErrors.TicketNotEditable);
    }

    private void EnsureNotCancelled()
    {
        if (Status == WashStatus.Cancelled) throw new BusinessException(CarWashErrors.TicketCancelled);
    }

    private int NextSortOrder() => Lines.Count == 0 ? 1 : Lines.Max(l => l.SortOrder) + 1;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
