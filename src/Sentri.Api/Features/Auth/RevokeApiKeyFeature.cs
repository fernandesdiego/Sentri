using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Infrastructure;
using System.Security.Claims;

namespace Sentri.Api.Features.Auth;

public record RevokeApiKeyResult(Guid Id, DateTimeOffset RevokedAt);

public class RevokeApiKeyHandler(AppDbContext context)
{
    public async Task<Result<RevokeApiKeyResult>> Handle(Guid keyId, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        var apiKey = await context.ApiKeys
            .FirstOrDefaultAsync(key => key.Id == keyId && key.UserId == userId, ct);

        if (apiKey is null)
        {
            return Result.Fail("API key not found.");
        }

        apiKey.Revoke();
        await context.SaveChangesAsync(ct);

        return Result.Ok(new RevokeApiKeyResult(apiKey.Id, apiKey.RevokedAt!.Value));
    }
}

public static class RevokeApiKeyEndpoint
{
    public static void MapRevokeApiKey(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/auth/keys/{keyId:guid}", async (
            Guid keyId,
            [FromServices] RevokeApiKeyHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(keyId, user, ct);

            return result.IsSuccess
                ? Results.Ok(new
                {
                    id = result.Value.Id,
                    revokedAt = result.Value.RevokedAt
                })
                : Results.NotFound(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("RevokeApiKey")
        .WithTags("Auth")
        .RequireAuthorization(AuthConstants.PanelPolicy)
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Revokes an API key owned by the signed-in dashboard user.");
    }
}