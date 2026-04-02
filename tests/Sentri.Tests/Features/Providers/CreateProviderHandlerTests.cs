using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using MediatR;
using Sentri.Api.Domain;
using Sentri.Api.Features.Providers.CreateProvider;
using Sentri.Api.Infrastructure;
using Sentri.Tests.Common.Builders;

namespace Sentri.Tests.Features.Providers;

public class CreateProviderHandlerTests
{
    private static AppDbContext BuildInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique DB per test
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
        new ClaimsPrincipal(new ClaimsIdentity()); // No claims

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidRequest_ReturnsNewProviderId()
    {
        await using var context = BuildInMemoryContext();
        var handler = new CreateProviderHandler(context);
        var userId = Guid.NewGuid();
        var request = new CreateProviderRequest("OpenAI", 200m, 0.8m);

        var result = await handler.Handle(request, AuthenticatedUser(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        (await context.Providers.FindAsync(result.Value)).Should().NotBeNull();
    }

    // ── Auth guard ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithUnauthenticatedUser_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new CreateProviderHandler(context);
        var request = new CreateProviderRequest("OpenAI", 200m, 0.8m);

        var result = await handler.Handle(request, AnonymousUser(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    // ── Domain validation ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithZeroBudget_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new CreateProviderHandler(context);
        var request = new CreateProviderRequest("OpenAI", 0m, 0.8m);

        var result = await handler.Handle(request, AuthenticatedUser(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("positive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WithInvalidThreshold_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new CreateProviderHandler(context);
        var request = new CreateProviderRequest("OpenAI", 200m, 1.5m);

        var result = await handler.Handle(request, AuthenticatedUser(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyName_ReturnsFailure()
    {
        await using var context = BuildInMemoryContext();
        var handler = new CreateProviderHandler(context);
        var request = new CreateProviderRequest("", 200m, 0.8m);

        var result = await handler.Handle(request, AuthenticatedUser(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }
}
