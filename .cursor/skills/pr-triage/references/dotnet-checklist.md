# Cross.Identity PR Review Checklist

Use for deep review of PRs in `pr-triage` and `bugbot`.

## Security & Auth (critical)

- No logging of passwords, refresh/access tokens, verification codes
- JWT: correct claims, expiry, signing key handling
- OAuth/external login: state validation, callback security
- Input validation (FluentValidation / ModelState)
- Parameterized EF Core queries (no SQL concatenation)
- See `.cursor/rules/104-backend-auth.mdc`, `105-backend-security.mdc`

## .NET & Code Style

- `Nullable enable`, `Async` suffix on async methods
- `GlobalUsings.cs` in projects
- UTF-8 with BOM for `.cs`, `.csproj`, `.sln`
- Follow `.editorconfig`
- Minimal logic in controllers (if Sample.Api is affected)

## Process Engine

- New/changed flows: JSON in `ProcessEngine/Definitions/Flows/`
- Steps registered via factories
- Update `FLOWS.md` when public flows change
- Email/SMS templates in `Definitions/Templates/`

## Tests

Canonical: `.cursor/rules/300-testing-dotnet.mdc` (keep review comments aligned with it).

- New behavior covered in `Cross.Identity.Tests/`
- Method names: `Given[X]_When[Y]_Then[Z]`; async tests end with `Async`
- Flow → `Identity/FlowTests/` (Integration); steps/factories → `StepTests` / `StepFactoryTests` (Unit)
- Run: `dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj`
- **Do not** request or nitpick missing XML `/// <summary>` on test methods / `[SetUp]` — names are the documentation

## Breaking Changes

- Public NuGet API — semver impact
- EF migrations (if any) — backward compatibility
