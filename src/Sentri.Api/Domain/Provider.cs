using Sentri.Api.Domain.Events;

namespace Sentri.Api.Domain;

public class Provider : Entity
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private readonly List<Expense> _expenses = [];
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public decimal MonthlyBudget { get; private set; }
    public decimal WarningThreshold { get; private set; }
    public decimal CurrentSpend { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

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
        CurrentSpend = 0;
    }

    public bool HasReachedThreshold() => CurrentSpend >= (MonthlyBudget * WarningThreshold);

    public Expense AddExpense(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Expense amount must be positive.");

        var wasBelowThreshold = !HasReachedThreshold();

        var expense = new Expense(amount, Id);
        _expenses.Add(expense);

        CurrentSpend += amount;

        if (wasBelowThreshold && HasReachedThreshold())
        {
            Raise(new WarningThresholdReachedDomainEvent(
                Id, 
                Name, 
                CurrentSpend, 
                MonthlyBudget, 
                WarningThreshold));
        }

        return expense;
    }

    public void UpdateThreshold(decimal newThreshold)
    {
        if (newThreshold <= 0 || newThreshold > 1) throw new ArgumentException("Invalid threshold.");
        WarningThreshold = newThreshold;
    }
}