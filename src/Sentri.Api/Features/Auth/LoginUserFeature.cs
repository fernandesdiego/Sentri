using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Auth;

public record LoginUserRequest(string Email, string Password);

public class LoginUserHandler(AppDbContext context, IPasswordHasher<User> passwordHasher, TokenService tokenService)
{
    public async Task<Result<string>> Handle(LoginUserRequest request, CancellationToken ct)
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

        var token = tokenService.GenerateToken(user);
        return Result.Ok(token);
    }
}

public static class LoginUserEndpoint
{
    public static void MapLoginUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginUserRequest request,
            [FromServices] LoginUserHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(new { token = result.Value })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("LoginUser")
        .WithTags("Auth")
        .AllowAnonymous()
        .WithDescription("Authenticates a user and returns a JWT token.");
    }
}
