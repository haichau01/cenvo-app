using Cenvo.Modules.Cafe.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Cenvo.Api.Hubs;

/// <summary>Clients listen for "ordersChanged" and reload the kitchen queue / floor plan.</summary>
public class KitchenHub : BranchHub;

public class KitchenNotifier(IHubContext<KitchenHub> hub) : IKitchenNotifier
{
    public Task OrdersChanged(Guid branchId, CancellationToken ct = default) =>
        hub.Clients.Group(BranchHub.Group(branchId)).SendAsync("ordersChanged", ct);
}
