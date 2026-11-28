using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cenvo.Api.Hubs;

/// <summary>Hub whose clients join their branch group, so a signal only reaches tablets of that branch.</summary>
[Authorize]
public abstract class BranchHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var branch = Context.User?.FindFirst(CenvoClaims.Branch)?.Value;
        if (branch != null) await Groups.AddToGroupAsync(Context.ConnectionId, Group(Guid.Parse(branch)));
        await base.OnConnectedAsync();
    }

    public static string Group(Guid branchId) => $"branch:{branchId}";
}
