# Release readiness plan `dev` → `master`

> **Analysis date:** 2026-07-15  
> **Branch:** `dev`  
> **Comparison base:** `master...dev` (merge-base `163b8a5`)  
> **Goal:** exhaustive list of new functionality and verification checklist before merge into `master`  
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker  
> **Sources:** `dotnet test`, `git diff master...dev`, `gh run list` (verified 2026-07-15)
> **Maintenance:** when changing any checklist or migration items, recalculate the summary: `node docs/scripts/release-plan-summary.mjs --write`

**Checklist summary:** **100** items — ✅ **59** (59%) · 🟨 **27** (27%) · ⬜ **14** (14%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---------|----------|
| Commits | 124 |
| Files | 374 |
| Lines | +12 559 / −5 721 |
| Tests (`dotnet test`) | 301 total · 301 passed · 0 failed |

| Area | Files | Role in release |
|---------|--------|---------------|
| Cross.Identity | 138 | NuGet library: JWT, refresh, OAuth flows, licensing, cleanup |
| Cross.Identity.Tests | 62 | Unit + integration (NUnit) |
| Sample.Api | 4 | Minimal API, smoke via Swagger |
| .github/workflows | 2 | CI/CD (`dotnet.yml`, `triage.yml`) |
| .cursor (rules + triage) | 21 | Conventions, automated triage (not runtime) |
| docs + Infrastructure/Scripts | 17+ | SQL schema, release plan, E2E `.http` |
| Removed in-repo packages | ~65 | `Cross.Notification`, `Cross.PepperVault.*` → NuGet |

---

## Table of contents

1. [Complete list of new functionality](#1-complete-list-of-new-functionality)
2. [Breaking changes](#2-breaking-changes-required-for-package-consumers)
3. [Verification checklists by area](#3-checklists-by-functional-blocks)
4. [Automated tests — coverage matrix](#4-automated-tests--coverage-matrix)
5. [Manual/E2E via Sample.Api](#5-manuale2e-testing-via-sampleapi)
6. [CI/CD and infrastructure](#6-cicd-and-infrastructure)
7. [Documentation and release notes](#7-documentation-and-release-notes)
8. [Database migration](#8-database-migration-for-existing-installations)
9. [Blockers and risks](#9-blockers-and-risks-before-merge)
10. [Go / No-Go and execution order](#10-recommended-work-order-release-gate)

---

## 1. Complete list of new functionality

| # | Block | Key changes | Commits/files |
|---|------|-------------------|---------------|
| A | **Registration (US-129)** | `license.Register` — email+password, no `ConfirmPassword`; `createUser` → `sendCode` → `LastCode`+`UserId` | `license.Register.json`, `UserService`, flow tests |
| B | **External OAuth** | Google/Microsoft/GitHub/Apple; steps `initiateExternalLogin` / `completeExternalLogin`; flows `ExternalLogin`, `ExternalLoginCallback`; linking | `ExternalLoginService`, `ExternalOAuthProviders`, 2 JSON flows |
| C | **Refresh Token / "Remember me"** | `AbsoluteExpiresAt`, `FamilyId` chain, rotation, background cleanup | `JwtTokenService`, `RefreshTokenStep`, `ExpiredRefreshTokenCleanupHostedService` |
| D | **JWT licensing** | Validation on first `ExecuteAsync`; section `CrossIdentity:LicenseKey` | `LicenseAccessor`, `LicenseValidator`, `FlowExecutor` |
| E | **Developer Mode** | Codes stored in DB without sending email/SMS | `CodeService`, `SendCodeStep`, `Authentication:DeveloperMode` |
| F | **Token / TokenByCode / RequestCode** | End-to-end scenario request code → token by code | `License_RequestCode_TokenByCode_FlowTests` |
| G | **Reset Password** | Email/SMS notification after password change; new form fields | `ResetPasswordStep`, `license.ResetPassword.json` |
| H | **Infrastructure** | `UnitTests` → `Tests`, CI triage, PR triggers, dependency updates | `.github/workflows/`, `.cursor/triage/` |

---

## 2. Breaking changes (required for package consumers)

### 2.1 API / flow contracts

| Change | Was (master) | Now (dev) | Action |
|-----------|---------------|-------------|----------|
| `GetUser` operation | `FlowOperationEnum.GetUser` | **`GetUserId`** | Update clients: `license/GetUserId` |
| Flow file | `license.GetUser.json` | **`license.GetUserId.json`** | Update custom overrides |
| Removed flows | `license.Auth.json`, `license.register1.json` | removed | Verify no one still calls them |
| `collectResult` response with 1 field | bare value (`"abc"`) | **always an object** `{ "field": "abc" }` | Update deserialization on clients |
| `IFlowExecutor` / `FlowExecutor` | public class | **internal class** | Public contract — only `IFlowExecutor` |

### 2.2 Data model

| Change | Action |
|-----------|----------|
| Removed field `NormalizedEmail` | DB migration: column `NormalizedEmail` → use `Email`; with EF — new migration |
| `RefreshTokenEntity.AbsoluteExpiresAt` | New column; backfill for existing tokens |
| `UserExternalLoginEntity` + FK to `ProviderEntity` | Verify schema and seed providers (Google, Microsoft, …) |

### 2.3 NuGet dependencies

| Was | Now |
|------|-------|
| `System.IdentityModel.Tokens.Jwt` | **`Microsoft.IdentityModel.JsonWebTokens` 8.16** |
| In-repo `Cross.Notification`, `Cross.PepperVault.*` | **`Cross.Messaging`**, **`Cross.PepperVault`** (NuGet) |
| `Magick.NET.Core` | **removed** |
| `Cross.ErrorHandlers` 7.3 → **7.6**, `Cross.Headers` 1.0 → **1.2.1** | Update by consumers on conflicts |

> **Note:** `config.nuspec` is synchronized with `.csproj` (TFM groups, current versions).

---

## 3. Checklists by functional blocks

### A. Registration (`license.Register`)

**Automated tests:** `License_Registration_FlowTests`, `LicenseRegisterFlowTests`, `CreateUser_StepTests`

| # | Check | Type | Status |
|---|----------|-----|--------|
| A1 | Registration with valid email+password → `UserId` + `LastCode` in response | Integration | ✅ `License_Registration_FlowTests` |
| A2 | Re-registration with same email → error | Integration | 🟨 unit `CreateUserAsync_ShouldThrowWhenEmailExists`; flow ⬜ |
| A3 | Password validation (min 8, max 128) | Unit/Integration | ✅ `Handle_InvalidInput_ShouldThrowValidationException` |
| A4 | `ConfirmPassword` no longer required — old clients not broken | Integration | ✅ test without `ConfirmPassword` |
| A5 | Confirmation code stored in `EmailVerifications` | Integration | 🟨 `CodeServiceTests`; DB not verified in registration flow |
| A6 | `createUser` field mapping (`FullName`, `Company`, flags) | Integration | ⬜ no test for extended mapping |

---

### B. External OAuth Login

**Automated tests:** `ExternalLoginServiceTests`, `License_ExternalOAuth_FlowTests`, step/factory unit tests.

#### B1. Configuration (required before any manual test)

```json
{
  "Authentication": {
    "ExternalLogin": {
      "CallbackUrl": "https://your-spa/callback",
      "StateLifetime": "00:10:00",
      "Providers": {
        "Google": {
          "ClientId": "...",
          "ClientSecret": "...",
          "IsEnabled": true
        }
      }
    }
  }
}
```

Env: `Authentication__ExternalLogin__CallbackUrl`, `Authentication__ExternalLogin__Providers__Google__ClientId`, etc.

| # | Check | Type | Status |
|---|----------|-----|--------|
| B1 | `CallbackUrl` set — otherwise `InvalidOperationException` | Unit | ✅ `ExternalLoginServiceTests` |
| B2 | Provider not in config → `ValidationException` | Unit | ✅ |
| B3 | Provider not in DB (`Providers` table) / disabled → `NotFoundException` | Unit | ✅ |
| B4 | **Initiate:** `POST license/ExternalLogin` → `{ url }` with OAuth redirect | Integration | ✅ `License_ExternalOAuth_FlowTests` |
| B5 | **Callback:** `POST license/ExternalLoginCallback` → tokens + `user_id` | Integration | ✅ `ExternalLoginCallback_ShouldReturnTokens_*` |
| B6 | OAuth error (`Error`, `ErrorDescription`) → correct error | Unit + Integration | ✅ |
| B7 | State TTL expired → rejection | Unit | ✅ `CompleteAsync_ShouldThrow_WhenStateExpired` |
| B8 | **New user** — auto-provision | Integration | ✅ callback creates user + external login in DB |
| B9 | **Existing user** — login by provider+subject | Unit | ✅ `CompleteAsync_ShouldReturnExistingUser_*` |
| B10 | **Linking:** `LinkUserId` → `is_linking: true` | Unit + Integration | 🟨 unit `CompleteAsync_ShouldLinkProviderToExistingUser`; flow ⬜ |
| B11 | **Linking without auth** → `NotAuthorizedException` | Unit | ✅ |
| B12 | Re-linking same provider → `ValidationException` | Unit | ✅ |
| B13 | Google / Microsoft / GitHub / Apple — profile | Unit / Manual | 🟨 Google in flow tests; others — unit fetch only |
| B14 | Multi-instance: OAuth state in DB (`ExternalLoginStates`) | Arch review | ✅ shared DB, no sticky |

---

### C. Refresh Token / Remember Me

Implemented via `AbsoluteExpiresAt` + `FamilyId` (see `RefreshToken.md`), not via a separate `RememberMe` flag.

| # | Check | Type | Status |
|---|----------|-----|--------|
| C1 | Access+refresh pair issued on `license.Token` (password) | Integration | ✅ |
| C2 | Pair issued on `license.TokenByCode` | Integration | ✅ |
| C3 | Rotation: old refresh invalidated, new one works | Unit | ✅ `JwtTokenServiceTests` |
| C4 | `AbsoluteExpiresAt` preserved on rotation (chain) | Unit | ✅ |
| C5 | Refresh after `AbsoluteExpiresAt` → rejection | Unit | 🟨 logic in `ValidateRefreshTokenAsync`; no dedicated test for expired absolute |
| C6 | `RefreshTokenAbsoluteExpires` in config affects new chains | Unit | ✅ `GenerateRefreshTokenAsync_ShouldUseConfiguredRollingLifetime` |
| C7 | `ExpiredRefreshTokenCleanupHostedService` removes expired tokens | Unit | ✅ |
| C8 | Cleanup interval `Authentication:TokenCleanupInterval` (default 1h) | Manual | ⬜ |
| C9 | `license.RefreshToken` flow end-to-end | Integration | ✅ `License_RefreshToken_FlowTests` |
| C10 | Reuse of old refresh after rotation → rejection | Integration | 🟨 flow issues new pair (`License_RefreshToken_FlowTests`); repeat call with old token ⬜ |

**Config for verification:**

```json
"Authentication": {
  "Jwt": {
    "AccessTokenExpires": "00:15:00",
    "RefreshTokenExpires": "30.00:00:00",
    "RefreshTokenAbsoluteExpires": "60.00:00:00"
  },
  "TokenCleanupInterval": "01:00:00"
}
```

---

### D. JWT licensing

| # | Check | Type | Status |
|---|----------|-----|--------|
| D1 | Key not set → `LogCritical`, flow works | Unit | ✅ |
| D2 | Invalid JWT → `LogError`, flow works | Unit | ✅ |
| D3 | Expired key → `LogError` + `LogCritical` | Unit | ✅ |
| D4 | Valid key → `LogInformation` with edition/expiry | Unit | ✅ |
| D5 | Validation only on **first** call (singleton flag) | Unit | ✅ |
| D5b | `CheckLicense` on first `ExecuteAsync`, flow not blocked | Integration | ✅ `License_LicenseCheck_FlowTests` |
| D6 | `CrossIdentity:LicenseKey` from appsettings | Manual | 🟨 `LicenseAccessor` + `Sample.Api` appsettings; E2E ⬜ |
| D7 | `CrossIdentity__LicenseKey` from env | Manual | ⬜ |
| D8 | Invalid `ProductType` in key | Unit | ✅ |
| D9 | **Production policy:** hard-fail without key? (currently soft-fail) | Product decision | ⬜ |

---

### E. Developer Mode

| # | Check | Type | Status |
|---|----------|-----|--------|
| E1 | `Authentication:DeveloperMode=true` → code in DB, email/SMS **not** sent | Unit | ✅ |
| E2 | `DeveloperMode=false` → send + store | Unit | ✅ |
| E3 | `LastCode` returned in flow response (for dev) | Integration | ✅ |
| E4 | Production: `DeveloperMode` **not set** or `false` | Manual | ⬜ |
| E5 | `SendCodeStep` also respects DeveloperMode | Unit | 🟨 code + flow with `DeveloperMode=true`; dedicated step test ⬜ |

---

### F. Token / TokenByCode / RequestCode

| # | Check | Type | Status |
|---|----------|-----|--------|
| F1 | `license.Token` — password OR code (validation `atLeastOneRequired`) | Integration | ✅ |
| F2 | `license.TokenByCode` — code only | Integration | ✅ |
| F3 | RequestCode → TokenByCode end-to-end scenario | Integration | ✅ |
| F4 | Invalid code → `IsInvalidCode` / empty token | Integration | 🟨 happy path ✅; negative scenario ⬜ |
| F5 | Expired code (TTL) | Unit | ✅ `CodeServiceTests` |
| F6 | `MaxAttempts` exceeded (3) | Unit | ✅ `CodeServiceTests` |

---

### G. Reset Password / Forgot Password

| # | Check | Type | Status |
|---|----------|-----|--------|
| G1 | `license.ForgotPassword` | Integration | ✅ |
| G2 | `ResetPasswordStep` — password change + email notification | Unit | ✅ |
| G3 | Notification on send failure — logged, flow does not fail | Unit | ✅ |
| G4 | `license.ResetPassword.json` — `passwordKey: collectForm.Password`, form: `Email`, `Code`, `Password` | Code review | ✅ |
| G5 | Integration flow test for `license.ResetPassword` | Integration | ✅ `License_ResetPassword_FlowTests` |
| G6 | Old password / code + new password — business logic | Manual | ⬜ |

> **Recommendation:** before release, run `license.ResetPassword` manually via Sample.Api (password change + notification).

---

### H. Other changes

| # | Check | Status |
|---|----------|--------|
| H1 | `GetUserId` flow returns `{ user_id }` | ✅ `License_LicenseCheck_FlowTests` |
| H2 | `FlowExecutor` — `collectResult` always an object | ✅ flow tests return `Dictionary<string, object?>` |
| H3 | `UserService` — provisioning, `ValidateCode`, `SetPassword` | ✅ `UserServiceTests` |
| H4 | Removal of `NormalizedEmail` — case-insensitive email lookup | 🟨 `ToLowerInvariant` in `UserService`; explicit lookup test ⬜ |
| H5 | `PasswordHasher` + Pepper via NuGet `Cross.PepperVault` | 🟨 `PasswordHasherTests` + `Sample.Api` Pepper in appsettings |
| H6 | JWT encryption (`UseEncryption`, `EncryptionKey` Base64 32 bytes) | ⬜ tests with `UseEncryption=false` |
| H7 | Migration to `Microsoft.IdentityModel.JsonWebTokens` | 🟨 package referenced; downstream validation ⬜ |

---

## 4. Automated tests — coverage matrix

```bash
# Full run
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj

# By category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# With coverage (opencover)
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

| Area | Unit | Integration Flow | Gap |
|---------|------|------------------|--------|
| Registration | ✅ | ✅ | — |
| Token / TokenByCode | ✅ | ✅ | — |
| RefreshToken | ✅ | ✅ | `License_RefreshToken_FlowTests` |
| External OAuth | ✅ (service) | ✅ | `License_ExternalOAuth_FlowTests` |
| ResetPassword | ✅ (step) | ✅ | `License_ResetPassword_FlowTests` |
| Licensing | ✅ | ✅ | `License_LicenseCheck_FlowTests` |
| ForgotPassword | ✅ | ✅ | `ForgotPassword_StepTests`, `ForgotPassword_StepFactoryTests`, `License_ForgotPassword_FlowTests` |

**Current status:** 301/301 passed.

---

## 5. Manual/E2E testing via Sample.Api

```bash
dotnet run --project Sample.Api
# Swagger: POST /api/identity/{flow}/{operation}
# Or: rest-client/Sample.Api.http (10 license/* operations)
```

| Scenario | Endpoint | Body (example) |
|----------|----------|---------------|
| Registration | `license/Register` | `{ "Email": "...", "Password": "..." }` |
| Request code | `license/RequestCode` | `{ "Email": "...", "Ttl": "00:05:00" }` |
| Token by code | `license/TokenByCode` | `{ "Email": "...", "Code": "..." }` |
| Token by password | `license/Token` | `{ "Email": "...", "Password": "..." }` |
| Refresh | `license/RefreshToken` | `{ "RefreshToken": "..." }` |
| GetUserId | `license/GetUserId` | `{ "Email": "..." }` |
| OAuth start | `license/ExternalLogin` | `{ "Provider": "Google" }` |
| OAuth callback | `license/ExternalLoginCallback` | `{ "Code": "...", "State": "..." }` |

**Before E2E:**

1. Real DB (not InMemory only) — PostgreSQL/SQL Server
2. Seed `Providers` table (Google, Microsoft, …)
3. Configure OAuth credentials + `CallbackUrl`
4. `Authentication:DeveloperMode=false` + working Cross.Messaging (email/SMS)
5. `CrossIdentity:LicenseKey` — valid test key

---

## 6. CI/CD and infrastructure

| # | Check | Status |
|---|----------|--------|
| CI1 | `dotnet.yml` — build + test on PR to `dev`/`master`/`release/*`/`hotfix/*`; push on `feature/*`/`fix/*`/`chore/*` | ✅ restore/build/test; `branch-policy.yml` enforces owner-only `master`/`release/*`/`hotfix/*` |
| CI2 | SonarCloud quality gate wait on PR | ✅ PR [#5](https://github.com/denis-peshkov/Cross.Identity/pull/5#issuecomment-4834799217): QG **passed**, 88.7% coverage on new code; [`dotnet.yml`](.github/workflows/dotnet.yml) — `sonar.qualitygate.wait=true` on `pull_request` |
| CI3 | `triage.yml` — automated PR triage | ✅ latest run ok |
| CI4 | GitVersion: `dev` is now **not** a release branch | 🟨 config changed; merge behavior not verified |
| CI5 | NuGet pack from `config.nuspec` — dependencies up to date | ✅ synchronized with `.csproj` |

---

## 7. Documentation and release notes

| # | Document | Status | Action |
|---|----------|--------|----------|
| DOC1 | `README.md` | ✅ updated (licensing, structure) | — |
| DOC2 | `FLOWS.md` | ✅ | Updated (18 flows, External OAuth) |
| DOC3 | `RefreshToken.md` | ✅ current | — |
| DOC4 | `config.nuspec` releaseNotes | 🟨 | licensing + OAuth; full breaking list ⬜ |
| DOC5 | `LICENSE.md` | ✅ | updated (peshkov.biz) |
| DOC6 | Migration guide for consumers | ⬜ | `docs/MIGRATION.md` not created |
| DOC7 | CHANGELOG / GitHub Release | ⬜ | Before release |

---

## 8. Database migration (for existing installations)

```sql
-- Example steps (adjust for your DBMS / EF migrations)

-- 1. Refresh tokens: new column
ALTER TABLE RefreshTokens ADD AbsoluteExpiresAt datetime2 NOT NULL
  DEFAULT DATEADD(day, 30, CreatedAt);

-- 2. Users: remove NormalizedEmail (if present)
-- UPDATE Users SET Email = NormalizedEmail WHERE Email IS NULL;
-- ALTER TABLE Users DROP COLUMN NormalizedEmail;

-- 3. Providers: seed OAuth providers
INSERT INTO Providers (Id, Name, IsEnabled) VALUES (...);

-- 4. UserExternalLogins: FK to Providers

-- 5. OAuth state (CSRF / one-time), instead of in-memory cache
CREATE TABLE auth.ExternalLoginStates (
  ExternalLoginStateId bigint IDENTITY(1,1) NOT NULL,
  Nonce nvarchar(32) NOT NULL,
  Provider nvarchar(64) NOT NULL,
  ReturnUrl nvarchar(512) NULL,
  LinkUserId uniqueidentifier NULL,
  ExpiresAt datetime2(7) NOT NULL,
  CreatedAt datetime2(7) NOT NULL,
  CONSTRAINT PK_auth_ExternalLoginStates PRIMARY KEY (ExternalLoginStateId),
  CONSTRAINT UX_auth_ExternalLoginStates_Nonce UNIQUE (Nonce)
);
CREATE INDEX IX_auth_ExternalLoginStates_ExpiresAt ON auth.ExternalLoginStates (ExpiresAt);
```

| # | Check | Status |
|---|----------|--------|
| M1 | EF migration / SQL scripts created and tested on staging | ✅ `Infrastructure/Scripts/2_Initial/*` |
| M2 | Backfill `AbsoluteExpiresAt` for existing refresh tokens | 🟨 column in initial schema; no `1_PreDeployment` upgrade script |
| M3 | Seed `Providers` for OAuth | ✅ `4_SeedData/4_01_auth_Providers.sql` |
| M4 | Rollback plan | 🟨 SQL scripts documented; explicit rollback runbook ⬜ |

---

## 9. Blockers and risks before merge

| Priority | Issue | Recommendation |
|-----------|----------|--------------|
| **P0** | `license.ResetPassword.json` — `passwordKey` | ✅ Fixed + `License_ResetPassword_FlowTests` |
| **P0** | No integration flow tests for External OAuth | ✅ `License_ExternalOAuth_FlowTests` |
| **P1** | `config.nuspec` — outdated dependencies | ✅ Synchronized with `.csproj` |
| **P1** | `FLOWS.md` does not match code | ✅ Updated |
| **P1** | Breaking change `collectResult` (1 field) | 🟨 document in `docs/MIGRATION.md` (file ⬜) |
| **P1** | OAuth state — multi-instance | ✅ `auth.ExternalLoginStates` instead of `IMemoryCache` |
| **P2** | License soft-fail in production | Product decision |
| **P2** | No EF migrations in repo | 🟨 SQL scripts in `Infrastructure/Scripts/` (reference copy); no EF `Migrations/` |
| **P2** | `Sample.Api` — InMemory DB, OAuth placeholders | 🟨 `rest-client/Sample.Api.http` + `Authentication:ExternalLogin` skeleton; Google disabled |

---

## 10. Recommended work order (release gate)

Execute in order; proceed to the next step after closing the previous one (or an explicit "skip" decision recorded in the PR).

- ✅ **1. P0 blockers** — `license.ResetPassword.json`, `License_ResetPassword_FlowTests`, `License_ExternalOAuth_FlowTests`
- 🟨 **2. Tests** — `dotnet test` 301/301 ✅ locally; coverage (opencover) ⬜
- 🟨 **3. Documentation and package** — `config.nuspec`, `FLOWS.md` ✅; `docs/MIGRATION.md` + CHANGELOG ⬜
- 🟨 **4. DB** — SQL scripts ✅; staging apply + backfill/rollback runbook ⬜
- 🟨 **5. E2E Sample.Api** — `rest-client/Sample.Api.http` prepared (10 ops); manual run ⬜
- 🟨 **6. OAuth** — integration flow tests ✅ (mocked Google); real Google E2E ⬜
- 🟨 **7. CI** — `dotnet.yml` ✅ on `dev`; SonarCloud QG ✅ on PR #5; triage ✅
- 🟨 **8. Breaking changes** — described in plan; migration guide + consumer alignment ⬜
- ⬜ **9. Release** — merge into `master`, tag, NuGet publish

### Minimum go/no-go checklist

- ✅ All 301 tests green (locally)
- ✅ P0 fixed (`ResetPassword` JSON + flow tests, External OAuth flow tests)
- 🟨 10 flow operations — `rest-client/Sample.Api.http` ready; manual run ⬜
- 🟨 OAuth initiate+callback (integration ✅ mocked; manual Google E2E ⬜)
- ⬜ Refresh rotation + absolute expiry verified manually
- ⬜ DeveloperMode disabled in prod config
- ⬜ LicenseKey configured (or consciously soft-fail)
- 🟨 Breaking changes — in plan; `docs/MIGRATION.md` ⬜
- ✅ `config.nuspec` synchronized
- 🟨 CI green on PR (`dotnet.yml` ✅ on `dev`; repeat after final push)
