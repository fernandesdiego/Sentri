using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Sentri.Api.Domain;
using Sentri.Api.Features.Auth;
using Sentri.Api.Features.Expenses.GetProviderExpenses;
using Sentri.Api.Features.Notifications;
using Sentri.Api.Features.Providers.CreateProvider;
using Sentri.Api.Features.Providers.GetProviderById;
using Sentri.Api.Features.Providers.GetProviders;
using Sentri.Api.Features.Providers.RegisterExpense;
using Sentri.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string AuthScheme = AuthConstants.AuthScheme;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(AuthConstants.ApiKeyScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = $"API key authentication using the '{AuthConstants.ApiKeyHeaderPrefix} ' prefix"
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("PanelCors", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = AuthScheme;
        options.DefaultAuthenticateScheme = AuthScheme;
        options.DefaultChallengeScheme = AuthScheme;
    })
    .AddPolicyScheme(AuthScheme, "Sentri authentication", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorizationHeader = context.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(authorizationHeader) &&
                authorizationHeader.StartsWith($"{AuthConstants.ApiKeyHeaderPrefix} ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthConstants.ApiKeyScheme;
            }

            return AuthConstants.PanelCookieScheme;
        };
    })
    .AddCookie(AuthConstants.PanelCookieScheme, options =>
    {
        options.Cookie.Name = AuthConstants.PanelCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthConstants.ApiKeyScheme, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.BusinessPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(AuthConstants.PanelPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(AuthConstants.PanelCookieScheme);
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddScoped<CreateProviderHandler>();
builder.Services.AddScoped<RegisterExpenseHandler>();
builder.Services.AddScoped<GetProvidersHandler>();
builder.Services.AddScoped<GetProviderByIdHandler>();
builder.Services.AddScoped<GetProviderExpensesHandler>();
builder.Services.AddScoped<CreateApiKeyHandler>();
builder.Services.AddScoped<ListApiKeysHandler>();
builder.Services.AddScoped<RevokeApiKeyHandler>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PanelCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapCreateProvider();
app.MapRegisterExpense();
app.MapGetProviders();
app.MapGetProviderById();
app.MapGetProviderExpenses();
app.MapRegisterUser();
app.MapLoginUser();
app.MapCreateApiKey();
app.MapListApiKeys();
app.MapRevokeApiKey();

app.Run();