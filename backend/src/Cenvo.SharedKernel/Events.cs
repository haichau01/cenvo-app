namespace Cenvo.SharedKernel;

public interface IDomainEvent;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent e, CancellationToken ct);
}

/// <summary>Dispatches events synchronously within the same transaction (in-process).</summary>
public interface IDomainEventDispatcher
{
    Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct);
}
