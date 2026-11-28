namespace Cenvo.Modules.CarWash.Contracts;

/// <summary>Real-time "wash queue changed" signal for every tablet of the branch (the Api implements it with SignalR). Call AFTER saving.</summary>
public interface IWashNotifier
{
    Task TicketsChanged(Guid branchId, CancellationToken ct = default);
}
