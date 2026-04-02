namespace Sentri.Api.Domain;

public class Provider : Entity
{
    private readonly List<ProviderMonthlySnapshot> _snapshots = [];
    public IReadOnlyCollection<ProviderMonthlySnapshot> Snapshots => _snapshots.AsReadOnly();
    
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public decimal MonthlyBudget { get; private set; }
    public decimal WarningThreshold { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    private Provider() { } // Required for EF Core

    public Provider(string name, decimal monthlyBudget, decimal warningThreshold, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (monthlyBudget <= 0) throw new ArgumentException("Budget must be positive.");
        if (warningThreshold <= 0 || warningThreshold > 1) throw new ArgumentException("Threshold must be between 0.1 and 1.0.");

        Id = Guid.NewGuid();
        Name = name;
        MonthlyBudget = monthlyBudget;
        WarningThreshold = warningThreshold;
        UserId = userId;
    }

    public Expense AddExpense(decimal amount, string? notes = null)
    {
        if (amount <= 0) throw new ArgumentException("Expense amount must be positive.");

        var date = DateTimeOffset.UtcNow;
        var snapshot = GetOrCreateCurrentSnapshot(date.Year, date.Month);
        
        return snapshot.RecordExpense(amount, date, MonthlyBudget, WarningThreshold, Name, notes);
    }

    public void UpdateThreshold(decimal newThreshold)
    {
        if (newThreshold <= 0 || newThreshold > 1) throw new ArgumentException("Invalid threshold.");
        WarningThreshold = newThreshold;
    }

    private ProviderMonthlySnapshot GetOrCreateCurrentSnapshot(int year, int month)
    {
        var snapshot = _snapshots.FirstOrDefault(s => s.Year == year && s.Month == month);
        
        if (snapshot is null)
        {
            snapshot = new ProviderMonthlySnapshot(Id, year, month);
            _snapshots.Add(snapshot);
        }

        return snapshot;
    }
}