using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sentri.Api.Infrastructure;
using Sentri.Api.Domain;

namespace Sentri.Api.Features.Providers.GetProviders;

public record ProviderSummaryResult(Guid Id, string Name, decimal MonthlyBudget, decimal WarningThreshold, decimal CurrentSpend);

public class GetProvidersHandler(AppDbContext context)
{
    public async Task<List<ProviderSummaryResult>> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return [];
        }

        return await context.Providers
            .Where(p => p.UserId == userId)
            .Select(p => new ProviderSummaryResult(
                p.Id, 
                p.Name, 
                p.MonthlyBudget, 
                p.WarningThreshold, 
                p.CurrentSpend))
            .ToListAsync(ct);
    }
}

public static class GetProvidersEndpoint
{
    public static void MapGetProviders(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/providers", async (
            [FromServices] GetProvidersHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(user, ct);
            return Results.Ok(result);
        })
        .WithName("GetProviders")
        .WithTags("Providers")
        .RequireAuthorization()
        .Produces<List<ProviderSummaryResult>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Retrieves all providers for the logged-in user.");
    }
}
