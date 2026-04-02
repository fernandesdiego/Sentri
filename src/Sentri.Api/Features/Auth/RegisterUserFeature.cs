using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentri.Api.Domain;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Auth;

public record RegisterUserRequest(string Email, string Name, string Password);

public class RegisterUserHandler(AppDbContext context, IPasswordHasher<User> passwordHasher)
{
    public async Task<Result<Guid>> Handle(RegisterUserRequest request, CancellationToken ct)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email, ct))
        {
            return Result.Fail("Email is already registered.");
        }

        try
        {
            var hash = passwordHasher.HashPassword(null!, request.Password);
            var user = new User(request.Email, request.Name, hash);
            
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);

            return Result.Ok(user.Id);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}

public static class RegisterUserEndpoint
{
    public static void MapRegisterUser(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            [FromBody] RegisterUserRequest request,
            [FromServices] RegisterUserHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(new { userId = result.Value })
                : Results.BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        })
        .WithName("RegisterUser")
        .WithTags("Auth")
        .AllowAnonymous()
        .WithDescription("Registers a new user.");
    }
}
