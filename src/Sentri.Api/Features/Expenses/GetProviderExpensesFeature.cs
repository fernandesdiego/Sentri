using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sentri.Api.Features.Auth;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Expenses.GetProviderExpenses;

public record ExpenseResult(Guid Id, decimal Amount, DateTimeOffset Date, string? Notes);

public class GetProviderExpensesHandler(AppDbContext context)
{
    public async Task<Result<List<ExpenseResult>>> Handle(Guid providerId, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        var providerExists = await context.Providers.AnyAsync(p => p.Id == providerId && p.UserId == userId, ct);

        if (!providerExists)
        {
            return Result.Fail("Provider not found or access denied.");
        }

        var expenses = await context.Expenses
            .Join(context.Snapshots,
                  e => e.SnapshotId,
                  s => s.Id,
                  (e, s) => new { Expense = e, Snapshot = s })
            .Where(x => x.Snapshot.ProviderId == providerId)
            .OrderByDescending(x => x.Expense.Date)
            .Select(x => new ExpenseResult(x.Expense.Id, x.Expense.Amount, x.Expense.Date, x.Expense.Notes))
            .ToListAsync(ct);

        return Result.Ok(expenses);
    }
}

public static class GetProviderExpensesEndpoint
{
    public static void MapGetProviderExpenses(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/providers/{providerId:guid}/expenses", async (
            Guid providerId,
            [FromServices] GetProviderExpensesHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(providerId, user, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("GetProviderExpenses")
        .WithTags("Expenses")
        .RequireAuthorization(AuthConstants.BusinessPolicy)
        .Produces<List<ExpenseResult>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Retrieves all expenses associated with a specific provider.");
    }
}
