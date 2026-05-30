# Git Flow

This repository uses a lightweight Git Flow model.

## Branches

- `main` contains stable code.
- `develop` is used for integration before a release.
- `feature/<name>` branches are used for application changes.
- `release/<version>` branches are used for final release checks.
- `hotfix/<name>` branches are used for urgent fixes from `main`.

## Typical Workflow

Create a feature branch from `develop`:

```bash
git checkout develop
git pull
git checkout -b feature/audit-chain-status
```

Before opening a pull request:

```bash
dotnet build Fintech.InvestmentDemo.slnx
dotnet test tests/Fintech.Api.Tests/Fintech.Api.Tests.csproj
```

Merge order:

1. `feature/*` -> `develop`
2. `develop` -> `release/*`
3. `release/*` -> `main`
4. `main` -> `develop`

For small fixes, `hotfix/*` can branch from `main` and then be merged back into both `main` and `develop`.

## Commit Style

Use short imperative commit messages:

```text
Add audit chain status endpoint
Fix idempotency retry handling
Update compose health checks
```

## Pull Request Checklist

- Build passes.
- Tests pass.
- Docker Compose still starts locally when infrastructure files changed.
- Public docs are updated when behavior or setup changes.
