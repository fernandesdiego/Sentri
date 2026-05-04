using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Infrastructure;
using System.Security.Claims;

namespace Sentri.Api.Features.Auth;

public record ApiKeySummaryResult(Guid Id, string? Name, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? RevokedAt);

public class ListApiKeysHandler(AppDbContext context)
{
    public async Task<List<ApiKeySummaryResult>> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return [];
        }

        return await context.ApiKeys
            .Where(key => key.UserId == userId)
            .OrderByDescending(key => key.CreatedAt)
            .Select(key => new ApiKeySummaryResult(key.Id, key.Name, key.CreatedAt, key.LastUsedAt, key.RevokedAt))
            .ToListAsync(ct);
    }
}

public static class ListApiKeysEndpoint
{
    public static void MapListApiKeys(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/keys", async (
            [FromServices] ListApiKeysHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(user, ct);
            return Results.Ok(result);
        })
        .WithName("ListApiKeys")
        .WithTags("Auth")
        .RequireAuthorization(AuthConstants.PanelPolicy)
        .Produces<List<ApiKeySummaryResult>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Lists the API keys belonging to the signed-in dashboard user.");
    }
}