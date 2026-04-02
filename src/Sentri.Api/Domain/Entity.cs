namespace Sentri.Api.Domain;

public abstract class Entity
{
    public readonly List<IDomainEvent> _domainEvents = new();

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
