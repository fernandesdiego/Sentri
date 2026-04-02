using Sentri.Api.Domain;
using Sentri.Tests.Common.Builders;

namespace Sentri.Tests.Domain;

public class ProviderTests
{
    // ── Construction ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_CreatesProvider()
    {
        var userId = Guid.NewGuid();
        var provider = new Provider("OpenAI", 200m, 0.8m, userId);

        provider.Id.Should().NotBeEmpty();
        provider.Name.Should().Be("OpenAI");
        provider.MonthlyBudget.Should().Be(200m);
        provider.WarningThreshold.Should().Be(0.8m);
        provider.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string name)
    {
        var act = () => new Provider(name, 100m, 0.8m, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Constructor_WithZeroBudget_ThrowsArgumentException(decimal budget)
    {
        var act = () => new Provider("OpenAI", budget, 0.8m, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.01)]
    [InlineData(-0.5)]
    public void Constructor_WithInvalidThreshold_ThrowsArgumentException(decimal threshold)
    {
        var act = () => new Provider("OpenAI", 100m, threshold, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    // ── AddExpense ───────────────────────────────────────────────────────────

    [Fact]
    public void AddExpense_WithValidAmount_ReturnsExpenseWithCorrectAmount()
    {
        var provider = new ProviderBuilder().Build();

        var expense = provider.AddExpense(25m, "API call batch");

        expense.Amount.Should().Be(25m);
        expense.Notes.Should().Be("API call batch");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void AddExpense_WithNonPositiveAmount_ThrowsArgumentException(decimal amount)
    {
        var provider = new ProviderBuilder().Build();

        var act = () => provider.AddExpense(amount);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddExpense_CreatesSnapshotForCurrentMonth()
    {
        var provider = new ProviderBuilder().Build();

        provider.AddExpense(10m);

        provider.Snapshots.Should().HaveCount(1);
        provider.Snapshots.First().Year.Should().Be(DateTimeOffset.UtcNow.Year);
        provider.Snapshots.First().Month.Should().Be(DateTimeOffset.UtcNow.Month);
    }

    [Fact]
    public void AddExpense_CalledTwiceInSameMonth_ReusesSameSnapshot()
    {
        var provider = new ProviderBuilder().Build();

        provider.AddExpense(10m);
        provider.AddExpense(20m);

        provider.Snapshots.Should().HaveCount(1);
        provider.Snapshots.First().TotalSpend.Should().Be(30m);
    }

    // ── UpdateThreshold ──────────────────────────────────────────────────────

    [Fact]
    public void UpdateThreshold_WithValidValue_UpdatesThreshold()
    {
        var provider = new ProviderBuilder().WithThreshold(0.8m).Build();

        provider.UpdateThreshold(0.5m);

        provider.WarningThreshold.Should().Be(0.5m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.01)]
    [InlineData(-1)]
    public void UpdateThreshold_WithInvalidValue_ThrowsArgumentException(decimal threshold)
    {
        var provider = new ProviderBuilder().Build();

        var act = () => provider.UpdateThreshold(threshold);

        act.Should().Throw<ArgumentException>();
    }
}
