using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Providers.CreateProvider;

public record CreateProviderRequest(string Name, decimal MonthlyBudget, decimal WarningThreshold = 0.8m);

public class CreateProviderHandler(AppDbContext context)
{
    public async Task<Result<Guid>> Handle(CreateProviderRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        try
        {
            var provider = new Provider(
                request.Name,
                request.MonthlyBudget,
                request.WarningThreshold,
                userId);

            context.Providers.Add(provider);
            await context.SaveChangesAsync(ct);

            return Result.Ok(provider.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}

public static class CreateProviderEndpoint
{
    public static void MapCreateProvider(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/providers", async (
            [FromBody] CreateProviderRequest request,
            [FromServices] CreateProviderHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, user, ct);

            return result.IsSuccess
                ? Results.Created($"/api/providers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("CreateProvider")
        .WithTags("Providers")
        .RequireAuthorization()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Creates a new infrastructure or AI provider to monitor costs.");
    }
}