using Sentri.Api.Domain.Events;

namespace Sentri.Api.Domain;

public class ProviderMonthlySnapshot : Entity
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal TotalSpend { get; private set; }
    public bool AlertSent { get; private set; }
    
    public uint Version { get; set; }

    private ProviderMonthlySnapshot() { } // Required for EF Core

    internal ProviderMonthlySnapshot(Guid providerId, int year, int month)
    {
        Id = Guid.NewGuid();
        ProviderId = providerId;
        Year = year;
        Month = month;
        TotalSpend = 0;
        AlertSent = false;
    }

    public Expense RecordExpense(decimal amount, DateTimeOffset date, decimal budget, decimal threshold, string providerName, string? notes = null)
    {
        var wasBelowThreshold = TotalSpend < (budget * threshold);
        
        TotalSpend += amount;

        if (!AlertSent && wasBelowThreshold && TotalSpend >= (budget * threshold))
        {
            AlertSent = true;
            Raise(new WarningThresholdReachedDomainEvent(
                ProviderId, 
                providerName, 
                TotalSpend, 
                budget, 
                threshold));
        }

        return new Expense(amount, this.Id, date, notes);
    }
}
