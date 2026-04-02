using Sentri.Api.Domain;

namespace Sentri.Api.Infrastructure;

[Obsolete("Use MediatR's IPublisher instead")]
public interface IDomainEventDispatcher
{
    Task Dispatch(IDomainEvent domainEvent, CancellationToken ct);
}
[Obsolete("Use MediatR's IPublisher instead")]
public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task Dispatch(IDomainEvent domainEvent, CancellationToken ct)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            //TODO: source generator to generate this code at compile time
            var method = handlerType.GetMethod("Handle");

            if (method != null) await (Task)method.Invoke(handler, [domainEvent, ct])!;
        }
    }
}