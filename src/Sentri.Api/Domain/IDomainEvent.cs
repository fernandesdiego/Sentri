using MediatR;

namespace Sentri.Api.Domain;

public interface IDomainEvent : INotification
{
    DateTimeOffset OcurredAt { get; }
    Guid Id { get; }
}
