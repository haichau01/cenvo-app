using Cenvo.Modules.CarWash.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Cenvo.Api.Hubs;

/// <summary>Clients listen for "ticketsChanged" and reload the car wash queue.</summary>
public class WashHub : BranchHub;

public class WashNotifier(IHubContext<WashHub> hub) : IWashNotifier
{
    public Task TicketsChanged(Guid branchId, CancellationToken ct = default) =>
        hub.Clients.Group(BranchHub.Group(branchId)).SendAsync("ticketsChanged", ct);
}
