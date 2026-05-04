using Microsoft.EntityFrameworkCore;
using Sentri.Api.Features.Providers.CreateProvider;
using Sentri.Api.Features.Providers.RegisterExpense;
using Sentri.Api.Features.Providers.GetProviders;
using Sentri.Api.Features.Providers.GetProviderById;
using Sentri.Api.Features.Expenses.GetProviderExpenses;
using Sentri.Api.Domain;
using Sentri.Api.Domain.Events;
using Sentri.Api.Features.Notifications;
using Sentri.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Sentri.Api.Features.Auth;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<CreateProviderHandler>();
builder.Services.AddScoped<RegisterExpenseHandler>();
builder.Services.AddScoped<GetProvidersHandler>();
builder.Services.AddScoped<GetProviderByIdHandler>();
builder.Services.AddScoped<GetProviderExpensesHandler>();

builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginUserHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapCreateProvider();
app.MapRegisterExpense();
app.MapGetProviders();
app.MapGetProviderById();
app.MapGetProviderExpenses();
app.MapRegisterUser();
app.MapLoginUser();

app.UseHttpsRedirection();

app.Run();