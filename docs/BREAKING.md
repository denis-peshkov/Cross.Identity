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

OTP exchange is handled by **`Token`** (`main.Token`) with `{ Email|PhoneNumber, Code }` (same payload shape as before).

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

### No `IHttpContextAccessor` / ambient `HttpContext` in the library

Cross.Identity no longer reads `HttpContext` for the authenticated user, client IP, or User-Agent.
Hosts must pass explicit flow inputs (and matching service parameters). Host ASP.NET still registers
`IHttpContextAccessor` for its own handlers/cookies.

| Area | Was (1.10) | Now (1.11+) |
|------|------------|-------------|
| `JwtTokenService` ctor | `(IdentityContext, IOptionsSnapshot, IHttpContextAccessor)` | `(IdentityContext, IOptionsSnapshot)` |
| JWT issue/revoke APIs | IP/UA from `HttpContext` | `string? ipAddress` / `string? userAgent` on generate; `string? ipAddress` on invalidate/logout/family/user revoke helpers |
| `IUserService.SetPasswordAsync` | `(selector, value, password, ct)` | `(selector, value, password, string? ipAddress, ct)` |
| `IExternalLoginService.UnlinkAsync` | `(provider, ct)` — principal from `HttpContext` | `(provider, Guid userId, string? ipAddress, ct)` |
| `IExternalLoginService.GetAllAsync` | `(ct)` — principal from `HttpContext` | `(Guid userId, ct)` |
| `InitiateAsync` linking | bag/DB `LinkUserId`; must match authenticated principal | bag/DB/state `UserId`; host-supplied id is trusted (no principal match) |
| `AddExternalLogin` DI | `TryAddSingleton<IHttpContextAccessor>` | Removed — host registers accessor if needed |

**Flow bag keys (optional unless noted):** `IpAddress`, `UserAgent`, and `DeviceFingerprint` on **all** main flows (`collectForm`);
token / refresh / OAuth-callback wire `deviceFingerprintKey` into JWT create audit (`CreatedDeviceFingerprint`);
logout / logoutAll / password reset/change / unlink use `IpAddress` / `UserAgent` for revoke audit;
**required** `UserId` on `ExternalLoginUnlink` / `ExternalLoginGetAll`;
**optional** `UserId` on `ExternalLogin` (account link; formerly `LinkUserId`).

**Action:** fill bags from the host handler (`HttpContext` stays only in the host); update custom `IJwtTokenService` / OAuth callers; rename `LinkUserId` → `UserId` in bags, flow JSON (`userIdKey`), and `auth.ExternalLoginStates`.

### `RevokeReason` → `RevokedReason`

| Area | Was | Now |
|------|-----|-----|
| Enum | `RefreshTokenRevokeReason` | `RefreshTokenRevokedReason` |
| Entity property | `RevokeReason` | `RevokedReason` |
| DB column (`AccessTokens` / `RefreshTokens`) | `RevokeReason` | `RevokedReason` |

**Action:** rename type/property usages and alter column name on existing databases (DDL scripts under `Infrastructure/Scripts` updated for greenfield).

### Token create audit: `IpAddress` / `UserAgent` / `DeviceFingerprint` → `Created*`

| Area | Was | Now |
|------|-----|-----|
| Entity / DB (`AccessTokens` / `RefreshTokens`) | `IpAddress` | `CreatedIpAddress` |
| | `UserAgent` | `CreatedUserAgent` |
| | `DeviceFingerprint` | `CreatedDeviceFingerprint` |

Flow bag keys remain `IpAddress` / `UserAgent` / `DeviceFingerprint` (host input). Revoke audit fields stay `RevokedIpAddress` / `RevokedUserAgent`.

**Identity:** `Email` / `PhoneNumber` / `UserName` on `Token` / `RequestCode` / `ForgotPassword` / `ResetPassword` / `GetUserId` (`phoneNumberKey` / `userNameKey`; preference Email → PhoneNumber → UserName). Optional `PhoneNumber` / `UserName` on `Register`.

**Action:** rename columns on existing databases.

### `RevokedByIp` → `RevokedIpAddress` + `RevokedUserAgent`

