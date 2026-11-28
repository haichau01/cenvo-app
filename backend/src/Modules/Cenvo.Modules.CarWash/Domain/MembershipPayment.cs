namespace Cenvo.Modules.CarWash.Domain;

public class MembershipPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembershipId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}
