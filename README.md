<div align="center">

# 🛡️ Sentri

**Infrastructure spend tracking for contractors and developers.**  
Know when you're over budget.

[![.NET](https://github.com/fernandesdiego/Sentri/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/fernandesdiego/Sentri/actions/workflows/build-and-test.yml)
[![codecov](https://codecov.io/gh/fernandesdiego/Sentri/branch/master/graph/badge.svg?token=141I5WKW5R)](https://codecov.io/gh/fernandesdiego/Sentri)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## What is Sentri?

Sentri is a self-hosted REST API designed to help contractors and developers **track infrastructure costs** across multiple cloud providers (AWS, DigitalOcean, Vercel, etc.). Define monthly budgets per provider, log expenses as they happen, and get **email alerts** automatically when spend approaches a configurable warning threshold . so you never get hit by a surprise bill.

---

## ✨ Features

| Feature | Description |
|---|---|
| **Provider Management** | Create and manage infrastructure providers with monthly budgets and warning thresholds |
| **Expense Tracking** | Register individual expenses against any provider |
| **Monthly Budget Cycles** | Automatic monthly snapshots isolate spending periods cleanly |
| **Threshold Alerts** | Email notifications via [Brevo](https://www.brevo.com/) when spending crosses your warning threshold |
| **JWT Authentication** | Secure, per-user data isolation out of the box |
| **Vertical Slice Architecture** | Features are fully self-contained . easy to navigate, easy to extend |

---

## 🏗️ Architecture

Sentri is built on **Vertical Slice Architecture** . each feature owns its own request, handler, response, and endpoint. There are no shared services leaking across feature boundaries.

```
Sentri.Api/
├── Domain/               # Core entities & domain events
│   ├── Provider.cs
│   ├── Expense.cs
│   ├── ProviderMonthlySnapshot.cs
│   └── User.cs
├── Features/             # One folder per use case
│   ├── Auth/             # Register & login
│   ├── Providers/        # CRUD + expense registration
│   ├── Expenses/         # Expense queries
│   └── Notifications/    # Brevo email integration
└── Infrastructure/       # EF Core DbContext & migrations
```

**Key technology choices:**

- **ASP.NET Core 10** . Minimal APIs, no controllers
- **Entity Framework Core 10** . PostgreSQL via Npgsql
- **MediatR** . Internal event dispatch (domain events)
- **JWT Bearer** . Stateless authentication
- **xUnit + FluentAssertions + NSubstitute** . Test stack
- **coverlet** . Code coverage collection

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance (local or Docker)
- A [Brevo](https://www.brevo.com/) API key (for email alerts)

### 1. Clone the repo

```bash
git clone https://github.com/fernandesdiego/Sentri.git
cd Sentri
```

### 2. Configure secrets

```bash
cd src/Sentri.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=sentri;Username=postgres;Password=yourpassword"
dotnet user-secrets set "JwtSettings:Key" "your-super-secret-key-at-least-32-chars"
dotnet user-secrets set "JwtSettings:Issuer" "sentri"
dotnet user-secrets set "JwtSettings:Audience" "sentri-users"
dotnet user-secrets set "Brevo:ApiKey" "your-brevo-api-key"
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

Swagger UI is available at `https://localhost:{port}/swagger` in development mode.

---

## 🔁 API Overview

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `POST` | `/auth/register` | Register a new user | ❌ |
| `POST` | `/auth/login` | Get a JWT token | ❌ |
| `GET` | `/providers` | List all your providers | ✅ |
| `GET` | `/providers/{id}` | Get a single provider | ✅ |
| `POST` | `/providers` | Create a provider | ✅ |
| `POST` | `/providers/{id}/expenses` | Register an expense | ✅ |
| `GET` | `/providers/{id}/expenses` | List provider expenses | ✅ |

> All authenticated endpoints scope data strictly to the authenticated user.

---

## 🧪 Running Tests

```bash
dotnet test
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🔔 Budget Alerts

When an expense pushes a provider's monthly spend past its `WarningThreshold` (a value between 0–1 representing the fraction of `MonthlyBudget`), Sentri automatically fires a domain event that triggers an email notification via Brevo. No polling, no cron jobs.

**Example:** A provider with a $100 budget and a `0.8` threshold will trigger an alert the moment cumulative spend in the current month exceeds $80.

<img width="756" height="764" alt="image" src="https://github.com/user-attachments/assets/0a1df9ac-78fe-4aec-b849-3b726e8cd71c" />


---

## 🤝 Contributing

This is a personal project and is not currently accepting contributions. Feel free to fork and adapt it for your own use.
