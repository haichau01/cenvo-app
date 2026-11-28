using System.ComponentModel.DataAnnotations;

namespace Cenvo.Modules.CarWash.Domain;

/// <summary>
/// A package sold to a customer: paid at the POS inside a cashier shift, then used one wash at a time at any branch.
/// Money document → never deleted, only cancelled (and only while unused).
/// </summary>
public class CustomerMembership : BaseEntity
{
    public const string CodePrefix = "TV";

    public string Code { get; set; } = "";
    /// <summary>Branch that sold the package (the money belongs to its shift). Not a filter: the package is usable everywhere.</summary>
    public Guid SoldBranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = "";
    public Guid OfferingId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public int TotalUses { get; set; }
    public int RemainingUses { get; set; }
    public decimal Price { get; set; }
    public DateTime ExpiresAt { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public Guid ShiftId { get; set; }
    public DateTime PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public decimal CashReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<MembershipPayment> Payments { get; set; } = [];
    [Timestamp] public uint Version { get; set; }

    public static CustomerMembership Sell(string code, Guid branchId, Guid customerId, MembershipPackage package,
        Guid shiftId, Guid? paidBy, IReadOnlyCollection<TenderedPayment> tendered, DateTime now)
    {
        if (!package.IsActive) throw new BusinessException(CarWashErrors.PackageUnavailable);
        var result = PaymentCalculator.Calculate(package.Price, tendered);
        var membership = new CustomerMembership
        {
            Code = code, SoldBranchId = branchId, CustomerId = customerId,
            PackageId = package.Id, PackageName = package.Name, OfferingId = package.OfferingId, VehicleType = package.VehicleType,
            TotalUses = package.Uses, RemainingUses = package.Uses, Price = package.Price,
            ExpiresAt = now.AddDays(package.ValidDays),
            ShiftId = shiftId, PaidAt = now, PaidBy = paidBy, CashReceived = result.CashReceived, ChangeAmount = result.Change,
        };
        membership.Payments.AddRange(result.Recorded.Select(p => new MembershipPayment
        {
            MembershipId = membership.Id, Method = p.Method, Amount = p.Amount, Reference = p.Reference,
        }));
        return membership;
    }

    public bool CanCover(Guid customerId, Guid offeringId, VehicleType vehicleType, DateTime now) =>
        Status == MembershipStatus.Active
        && RemainingUses > 0
        && ExpiresAt > now
        && CustomerId == customerId
        && OfferingId == offeringId
        && (VehicleType == null || VehicleType == vehicleType);

    public void Use(Guid customerId, Guid offeringId, VehicleType vehicleType, DateTime now)
    {
        if (!CanCover(customerId, offeringId, vehicleType, now)) throw new BusinessException(CarWashErrors.MembershipNotUsable);
        RemainingUses--;
    }

    /// <summary>Gives a use back (the service was removed from the ticket or the ticket was cancelled).</summary>
    public void Restore()
    {
        if (RemainingUses < TotalUses) RemainingUses++;
    }

    public void Cancel(string reason, bool byManager, Guid? currentBranchId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(CarWashErrors.CancelReasonRequired);
        if (!byManager) throw new BusinessException(CarWashErrors.MembershipCancelNeedsManager);
        // The refund comes out of the selling branch's till, so only that branch may cancel.
        if (currentBranchId != SoldBranchId) throw new BusinessException(CarWashErrors.MembershipOtherBranch);
        if (Status == MembershipStatus.Cancelled) throw new BusinessException(CarWashErrors.MembershipCancelled);
        if (RemainingUses < TotalUses) throw new BusinessException(CarWashErrors.MembershipUsedCannotCancel);
        Status = MembershipStatus.Cancelled;
        CancelledAt = now;
        CancelReason = reason.Trim();
    }
}
