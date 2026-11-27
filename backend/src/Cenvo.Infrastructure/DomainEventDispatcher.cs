using Cenvo.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Cenvo.Infrastructure;

public class DomainEventDispatcher(IServiceProvider sp) : IDomainEventDispatcher
{
    public async Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var e in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
            foreach (var h in sp.GetServices(handlerType))
                await (Task)handlerType.GetMethod("Handle")!.Invoke(h, [e, ct])!;
        }
    }
}
