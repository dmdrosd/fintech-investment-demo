# Fintech Investment Demo

A compact .NET demo application for processing investment operation requests.

The project shows a typical internal fintech workflow: a Blazor/MudBlazor web form sends requests to an ASP.NET Core API, the API stores data in PostgreSQL, writes an audit trail, supports Keycloak-ready authentication, and runs as containerized services.

## Stack

- .NET 8, C#
- ASP.NET Core Web API
- Blazor Server
- MudBlazor
- PostgreSQL with EF Core and Npgsql
- Keycloak-ready JWT authentication
- .NET Aspire AppHost
- Docker Compose
- xUnit tests

## Features

- Investment request creation form with validation and loading states.
- Request list with status transitions.
- PostgreSQL persistence with EF Core migrations.
- Idempotent request creation using the `Idempotency-Key` header.
- Optimistic concurrency with expected version checks.
- Audit log for request creation and status changes.
- SHA-256 audit hash-chain for tamper-evident audit verification.
- Development authentication mode for local runs.
- Keycloak realm import with `operator` and `auditor` roles.
- Health checks for API and PostgreSQL.

## Architecture

```text
Blazor Web UI
    |
    v
ASP.NET Core API
    |
    +-- PostgreSQL
    |
    +-- Keycloak-ready JWT auth
    |
    +-- Audit log with SHA-256 hash-chain
```

## Run With Docker Compose

```powershell
docker compose up -d --build
```

After startup:

- Web UI: http://localhost:8082
- API Swagger: http://localhost:8081/swagger
- API health: http://localhost:8081/health/live
- Audit chain status: http://localhost:8081/api/audit/chain/status
- Keycloak: http://localhost:8080

Keycloak admin:

- Login: `admin`
- Password: `admin`

Demo user in realm `fintech-demo`:

- Login: `operator`
- Password: `Passw0rd!`

By default, Docker Compose runs the API with `Auth__Mode=Development`, so the UI works locally without an OIDC login flow. Keycloak is still started and imports the realm, so the JWT mode can be tested by switching `Auth__Mode=Keycloak` and passing a valid access token to the API.

## Build And Test

```powershell
dotnet build Fintech.InvestmentDemo.slnx
dotnet test tests\Fintech.Api.Tests\Fintech.Api.Tests.csproj
```

## Run With Aspire

```powershell
dotnet run --project src\Fintech.AppHost\Fintech.AppHost.csproj
```

The AppHost describes PostgreSQL, Keycloak, the API, the web application, and service dependencies.

## Development Workflow

The repository uses a lightweight Git Flow model with `main`, `develop`, `feature/*`, `release/*`, and `hotfix/*` branches. See [docs/git-flow.md](docs/git-flow.md).

## Useful Endpoints

- `GET /api/investment-requests`
- `POST /api/investment-requests`
- `PUT /api/investment-requests/{id}/status`
- `GET /api/audit`
- `GET /api/audit/chain/status`
- `GET /health/live`
- `GET /health/ready`

## Clean Up

```powershell
docker compose down
```

Remove PostgreSQL data as well:

```powershell
docker compose down -v
```
