using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Sentri.Api.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Providers.RegisterExpense;

public record RegisterExpenseRequest(decimal Amount, Guid ProviderId, string? Notes = null);

public class RegisterExpenseHandler(AppDbContext context)
{
    public async Task<Result<bool>> Handle(RegisterExpenseRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        var provider = await context.Providers
            .Include(p => p.Snapshots)
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId && p.UserId == userId, cancellationToken: ct);

        if (provider is null)
        {
            return Result.Fail("Provider not found.");
        }

        try
        {
            var expense = provider.AddExpense(request.Amount, request.Notes);

            // I hate this, if a new snapshot was just created for this month, EF won't auto-detect it as Added
            // because it lives in a private backing field?. We have to explicitly track it here.
            foreach (var s in provider.Snapshots)
            {
                if (context.Entry(s).State == EntityState.Detached)
                    context.Snapshots.Add(s);
            }

            context.Expenses.Add(expense);

            await context.SaveChangesAsync(ct);

            var snapshot = provider.Snapshots.First(s => s.Id == expense.SnapshotId);
            var thresholdReached = snapshot.TotalSpend >= (provider.MonthlyBudget * provider.WarningThreshold);

            return Result.Ok(thresholdReached);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}

public static class RegisterExpenseEndpoint
{
    public static void MapRegisterExpense(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expenses/", async (
            [FromBody] RegisterExpenseRequest request,
            [FromServices] RegisterExpenseHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, user, ct);

            return result.IsSuccess
                ? Results.Ok(new { thresholdReached = result.Value })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("RegisterExpense")
        .WithTags("Expenses")
        .RequireAuthorization(AuthConstants.BusinessPolicy)
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Registers a new expense for a provider and checks if it has reached its warning threshold limit.");
    }
}
