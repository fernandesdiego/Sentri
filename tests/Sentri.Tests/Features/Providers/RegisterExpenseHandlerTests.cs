using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Sentri.Api.Domain;
using Sentri.Api.Features.Providers.RegisterExpense;
using Sentri.Api.Infrastructure;
using Sentri.Tests.Common.Builders;

namespace Sentri.Tests.Features.Providers;

public class RegisterExpenseHandlerTests
{
    private static AppDbContext BuildInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var publisher = Substitute.For<IPublisher>();
        return new AppDbContext(options, publisher);
    }

    private static ClaimsPrincipal AuthenticatedUser(Guid userId) =>
        new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ]));

    private static ClaimsPrincipal AnonymousUser() =>
        new ClaimsPrincipal(new ClaimsIdentity());

    /// Seeds a provider owned by the given user and returns its Id.
    private static async Task<Guid> SeedProviderAsync(
        AppDbContext context,
        Guid userId,
        decimal budget = 100m,
        decimal threshold = 0.8m)
    {
        var provider = new ProviderBuilder()
            .WithUserId(userId)
            .WithBudget(budget)
            .WithThreshold(threshold)
            .Build();

        context.Providers.Add(provider);
        await context.SaveChangesAsync();
        return provider.Id;
    }

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsSuccess()
    {
        await using var context = BuildInMemoryContext();
        var userId = Guid.NewGuid();
        var providerId = await SeedProviderAsync(context, userId);
        var handler = new RegisterExpenseHandler(context);
        var request = new RegisterExpenseRequest(25m, providerId);

        var result = await handler.Handle(request, AuthenticatedUser(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await context.Expenses.CountAsync()).Should().Be(1);
    }

    // ── Auth guard ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithUnauthenticatedUser_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new RegisterExpenseHandler(context);
        var request = new RegisterExpenseRequest(25m, Guid.NewGuid());

        var result = await handler.Handle(request, AnonymousUser(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    // ── Ownership guard ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProviderBelongsToDifferentUser_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var ownerId = Guid.NewGuid();
        var providerId = await SeedProviderAsync(context, ownerId);

        var handler = new RegisterExpenseHandler(context);
        var differentUser = AuthenticatedUser(Guid.NewGuid());
        var request = new RegisterExpenseRequest(25m, providerId);

        var result = await handler.Handle(request, differentUser, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenProviderDoesNotExist_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new RegisterExpenseHandler(context);
        var request = new RegisterExpenseRequest(25m, Guid.NewGuid());

        var result = await handler.Handle(request, AuthenticatedUser(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    // ── Threshold flag ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenSpendCrossesThreshold_ReturnsTrueFlag()
    {
        await using var context = BuildInMemoryContext();
        var userId = Guid.NewGuid();
        // Budget=100, threshold=0.8 → fires at >=80
        var providerId = await SeedProviderAsync(context, userId, budget: 100m, threshold: 0.8m);
        var handler = new RegisterExpenseHandler(context);

        var result = await handler.Handle(
            new RegisterExpenseRequest(80m, providerId),
            AuthenticatedUser(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSpendIsBelowThreshold_ReturnsFalseFlag()
    {
        await using var context = BuildInMemoryContext();
        var userId = Guid.NewGuid();
        var providerId = await SeedProviderAsync(context, userId, budget: 100m, threshold: 0.8m);
        var handler = new RegisterExpenseHandler(context);

        var result = await handler.Handle(
            new RegisterExpenseRequest(50m, providerId),
            AuthenticatedUser(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    // ── Domain validation ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithZeroAmount_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var userId = Guid.NewGuid();
        var providerId = await SeedProviderAsync(context, userId);
        var handler = new RegisterExpenseHandler(context);

        var result = await handler.Handle(
            new RegisterExpenseRequest(0m, providerId),
            AuthenticatedUser(userId),
            CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }
}
