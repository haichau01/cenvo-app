using System.Linq.Expressions;

namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Named ticket conditions and the shared history projection.</summary>
public static class TicketQueries
{
    private static readonly WashStatus[] Settled = [WashStatus.Delivered, WashStatus.Cancelled];

    /// <summary>Vehicle still at the shop (waiting, washing or waiting for pickup).</summary>
    public static IQueryable<WashTicket> InProgress(this IQueryable<WashTicket> tickets) =>
        tickets.Where(ticket => !Settled.Contains(ticket.Status));

    /// <summary>The queue board: vehicles still at the shop + those delivered since the given time (today).</summary>
    public static IQueryable<WashTicket> OnQueueBoard(this IQueryable<WashTicket> tickets, DateTime deliveredSinceUtc) =>
        tickets.Where(ticket => !Settled.Contains(ticket.Status)
                                || (ticket.Status == WashStatus.Delivered && ticket.DeliveredAt >= deliveredSinceUtc));

    public static readonly Expression<Func<WashTicket, TicketHistoryDto>> ToHistory = ticket => new TicketHistoryDto
    {
        Id = ticket.Id,
        Code = ticket.Code,
        Plate = ticket.Plate,
        CustomerName = ticket.CustomerName,
        Status = ticket.Status,
        IsPaid = ticket.PaidAt != null,
        Total = ticket.Total,
        Services = ticket.Lines.OrderBy(line => line.SortOrder).Select(line => line.Name).ToList(),
        CreatedAt = ticket.CreatedAt,
    };
}
