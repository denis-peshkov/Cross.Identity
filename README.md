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
- **Security** — password hashing (Argon2), one-time codes, phone normalization.
- **Channels** — email and SMS (code delivery via Cross.Messaging).
- **External OAuth** — Google, Microsoft, GitHub, Apple; OAuth state in the database (`auth.ExternalLoginStates`), multi-instance without sticky sessions.
- **Forms** — declarative field definitions and validation rules (equal, requiredIf, atLeastOneRequired, etc.).
- **Licensing (JWT)** — Peshkov license key check on the first flow call; without a key in dev/test, execution continues with a warning in logs.

## Requirements

- .NET 8.0

## Repository structure

```
Cross.Identity.slnx
├── Cross.Identity/                     # NuGet library
│   ├── FlowExecutor.cs, IFlowExecutor.cs
│   ├── Entities/, Infrastructure/      # EF Core (users, tokens, verifications, external login)
│   ├── Services/                       # User, Code, JwtToken; Crypto/; ExternalOAuth/
│   ├── Licensing/                      # Peshkov JWT license (Accessor, Validator, ProductInfo)
│   ├── Options/                        # AuthenticationOptions, IdentityServiceConfiguration
│   ├── Extensions/, Helpers/, Dtos/, Enums/
│   ├── ProcessEngine/
│   │   ├── Core/                       # Bag, StepRegistry, ProcessLoader, Forms/validation
│   │   ├── Steps/, Factories/          # Steps and their DI factories
│   │   └── Definitions/                # Flows/*.json, Templates/, Providers/
│   ├── FLOWS.md                        # Flow and step documentation
│   └── config.nuspec
├── Cross.Identity.Tests/               # NUnit (unit + integration)
├── Sample.Api/                         # Minimal API example (ASP.NET Core)
├── .cursor/triage/docs/                # Automated triage reports (.data/, ci-report-*.md)
├── .github/workflows/                  # dotnet.yml, triage.yml
├── Infrastructure/Scripts/             # DbUp SQL example for auth schema (copy; see README)
├── RefreshToken.md
├── CONTRIBUTING.md
├── LICENSE.md
└── README.md
```

## Usage

1. **Registration** in the application (ASP.NET Core):

```csharp
services.AddCrossIdentity(configuration);
// Registers: IFlowExecutor, StepRegistry, all IStepFactory, UserService, CodeService, JwtTokenService,
// LicenseAccessor, LicenseValidator, ILicenseProductInfo, definition providers (files + embedded), forms, etc.
```

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

2. **Running a scenario** — in a controller or minimal API, pass the request body as a dictionary and call:

```csharp
var result = await _flowExecutor.ExecuteAsync(
    input: requestBodyAsDictionary,
    flow: "main",
    operation: FlowOperationEnum.Token,
    cancellationToken);
// result.Data — dictionary of fields from the collectResult step (e.g. access_token, refresh_token, LastCode).
```

3. **Flow definitions** — JSON in `ProcessEngine/Definitions/Flows/` (and optionally from the file system). File names: `{flow}.{Operation}.json` (e.g. `main.Token.json`, `main.Register.json`). See [FLOWS.md](Cross.Identity/FLOWS.md) for detailed flow and step documentation.

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

### Categories (NUnit)

Constants — `Cross.Identity.Tests.Common.TestCategory`, attributes: `[Category(TestCategory.UNIT)]`, `[Category(TestCategory.INTEGRATION)]`, `[Category(TestCategory.FUNCTIONAL)]`.

| Category | Purpose |
|----------|---------|
| **UNIT** | Mocks, single component, no InMemory EF |
| **INTEGRATION** | `EFTestsBase` (InMemory EF + real services), `RunFlowCommandHandlerTestsBase` / `Identity/FlowTests` (end-to-end process engine) |
| **FUNCTIONAL** | Reserved (E2E / TestServer / external dependencies), not used yet |

Run examples:

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

### Method naming

**Given_When_Then** convention:

- **Given** — context/preconditions.
- **When** — action.
- **Then** — expected result.

Example: `ExistingUser_RequestCode_SendsCodeAndReturnsLastCode`.

Layout: `Cross.Identity.Tests/Identity/` — FlowTests (integration), StepTests and StepFactoryTests (unit); `Services/` — unit or integration depending on the base class (`EFTestsBase` → integration).

## Additional resources

- [CONTRIBUTING.md](CONTRIBUTING.md) — how to contribute: branches, PRs, tests, code style.
- [Infrastructure/Scripts/README.md](Infrastructure/Scripts/README.md) — DbUp SQL example for the `auth` schema.
- [RefreshToken.md](RefreshToken.md) — access/refresh token lifetimes and rotation recommendations.
- [LICENSE.md](LICENSE.md) — license.

## ToDo

- ~~[x] Migrate from System.IdentityModel.Tokens.Jwt to Microsoft.IdentityModel.JsonWebTokens~~
