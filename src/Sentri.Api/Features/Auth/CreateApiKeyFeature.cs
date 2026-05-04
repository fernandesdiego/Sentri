using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;
using System.Security.Claims;

namespace Sentri.Api.Features.Auth;

public record CreateApiKeyRequest(string? Name = null);
public record CreateApiKeyResult(Guid Id, string? Name, string RawKey, DateTimeOffset CreatedAt);

public class CreateApiKeyHandler(AppDbContext context, ApiKeyService apiKeyService)
{
    public async Task<Result<CreateApiKeyResult>> Handle(CreateApiKeyRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result.Fail("User is not authenticated.");
        }

        var keyId = apiKeyService.GenerateKeyId();
        var secret = apiKeyService.GenerateSecret();
        var secretHash = apiKeyService.HashSecret(secret);

        var apiKey = new ApiKey(keyId, userId, secretHash, request.Name);
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync(ct);

        var rawKey = apiKeyService.BuildRawKey(apiKey.Id, secret);
        return Result.Ok(new CreateApiKeyResult(apiKey.Id, apiKey.Name, rawKey, apiKey.CreatedAt));
    }
}

public static class CreateApiKeyEndpoint
{
    public static void MapCreateApiKey(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/keys", async (
            [FromBody] CreateApiKeyRequest request,
            [FromServices] CreateApiKeyHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, user, ct);

            return result.IsSuccess
                ? Results.Created($"/api/auth/keys/{result.Value.Id}", new
                {
                    id = result.Value.Id,
                    name = result.Value.Name,
                    rawKey = result.Value.RawKey,
                    createdAt = result.Value.CreatedAt
                })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("CreateApiKey")
        .WithTags("Auth")
        .RequireAuthorization(AuthConstants.PanelPolicy)
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithDescription("Creates a new API key for the signed-in dashboard user.");
    }
}