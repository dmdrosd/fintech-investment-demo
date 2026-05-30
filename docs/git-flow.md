# Git Flow

В репозитории используется облегчённая модель Git Flow.

## Ветки

- `main` содержит стабильный код.
- `develop` используется для интеграции изменений перед релизом.
- `feature/<name>` используется для изменений в приложении.
- `release/<version>` используется для финальных проверок перед релизом.
- `hotfix/<name>` используется для срочных исправлений от `main`.

## Типовой Процесс

Создать feature-ветку от `develop`:

```bash
git checkout develop
git pull
git checkout -b feature/audit-chain-status
```

Перед pull request:

```bash
dotnet build Fintech.InvestmentDemo.slnx
dotnet test tests/Fintech.Api.Tests/Fintech.Api.Tests.csproj
```

Порядок merge:

1. `feature/*` -> `develop`
2. `develop` -> `release/*`
3. `release/*` -> `main`
4. `main` -> `develop`

Для небольших срочных исправлений `hotfix/*` можно создавать от `main`, а затем вливать обратно в `main` и `develop`.

## Стиль Коммитов

Коммиты пишутся коротко, в повелительном наклонении:

```text
Add audit chain status endpoint
Fix idempotency retry handling
Update compose health checks
```

## Документация

Публичная документация ведётся на русском языке. Технические имена, команды, endpoint-ы и названия классов остаются на английском.

## Checklist Для Pull Request

- Сборка проходит.
- Тесты проходят.
- Docker Compose проверен локально, если менялась инфраструктура.
- Публичная документация обновлена, если изменилось поведение или способ запуска.
