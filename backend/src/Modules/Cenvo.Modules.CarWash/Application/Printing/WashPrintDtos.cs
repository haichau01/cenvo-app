using Cenvo.Modules.CarWash.Application.Customers;
using Cenvo.Modules.CarWash.Application.Tickets;

namespace Cenvo.Modules.CarWash.Application.Printing;

/// <summary>
/// What a printed wash slip needs: the ticket plus the shop header and who took the payment.
/// The same data prints as the intake slip (given to the customer at drop-off) or the paid receipt.
/// </summary>
public record WashTicketReceiptDto(TicketDto Ticket, BranchContact Store, string? CashierName);

/// <summary>Receipt of a sold membership package.</summary>
public record MembershipReceiptDto(
    MembershipDto Membership,
    string CustomerName,
    string CustomerPhone,
    List<TicketPaymentDto> Payments,
    decimal CashReceived,
    decimal ChangeAmount,
    BranchContact Store,
    string? CashierName);
