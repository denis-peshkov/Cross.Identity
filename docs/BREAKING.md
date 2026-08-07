# Breaking changes (NuGet consumers)

Breaking changes for **Cross.Identity**, grouped by **from → to** package version. Apply every section in order when skipping releases (e.g. `1.4 → 1.10` = all sections below).

| Upgrade path         | Section                                     |
|----------------------|---------------------------------------------|
| `≤ 1.4.x` → `1.5.0+` | [From ≤1.4.x to 1.5.0](#from-14x-to-150)    |
| `1.5.x` → `1.6.0+`   | [From 1.5.x to 1.6.0](#from-15x-to-160)     |
| `1.6.x` → `1.7.0+`   | [From 1.6.x to 1.7.0](#from-16x-to-170)     |
| `1.9.x` → `1.10.0+`  | [From 1.9.x to 1.10.0](#from-19x-to-1100)   |
| `1.10.x` → `1.11.0+` | [From 1.10.x to 1.11.0](#from-110x-to-1110) |

`1.7.x` → `1.8.0` / `1.8.x` → `1.9.0` have no breaking API or flow-contract changes.

DB scripts: [`Infrastructure/Scripts/README.md`](../Infrastructure/Scripts/README.md).

Breaking-change details live **only** in this file. [`Cross.Identity/config.nuspec`](../Cross.Identity/config.nuspec) `releaseNotes` should link here and must not duplicate the versioned sections.

When shipping a new breaking change: append a **From X.Y.Z to A.B.C** section (chronological order) and prefix the **PR title** with `BREAKING:`.

---

## From ≤1.4.x to 1.5.0

### Built-in flow rename: `license` → `main`

| Area | Was (≤1.4) | Now (1.5+) |
|------|------------|------------|
| Flow id | `ExecuteAsync(..., "license", operation, ...)` | `ExecuteAsync(..., "main", operation, ...)` |
| Definition files | `license.{Operation}.json` | `main.{Operation}.json` |
| Sample/API path | `/api/identity/license/{Operation}` | `/api/identity/main/{Operation}` |

Removed demo flows: `game.*`, `shop.*`, `edoctors.*`.

**Action:** rename flow id, override files, and client routes to `main`.

### Operation rename: `GetUser` → `GetUserId`

| Area | Was | Now |
|------|-----|-----|
| Enum / route | `GetUser` | `GetUserId` |
| Flow file | `license.GetUser.json` / `main.GetUser.json` | `main.GetUserId.json` |

**Action:** update clients, custom overrides, and hardcoded operation names.

### `collectResult` with a single field

| Was | Now |
|-----|-----|
| bare scalar (`"abc"`) | always an object (`{ "fieldName": "abc" }`) |

**Action:** adjust client deserialization.

### Public executor surface

`FlowExecutor` is **internal**. Use **`IFlowExecutor`** only.

### Licensing

JWT license validation runs on the first `IFlowExecutor.ExecuteAsync` call. Calls fail without a valid key.

- `CrossIdentity:LicenseKey` in configuration, or
- `CrossIdentity__LicenseKey` environment variable

### Data model / schema (host-owned DB)

Reference DDL: `Infrastructure/Scripts/{SqlServer,PostgreSQL,MySQL}/`.

- Prefer `Email` over removed `NormalizedEmail`.
- `RowVersion` → `ConcurrencyStamp` (`IHasConcurrencyStamp` + interceptor); update host EF mappings and SQL scripts accordingly.
- `RefreshToken.AbsoluteExpiresAt` — add column and backfill.

### Dependencies

- JWT: `Microsoft.IdentityModel.JsonWebTokens`
- Messaging / pepper: `Cross.Messaging`, `Cross.PepperVault` (NuGet)
- Align `Cross.ErrorHandlers` / `Cross.Headers` if the app pins older versions

---

## From 1.5.x to 1.6.0

### Removed operation: `TokenByCode`

OTP exchange is handled by **`Token`** (`main.Token`) with `{ Email|Phone, Code }` (same payload shape as before).

| Area | Was (1.5) | Now (1.6+) |
|------|-----------|------------|
| Operation / route | `TokenByCode` | `Token` |
| Flow file | `main.TokenByCode.json` | removed — use `main.Token.json` |

**Action:** call `main` / `Token` instead of `TokenByCode`; drop custom `TokenByCode` overrides.

---

## From 1.6.x to 1.7.0

### External OAuth step type rename

Flow operations stay `ExternalLogin` / `ExternalLoginCallback`. Step **type** names in JSON and DI changed:

| Was (1.6) | Now (1.7+) |
|-----------|------------|
| `InitiateExternalLogin` | `ExternalLoginInitiate` |
| `CompleteExternalLogin` | `ExternalLoginComplete` |

**Action:** update custom flow overrides that reference the old step types; stock `main.ExternalLogin*.json` already use the new names.

---

## From 1.9.x to 1.10.0

### `IJwtTokenService.GetClaimValueAsync` → `GetClaimValue`

Claim extraction from a compact JWT is in-memory only (no I/O). The fake-async API was removed.

| Was (1.9) | Now (1.10+) |
|-----------|-------------|
| `Task<string?> GetClaimValueAsync(...)` | `string? GetClaimValue(...)` |

**Action:** replace `await jwt.GetClaimValueAsync(...)` with `jwt.GetClaimValue(...)`.

### `IJwtTokenService.GenerateIdTokenAsync` → `GenerateIdToken`

Id-token issuance is in-memory only (sign JWT, no I/O). The fake-async API was removed.

| Was (1.9) | Now (1.10+) |
|-----------|-------------|
| `Task<string> GenerateIdTokenAsync(...)` | `string GenerateIdToken(...)` |

**Action:** replace `await jwt.GenerateIdTokenAsync(...)` with `jwt.GenerateIdToken(...)`.

### `IJwtTokenService.ValidateAccessTokenAsync`

| Was (1.9) | Now (1.10+) |
|-----------|-------------|
| `Task<bool> ValidateAccessTokenAsync(string accessToken)` — parses JWT with `ReadJsonWebToken` (no crypto) then checks DB `jti` | `Task<bool> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)` — `ValidateTokenAsync` (signature, issuer, audience, lifetime; JWE decrypt when enabled), then DB `jti` |

Forged tokens that only copy a real `jti` into an unsigned/wrong-key JWT no longer pass. Custom `IJwtTokenService` implementations must match the signature (including required `CancellationToken`) and must not trust raw/unvalidated claims before the DB lookup.

**Action:** pass `CancellationToken` at every call site; custom implementations must perform crypto validation (e.g. `ValidateTokenAsync`) before using `jti`.

### `CancellationToken` is required (no `= default`)

On `IJwtTokenService`, `CancellationToken` is required on async methods (including generate/validate/revoke/cleanup helpers). Some methods that previously had no CT parameter now require one; optional `= default` was removed everywhere on this interface. Call sites must pass a token explicitly (e.g. `CancellationToken.None` or `HttpContext.RequestAborted`).

**Action:** update callers and custom `IJwtTokenService` implementations accordingly.

---

## From 1.10.x to 1.11.0

### `main.ChangePassword` input: `Email` → `UserId`

| Area | Was (1.10) | Now (1.11+) |
|------|------------|-------------|
| Form fields | `Email`, `CurrentPassword`, `NewPassword` | `UserId` (Guid string), `CurrentPassword`, `NewPassword` |
| `passwordAuth.selectorField` | `Email` | `Id` |
| `resetPassword.resolveBy.field` | `Email` | `Id` |

`ValidatePasswordAsync` / `SetPasswordAsync` / `GetUserByAsync` accept selector `"Id"`.
`GetUserIdByAsync` does **not** — when the selector is already the id, `PasswordAuthStep` writes it to the bag without a lookup.

**Action:** pass `{ UserId, CurrentPassword, NewPassword }` into `FlowOperationEnum.ChangePassword`; update custom flow overrides.
