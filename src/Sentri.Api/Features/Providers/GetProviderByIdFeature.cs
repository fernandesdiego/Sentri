using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Providers.GetProviderById;

public record ProviderDetailsResult(Guid Id, string Name, decimal MonthlyBudget, decimal WarningThreshold, decimal CurrentSpend);

public class GetProviderByIdHandler(AppDbContext context)
{
    public async Task<Result<ProviderDetailsResult>> Handle(Guid id, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        var currentYear = DateTimeOffset.UtcNow.Year;
        var currentMonth = DateTimeOffset.UtcNow.Month;

        var provider = await context.Providers
            .Where(p => p.Id == id && p.UserId == userId)
            .Select(p => new ProviderDetailsResult(
                p.Id, 
                p.Name, 
                p.MonthlyBudget, 
                p.WarningThreshold, 
                p.Snapshots.Where(s => s.Year == currentYear && s.Month == currentMonth).Select(s => (decimal?)s.TotalSpend).FirstOrDefault() ?? 0m))
            .FirstOrDefaultAsync(ct);

        if (provider is null)
        {
            return Result.Fail("Provider not found.");
        }

        return Result.Ok(provider);
    }
}

public static class GetProviderByIdEndpoint
{
    public static void MapGetProviderById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/providers/{id:guid}", async (
            Guid id,
            [FromServices] GetProviderByIdHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(id, user, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("GetProviderById")
        .WithTags("Providers")
        .RequireAuthorization()
        .Produces<ProviderDetailsResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Retrieves details of a specific provider.");
    }
}
