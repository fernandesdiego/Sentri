using Sentri.Api.Domain;
using Sentri.Tests.Common.Builders;

namespace Sentri.Tests.Domain;

public class ExpenseTests
{
    // Expense constructor is internal, so we test it through Provider.AddExpense.

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddExpense_WithNonPositiveAmount_ThrowsArgumentException(decimal amount)
    {
        var provider = new ProviderBuilder().Build();

        var act = () => provider.AddExpense(amount);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void AddExpense_WithValidAmount_ExpenseHasCorrectSnapshotId()
    {
        var provider = new ProviderBuilder().Build();

        var expense = provider.AddExpense(15m);

        expense.SnapshotId.Should().Be(provider.Snapshots.Single().Id);
    }

    [Fact]
    public void AddExpense_WithoutNotes_NotesIsNull()
    {
        var provider = new ProviderBuilder().Build();

        var expense = provider.AddExpense(10m);

        expense.Notes.Should().BeNull();
    }

    [Fact]
    public void AddExpense_DateIsSetToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var provider = new ProviderBuilder().Build();

        var expense = provider.AddExpense(10m);

        expense.Date.Should().BeOnOrAfter(before)
            .And.BeOnOrBefore(DateTimeOffset.UtcNow);
    }
}
