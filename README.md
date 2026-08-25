[![License](https://img.shields.io/badge/license-RPL%201.5-blue)](LICENSE.md)
[![GitHub Release Date](https://img.shields.io/github/release-date/denis-peshkov/Cross.Identity?label=released)](https://github.com/denis-peshkov/Cross.Identity/releases)
[![NuGetVersion](https://img.shields.io/nuget/v/Cross.Identity.svg)](https://nuget.org/packages/Cross.Identity/)
[![NugetDownloads](https://img.shields.io/nuget/dt/Cross.Identity.svg)](https://nuget.org/packages/Cross.Identity/)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=denis-peshkov.Cross.Identity&metric=coverage)](https://sonarcloud.io/summary/new_code?id=denis-peshkov.Cross.Identity)
[![issues](https://img.shields.io/github/issues/denis-peshkov/Cross.Identity)](https://github.com/denis-peshkov/Cross.Identity/issues)
[![.NET PR](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml/badge.svg?event=pull_request)](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml)

![Size](https://img.shields.io/github/repo-size/denis-peshkov/Cross.Identity)
[![GitHub contributors](https://img.shields.io/github/contributors/denis-peshkov/Cross.Identity)](https://github.com/denis-peshkov/Cross.Identity/contributors)
[![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/denis-peshkov/Cross.Identity/latest?label=new+commits)](https://github.com/denis-peshkov/Cross.Identity/commits/master)
![Activity](https://img.shields.io/github/commit-activity/w/denis-peshkov/Cross.Identity)
![Activity](https://img.shields.io/github/commit-activity/m/denis-peshkov/Cross.Identity)
![Activity](https://img.shields.io/github/commit-activity/y/denis-peshkov/Cross.Identity)

# Cross.Identity

A .NET identity and authentication library: configurable scenarios (registration, sign-in, password recovery, token issuance and refresh), JWT, Argon2, email/SMS verification, and a process engine with JSON-defined flows.

## Features

- **Process Engine** — runs scenarios (flows) from JSON definitions with sequential steps.
- **Flows** — registration, password/code sign-in, forgot password, token, refresh token, get user, request and verify codes (email/SMS).
- **JWT** — issue and validate access/refresh tokens, configurable claims and lifetimes.
- **Security** — password hashing (Argon2), one-time codes; phone number inputs must be E.164 (e.g. `+79161234567`).
- **Channels** — email and SMS (code delivery via Cross.Messaging).
- **External OAuth** — Google, Microsoft, GitHub, Apple; OAuth state in the database (`auth.ExternalLoginStates`), multi-instance without sticky sessions.
- **Forms** — declarative field definitions and validation rules (equal, requiredIf, atLeastOneRequired, etc.).
- **Licensing (JWT)** — Peshkov license key check on the first flow call; without a key in dev/test, execution continues with a warning in logs.

## Phone numbers (E.164)

Cross.Identity accepts **only** E.164 phone numbers, for example `+79161234567`.

- Gate: `collectForm` fields with `"type": "PhoneNumber"` — validated via `PhoneE164.IsValid` and stored with `PhoneE164.Require`.
- Downstream (`UserService`, lookups, OTP) **trust** the bag value and do not re-validate/normalize.

### Host helper: `PhoneE164`

Use when the host fills the bag **without** going through `collectForm` (or before it):

- **File:** [`Cross.Identity/Helpers/PhoneE164.cs`](Cross.Identity/Helpers/PhoneE164.cs)
- **Namespace:** `Cross.Identity.Services.Crypto`

| Method | Role |
|--------|------|
| `IsValid` / `Require` | Check or enforce already-E.164 |
| `Normalize` / `NormalizeOrThrow` / `Ensure` | Convert national / free-form input to E.164 |

```csharp
using Cross.Identity.Services.Crypto;

var phoneNumber = PhoneE164.Ensure(dto.PhoneNumber, defaultRegion: "RU");
bag["PhoneNumber"] = phoneNumber;
```

No DI registration is required.

## Host-supplied client context (`HostSuppliedClientContext`)

The host Web API sets optional `collectForm.IpAddress`, `UserAgent`, and `DeviceFingerprint` from **server-side** metadata before calling the library ([`HostSuppliedClientContext`](../Cross.Identity/ProcessEngine/Core/HostSuppliedClientContext.cs)). On refresh, the library compares them with `Created*` captured when the session started (family anchor). Use the **same host-derived sources** on login and every refresh. Details: [`FLOWS.md`](Cross.Identity/FLOWS.md) — Host-supplied client context.

```csharp
using Cross.Identity.ProcessEngine.Core;

var bag = new Dictionary<string, object?> { /* credentials, tokens, … */ };

bag["collectForm.IpAddress"] = httpContext.Connection.RemoteIpAddress?.ToString();
bag["collectForm.UserAgent"] = httpContext.Request.Headers.UserAgent.ToString();
bag["collectForm.DeviceFingerprint"] = deviceFingerprintFromHost; // optional

await flowExecutor.ExecuteAsync(bag, "main", FlowOperationEnum.Token, ct);
```

## Requirements

- .NET 8.0

## Repository structure

```
Cross.Identity.slnx
├── Cross.Identity/                     # NuGet library
│   ├── Dtos/                           #
│   ├── Entities/, Infrastructure/      # EF Core (users, tokens, verifications, external login)
│   ├── Enums/                          #
│   ├── Extensions/                     #
│   ├── Helpers/                        # PhoneE164
│   ├── Licensing/                      # Peshkov JWT license (Accessor, Validator, ProductInfo)
│   ├── Services/                       # User, Code, JwtToken; Crypto/, ExternalOAuth/
│   ├── Options/                        # AuthenticationOptions, IdentityServiceConfiguration
│   ├── ProcessEngine/
│   │   ├── Core/                       # Bag, StepRegistry, ProcessLoader, Forms/validation
│   │   ├── Definitions/                # Flows/*.json, Templates/, Providers/
│   │   └── Steps/, Factories/          # Steps and their DI factories
│   ├── FlowExecutor.cs, IFlowExecutor.cs
│   ├── FLOWS.md                        # Flow and step documentation
│   └── config.nuspec
├── Cross.Identity.Tests/               # NUnit (unit + integration)
├── Sample.Api/                         # Minimal API example (ASP.NET Core)
├── .cursor/triage/docs/                # Automated triage reports (.data/, ci-report-*.md)
├── .github/workflows/                  # dotnet.yml, triage.yml
├── Infrastructure/Scripts/             # DbUp DDL: SqlServer / PostgreSQL / MySQL (see Scripts README)
├── RefreshToken.md
├── CONTRIBUTING.md
├── LICENSE.md
└── README.md
```

## Usage

1. **Register `IdentityContext`** in the host application. `AddCrossIdentity` does **not** register `DbContext`.

   `IdentityContext` rotates `ConcurrencyStamp` on tracked insert/update inside `SaveChanges` / `SaveChangesAsync` for all `IHasConcurrencyStamp` entities. Hosts do **not** need to call `AddInterceptors`. This works with both `AddDbContext` and pooled registration (`AddDbContextPool` / `AddPooledDbContextFactory`).

```csharp
services.AddDbContext<IdentityContext>(options =>
    options
        // SQL Server:
        .UseSqlServer(connectionString)
        // PostgreSQL: .UseNpgsql(connectionString)
        // MySQL:      .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        // Test:       .UseInMemoryDatabase("…")
        );
```

   Apply the matching DDL under [`Infrastructure/Scripts`](Infrastructure/Scripts/README.md) (`SqlServer`, `PostgreSQL`, or `MySQL`). The EF model has no provider-specific column types; the host owns the database package and migrations.

   Note: bulk `ExecuteUpdateAsync` / `ExecuteDeleteAsync` bypass `SaveChanges` and automatic `ConcurrencyStamp` rotation. Prefer tracked `SaveChanges`. If you must use bulk APIs: filter by the **original** stamp, treat **0 affected rows** as a concurrency conflict, and assign a **new** stamp only in `ExecuteUpdateAsync` (`SetProperty`). `ExecuteDeleteAsync` has no SET — put the stamp only in the WHERE.

2. **Register Cross.Identity** services:

```csharp
services.AddCrossIdentity(configuration);
// Registers: IFlowExecutor, StepRegistry, all IStepFactory, UserService, CodeService, JwtTokenService,
// LicenseAccessor, LicenseValidator, ILicenseProductInfo, definition providers (files + embedded), forms, etc.
```

3. **Authorize user-scoped flows in the host.** Flows such as `CommunicationEndpoints*`, `ExternalLogin` (link), `ExternalLoginUnlink`, `ExternalLoginGetAll`, and `LogoutAll` take `UserAccountId` — the host must ensure the caller may act as that account before `ExecuteAsync`. `Logout` takes access-token `Jti` and `RefreshToken` takes refresh-token `Jti` (the host validates the client token and extracts `jti`). Details: [`FLOWS.md`](Cross.Identity/FLOWS.md). `Token` still accepts credentials or a code in the payload.

License key (optional) — `CrossIdentity` section in configuration or the `CrossIdentity__LicenseKey` environment variable:

```json
{
  "CrossIdentity": {
    "LicenseKey": "<license key here>"
  }
}
```

Validation runs automatically on the **first** call to `IFlowExecutor.ExecuteAsync` — no extra code required. Keys: [peshkov.biz](https://peshkov.biz).

Behavior:

| Scenario | Result |
|----------|--------|
| Key not set | `LogCritical`, flow runs (dev/test) |
| Invalid JWT | `LogError`, flow runs |
| Expired / wrong product type | `LogError` + `LogCritical`, flow runs |
| Valid key | `LogInformation` with edition and expiration date |

3. **Running a scenario** — in a controller or minimal API, pass the request body as a dictionary and call:

```csharp
var result = await _flowExecutor.ExecuteAsync(
    input: requestBodyAsDictionary,
    flow: "main",
    operation: FlowOperationEnum.Token,
    cancellationToken);
// result.Data — dictionary of fields from the collectResult step (e.g. access_token, refresh_token, LastCode).
```

4. **Flow definitions** — JSON in `ProcessEngine/Definitions/Flows/` (and optionally from the file system). File names: `{flow}.{Operation}.json` (e.g. `main.Token.json`, `main.Register.json`). See [FLOWS.md](Cross.Identity/FLOWS.md) for detailed flow and step documentation.

## Dependencies (NuGet)

- Cross.ErrorHandlers
- Cross.Headers
- Cross.Messaging
- Cross.PepperVault
- Konscious.Security.Cryptography.Argon2
- Microsoft.EntityFrameworkCore (+ InMemory, Relational)
- Microsoft.Extensions.Http
- Microsoft.IdentityModel.JsonWebTokens
- PhoneNumbersCore

## Build and tests

```bash
dotnet build
dotnet test
```

## Tests

Canonical rules (naming, AAA, categories, layout, run commands): [`.cursor/rules/300-testing-dotnet.mdc`](.cursor/rules/300-testing-dotnet.mdc).

Summary:

- **NUnit** in `Cross.Identity.Tests/`; categories via `TestCategory` (`Unit` / `Integration` / `Functional`).
- **Method names:** `Given[X]_When[Y]_Then[Z]` (async tests end with `Async`).
- **Layout:** `Identity/FlowTests` (integration), `Identity/StepTests` + `StepFactoryTests` (unit), `Services/` (unit or integration).

```bash
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

## Additional resources

- [CONTRIBUTING.md](CONTRIBUTING.md) — how to contribute: branches, PRs, tests, code style.
- [Infrastructure/Scripts/README.md](Infrastructure/Scripts/README.md) — DbUp DDL for SQL Server, PostgreSQL, and MySQL (`auth` schema).
- [RefreshToken.md](RefreshToken.md) — access/refresh token lifetimes and rotation recommendations.
- [LICENSE.md](LICENSE.md) — license.

## ToDo

- ~~[x] Migrate from System.IdentityModel.Tokens.Jwt to Microsoft.IdentityModel.JsonWebTokens~~