| Area | Was | Now |
|------|-----|-----|
| Entity / DB | `RevokedByIp` | `RevokedIpAddress` |
| Entity / DB | — | `RevokedUserAgent` (new) |
| JWT revoke APIs | `string? ipAddress` | `string? ipAddress, string? userAgent` |
| `SetPasswordAsync` / `UnlinkAsync` | `ipAddress` | `ipAddress, userAgent` |

Logout / logoutAll / password / unlink flows again accept optional `UserAgent` for revoke audit.

**Action:** pass User-Agent from the host on revoke paths; rename/add DB columns.

### No `IHeadersContextAccessor` in Cross.Identity

`UserService` no longer reads ambient `LanguageCode` from `IHeadersContextAccessor`.
Cross.Identity no longer depends on `Cross.Headers`.

### Phone numbers: E.164 only

Phone inputs must be E.164, e.g. `+79161234567`. **`collectForm`** (`type: PhoneNumber`) is the library gate (`PhoneE164`); `UserService` and other steps trust the normalized bag value.

- Accepted: `+` + digits only, no spaces/punctuation; number must be a valid E.164 subscriber number.
- Rejected: national formats (`8916…`), missing `+`, spaces, dashes, parentheses (`+7 (912) …`), and any value that is not already E.164.

The library validates and stores as-is; it does **not** reformat free-form numbers on the way in.

Use the public static helper [`PhoneE164`](../Cross.Identity/Helpers/PhoneE164.cs) (`Cross.Identity.Services.Crypto`) from host / external APIs:

- `IsValid` / `Require` — library entry (already E.164)
- `Normalize` / `NormalizeOrThrow` / `Ensure` — host-side conversion before `ExecuteAsync`

`IPhoneNormalizer` is removed — use `PhoneE164` instead (no DI).

Bag / form / entity / DB use `PhoneNumber` (`UsersAccounts`, `PhoneVerifications`). Bag / form / `resolveBy` use `PhoneNumber`. Selector alias `phonenumber` still accepted by `UserService`.

**Action:** rely on `collectForm` for flow phones, or normalize in the host with `PhoneE164` when bypassing forms; ensure DB column is `PhoneNumber` on existing databases; drop Cross.Identity's `Cross.Headers` dependency if used only for Identity.

### OTP `channel`: string → `ChannelEnum` (`phone` → `sms`)

`VerifyCodeStep` and `ICodeService.VerifyAsync` take `ChannelEnum` (not `string`).
Flow JSON / custom overrides must use enum names: `email`, `sms` (not `phone`).

`Selector.Bind` + `Selector.ChannelForField`: phone → `ChannelEnum.Sms`, email → `ChannelEnum.Email`, user name → no channel.

**Action:** replace `"channel": "phone"` with `"channel": "sms"` in custom `verifyCode` steps; update callers of `VerifyAsync`.

### `codeAuth` removed → use `verifyCode`

`CodeAuthStep` / kind `codeAuth` are removed. `verifyCode` verifies the OTP and writes `UserId` (configurable via `userIdKey`, default `UserId`).

**Action:** replace `"kind": "codeAuth"` with `"kind": "verifyCode"`; map bag keys from `codeAuth.*` to `verifyCode.*`.


### `main.ChangePassword` input: `Email` → `UserId`

| Area | Was (1.10) | Now (1.11+) |
|------|------------|-------------|
| Form fields | `Email`, `CurrentPassword`, `NewPassword` | `UserId` (Guid string), `CurrentPassword`, `NewPassword` (+ optional `IpAddress` / `UserAgent`) |
| `passwordAuth.selectorField` | `Email` | `Id` |
| `resetPassword.resolveBy.field` | `Email` | `Id` |

`ValidatePasswordAsync` / `SetPasswordAsync` / `GetUserByAsync` accept selector `"Id"`.
`GetUserIdByAsync` does **not** — when the selector is already the id, `PasswordAuthStep` writes it to the bag without a lookup.

**Action:** pass `{ UserId, CurrentPassword, NewPassword }` into `FlowOperationEnum.ChangePassword`; update custom flow overrides.
