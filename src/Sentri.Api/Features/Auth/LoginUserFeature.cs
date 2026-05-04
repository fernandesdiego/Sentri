using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;
using System.Security.Claims;

namespace Sentri.Api.Features.Auth;

public record LoginUserRequest(string Email, string Password);

public record LoginUserResult(Guid UserId, string Email, string Name);

public class LoginUserHandler(AppDbContext context, IPasswordHasher<User> passwordHasher)
{
    public async Task<Result<LoginUserResult>> Handle(LoginUserRequest request, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null)
        {
            return Result.Fail("Invalid email or password.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Result.Fail("Invalid email or password.");
        }

        return Result.Ok(new LoginUserResult(user.Id, user.Email, user.Name));
    }
}

public static class LoginUserEndpoint
{
    public static void MapLoginUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginUserRequest request,
            [FromServices] LoginUserHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? await SignInAsync(httpContext, result.Value)
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("LoginUser")
        .WithTags("Auth")
        .AllowAnonymous()
        .WithDescription("Authenticates a user and establishes a browser cookie session for the dashboard.");
    }

    private static async Task<IResult> SignInAsync(HttpContext httpContext, LoginUserResult result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Email, result.Email),
            new(ClaimTypes.Name, result.Name)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.PanelCookieScheme));

        await httpContext.SignInAsync(
            AuthConstants.PanelCookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

        return Results.Ok(new { userId = result.UserId, email = result.Email, name = result.Name });
    }
}
