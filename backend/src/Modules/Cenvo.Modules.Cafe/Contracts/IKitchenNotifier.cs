namespace Cenvo.Modules.Cafe.Contracts;

/// <summary>Real-time "order changed" signal for the kitchen display / floor plan (the Api implements it with SignalR). Call AFTER saving.</summary>
public interface IKitchenNotifier
{
    Task OrdersChanged(Guid branchId, CancellationToken ct = default);
}
