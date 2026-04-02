using Sentri.Api.Domain;
using Sentri.Api.Domain.Events;
using Sentri.Tests.Common.Builders;

namespace Sentri.Tests.Domain;

/// <summary>
/// ProviderMonthlySnapshot has an internal constructor, so we test its behaviour
/// through the public Provider.AddExpense API — which is the correct access boundary.
/// </summary>
public class ProviderMonthlySnapshotTests
{
    // ── TotalSpend accumulation ──────────────────────────────────────────────

    [Fact]
    public void RecordExpense_UpdatesTotalSpend()
    {
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(30m);
        provider.AddExpense(20m);

        provider.Snapshots.Single().TotalSpend.Should().Be(50m);
    }

    // ── Domain event: not raised below threshold ──────────────────────────────

    [Fact]
    public void RecordExpense_BelowThreshold_DoesNotRaiseDomainEvent()
    {
        // Budget=100, threshold=0.8 → event fires at >=80
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(79m);

        provider.Snapshots.Single().DomainEvents
            .Should().BeEmpty();
    }

    // ── Domain event: raised exactly once on crossing ─────────────────────────

    [Fact]
    public void RecordExpense_CrossingThreshold_RaisesWarningEvent()
    {
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(79m); // still below
        provider.AddExpense(2m);  // crosses 80 → event should fire

        var events = provider.Snapshots.Single().DomainEvents
            .OfType<WarningThresholdReachedDomainEvent>()
            .ToList();

        events.Should().HaveCount(1);
        events.Single().CurrentSpend.Should().Be(81m);
    }

    // ── Domain event: not raised again after AlertSent is set ─────────────────

    [Fact]
    public void RecordExpense_AfterAlertSent_DoesNotRaiseEventAgain()
    {
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(80m); // crosses — first alert
        provider.AddExpense(10m); // already alerted

        var events = provider.Snapshots.Single().DomainEvents
            .OfType<WarningThresholdReachedDomainEvent>();

        events.Should().HaveCount(1);
    }

    // ── AlertSent flag ────────────────────────────────────────────────────────

    [Fact]
    public void RecordExpense_BelowThreshold_AlertSentIsFalse()
    {
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(50m);

        provider.Snapshots.Single().AlertSent.Should().BeFalse();
    }

    [Fact]
    public void RecordExpense_CrossingThreshold_AlertSentIsTrue()
    {
        var provider = new ProviderBuilder().WithBudget(100m).WithThreshold(0.8m).Build();

        provider.AddExpense(80m);

        provider.Snapshots.Single().AlertSent.Should().BeTrue();
    }

    // ── Returned Expense ──────────────────────────────────────────────────────

    [Fact]
    public void RecordExpense_ReturnsExpenseWithCorrectAmountAndNotes()
    {
        var provider = new ProviderBuilder().Build();

        var expense = provider.AddExpense(42m, "Test note");

        expense.Amount.Should().Be(42m);
        expense.Notes.Should().Be("Test note");
        expense.SnapshotId.Should().Be(provider.Snapshots.Single().Id);
    }
}
