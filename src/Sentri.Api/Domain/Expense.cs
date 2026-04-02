namespace Sentri.Api.Domain;

public class Expense : Entity
{
    public Guid Id { get; init; }
    public decimal Amount { get; private set; }
    public DateTimeOffset Date { get; private set; }
    public Guid SnapshotId { get; private set; }
    public string? Notes { get; private set; }

    private Expense() { } // Required for EF Core

    internal Expense(decimal amount, Guid snapshotId, DateTimeOffset date, string? notes)
    {
        if (amount <= 0) throw new ArgumentException("Expense amount must be positive.");
        
        Id = Guid.NewGuid();
        Amount = amount;
        Date = date;
        SnapshotId = snapshotId;
        Notes = notes;
    }
}
