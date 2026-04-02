namespace Sentri.Api.Domain.Events;

public record WarningThresholdReachedDomainEvent(
    Guid ProviderId, 
    string ProviderName, 
    decimal CurrentSpend, 
    decimal MonthlyBudget, 
    decimal WarningThreshold) : IDomainEvent
{
    public DateTimeOffset OcurredAt { get; } = DateTimeOffset.UtcNow;

    public Guid Id { get; } = Guid.NewGuid();
}
