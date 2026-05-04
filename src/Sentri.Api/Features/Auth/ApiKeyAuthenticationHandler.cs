using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentri.Api.Infrastructure;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Sentri.Api.Features.Auth;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppDbContext context,
    ApiKeyService apiKeyService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var rawKey = extractedApiKey.ToString().Trim();

        if (!apiKeyService.TryParseRawKey(rawKey, out var keyId, out var secret))
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        var apiKey = await context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == keyId, Context.RequestAborted);

        if (apiKey is null || apiKey.RevokedAt is not null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!apiKeyService.VerifySecret(apiKey.SecretHash, secret))
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        apiKey.MarkUsed();
        await context.SaveChangesAsync(Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()),
            new(ClaimTypes.Email, apiKey.User.Email),
            new(ClaimTypes.Name, apiKey.User.Name),
            new("auth_type", "api_key"),
            new("api_key_id", apiKey.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}