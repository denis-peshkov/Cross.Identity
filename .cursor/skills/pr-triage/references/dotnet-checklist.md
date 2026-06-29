# Cross.Identity PR Review Checklist

Использовать при deep review PR в `pr-triage` и `bugbot`.

## Security & Auth (критично)

- Нет логирования паролей, refresh/access tokens, verification codes
- JWT: корректные claims, expiry, signing key handling
- OAuth/external login: state validation, callback security
- Валидация входных данных (FluentValidation / ModelState)
- Параметризованные запросы EF Core (без SQL concatenation)
- См. `.cursor/rules/104-backend-auth.mdc`, `105-backend-security.mdc`

## .NET & Code Style

- `Nullable enable`, суффикс `Async` у async-методов
- `GlobalUsings.cs` в проектах
- UTF-8 with BOM для `.cs`, `.csproj`, `.sln`
- Следовать `.editorconfig`
- Минимальная логика в контроллерах (если Sample.Api затронут)

## Process Engine

- Новые/изменённые flows: JSON в `ProcessEngine/Definitions/Flows/`
- Шаги регистрируются через factories
- Обновить `FLOWS.md` при изменении публичных flows
- Шаблоны email/SMS в `Definitions/Templates/`

## Tests

- Новое поведение покрыто в `Cross.Identity.Tests/`
- Flow tests / step tests по существующим паттернам
- Запуск: `dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj`

## Breaking Changes

- Публичный API NuGet — semver impact
- Миграции EF (если есть) — обратная совместимость
