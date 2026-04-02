using MediatR;

namespace Sentri.Api.Domain;

public interface IDomainEvent : INotification
{
    DateTimeOffset OcurredAt { get; }
    Guid Id { get; }
}

[Obsolete("Use MediatR's INotificationHandler<TEvent> instead")]
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken ct = default);
}
