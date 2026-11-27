
using Cenvo.Modules.Cafe.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cenvo.Api.Hubs;

/// <summary>Clients listen for "ordersChanged" and reload the kitchen queue / floor plan.</summary>
[Authorize]
public class KitchenHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var branch = Context.User?.FindFirst(CenvoClaims.Branch)?.Value;
        if (branch != null) await Groups.AddToGroupAsync(Context.ConnectionId, Group(Guid.Parse(branch)));
        await base.OnConnectedAsync();
    }

    public static string Group(Guid branchId) => $"branch:{branchId}";
}

public class KitchenNotifier(IHubContext<KitchenHub> hub) : IKitchenNotifier
{
    public Task OrdersChanged(Guid branchId, CancellationToken ct = default) =>
        hub.Clients.Group(KitchenHub.Group(branchId)).SendAsync("ordersChanged", ct);
}
