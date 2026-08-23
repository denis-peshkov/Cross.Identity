# Breaking changes (NuGet consumers)

Breaking changes for **Cross.Identity**, grouped by **from → to** package version. Apply every section in order when skipping releases (e.g. `1.4 → 1.10` = all sections below).

| Upgrade path         | Section                                     |
|----------------------|---------------------------------------------|
| `≤ 1.4.x` → `1.5.0+` | [From ≤1.4.x to 1.5.0](#from-14x-to-150)    |
| `1.5.x` → `1.6.0+`   | [From 1.5.x to 1.6.0](#from-15x-to-160)     |
| `1.6.x` → `1.7.0+`   | [From 1.6.x to 1.7.0](#from-16x-to-170)     |
| `1.9.x` → `1.10.0+`  | [From 1.9.x to 1.10.0](#from-19x-to-1100)   |
| `1.10.x` → `2.0.0+`  | [From 1.10.x to 2.0.0](#from-110x-to-200)   |

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

## From 1.10.x to 2.0.0

Stock flows: [`FLOWS.md`](../Cross.Identity/FLOWS.md).

### No `IHttpContextAccessor` / ambient `HttpContext` → `ClientContext`

Cross.Identity no longer reads `HttpContext` for the authenticated user, client IP, or User-Agent.
Hosts must pass explicit flow inputs. Host ASP.NET still registers `IHttpContextAccessor` for its own handlers/cookies.

Public JWT and related APIs take a single non-nullable [`ClientContext`](../Cross.Identity/ProcessEngine/Core/ClientContext.cs)
(`IpAddress`, `UserAgent`, `DeviceFingerprint`). Use `ClientContext.Empty` when unknown.
Flow steps read metadata via `ClientContext.Read(bag)` from `collectForm.*`
(no per-step `ipAddressKey` / `userAgentKey` / `deviceFingerprintKey`).
The host must supply trusted values via the **trusted pipeline** (see below); the library does not validate them.

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `JwtTokenService` ctor | `(IdentityContext, IOptionsSnapshot, IHttpContextAccessor)` | `(IdentityContext, IOptionsSnapshot)` |
| JWT issue / refresh invalidate / logout / logout-all / family / user revoke | IP/UA from `HttpContext` | `ClientContext clientContext` |
| `IUserService.SetPasswordAsync` | `(selector, value, password, ct)` | `(selector, value, password, ClientContext clientContext, ct)` |
| `IExternalLoginService.UnlinkAsync` | `(provider, ct)` — principal from `HttpContext` | `(provider, Guid userId, string refreshToken, ClientContext clientContext, ct)` |
| `IExternalLoginService.GetAllAsync` | `(ct)` — principal from `HttpContext` | `(Guid userId, string refreshToken, ct)` |
| `ICommunicationEndpointService.GetAllAsync` | `(Guid userId, ct)` | `(Guid userId, string refreshToken, ct)` |
| `ICommunicationEndpointService.SetPreferredAsync` | `(Guid userId, Guid endpointId, ClientContext, ct)` | `(Guid userId, Guid endpointId, string refreshToken, ClientContext, ct)` |
| User-scoped flows (`ExternalLogin` link, `ExternalLoginUnlink`, `ExternalLoginGetAll`, `CommunicationEndpoints*`) | bag `UserId` trusted without session proof | bag `UserId` + **`RefreshToken`**; `IJwtTokenService.EnsureRefreshTokenBelongsToUserAsync` |
| OAuth sign-in auto-link by email | any matching `UsersAccounts.Email` | only when provider email is verified (`EmailVerified`); links to **confirmed** account only |
| `UsersAccounts.Email` uniqueness | unique on `Email` (all rows) | unique only when `EmailConfirmed = 1` (filtered index); multiple unconfirmed rows allowed |
| `UsersAccounts.PhoneNumber` uniqueness | unique on `PhoneNumber` (all rows) | unique only when `PhoneNumberConfirmed = 1` (filtered index); multiple unconfirmed rows allowed |
| `AddExternalLogin` DI | `TryAddSingleton<IHttpContextAccessor>` | Removed — host registers accessor if needed |

**Flow bag keys (optional unless noted):** `IpAddress`, `UserAgent`, and `DeviceFingerprint` on **all** main flows (`collectForm`);
**required** `UserId` + **`RefreshToken`** on `ExternalLoginUnlink` / `ExternalLoginGetAll` / `CommunicationEndpointsGetAll` / `CommunicationEndpointSetPreferred`;
**optional** `UserId` on `ExternalLogin` (account link; formerly `LinkUserId`); when `UserId` is set, **`RefreshToken` is required** and must belong to that user.

**Action:** fill bags from the host handler; pass `new ClientContext(ip, ua, deviceFingerprint)` or `ClientContext.Empty` into JWT / password / unlink APIs; rename `LinkUserId` → `UserId` in bags, flow JSON (`userIdKey`), and `auth.ExternalLoginStates`.

**Trusted pipeline (host responsibility, not a library bug):** `collectForm.IpAddress`, `UserAgent`, and `DeviceFingerprint` are **host-supplied**. Cross.Identity does not read `HttpContext` and does not verify metadata. The **host** must implement a trusted pipeline: overwrite these bag keys from server-side sources (`RemoteIpAddress` after `ForwardedHeaders`, request `User-Agent`, host-computed fingerprint) before `ExecuteAsync`, and pass the same values into direct JWT/password/unlink APIs. The library records them in audit and revoke paths as trusted. Do not copy values from the client request body. Details: [`FLOWS.md`](../Cross.Identity/FLOWS.md) — Client context (host).

### `RevokeReason` → `RevokedReason`

| Area | Was | Now |
|------|-----|-----|
| Enum | `RefreshTokenRevokeReason` | `RefreshTokenRevokedReason` |
| Audit (`AuditEntity` / `auth.Audits`) | `RevokeReason` | `RevokedReason` |

`AccessTokens` and `RefreshTokens` store only **`RevokedAt`** on the token row. Revoke **reason** and client metadata are append-only in **`auth.Audits`** (`RevokedReason`, `IpAddress`, `UserAgent`, `DeviceFingerprint`).

**Action:** rename enum/usages; ensure `auth.Audits.RevokedReason` exists on existing databases (greenfield scripts already use `RevokedReason`).

### Session binding: `Created*` on `RefreshTokens` only

| Area | Now |
|------|-----|
| `RefreshTokenEntity` / `auth.RefreshTokens` | `CreatedIpAddress`, `CreatedUserAgent`, `CreatedDeviceFingerprint` — family anchor for session binding on refresh |
| `AccessTokenEntity` / `auth.AccessTokens` | **No** `Created*` columns |

Flow bag keys remain `IpAddress` / `UserAgent` / `DeviceFingerprint` (host → `ClientContext`).

**Access token** issue metadata is **not** denormalized on the token row. It is written to **`auth.Audits`** via `AuditService.RecordTokenIssued` (`IpAddress`, `UserAgent`, `DeviceFingerprint`, `EntityId` = access-token jti).

**Refresh token** issue: same audit row **plus** non-empty `ClientContext` values are copied to `Created*` on the refresh row (family anchor inherited on rotation).

**Action:** run `1_04_auth_RefreshTokens_SessionBinding.sql` on existing databases; greenfield `2_01_auth_RefreshTokens.sql` already includes `Created*`.

### Refresh idle timeout: `LastActivityAt` + `RefreshTokenIdleTimeout`

| Area | Now |
|------|-----|
| `RefreshTokenEntity` / `auth.RefreshTokens` | `LastActivityAt` — updated to `UtcNow` on each login/rotation |
| `Authentication:Jwt:RefreshTokenIdleTimeout` | Max idle time since `LastActivityAt`; `Zero` disables the check |

On refresh, when idle is exceeded, `EnsureRefreshTokenActiveForRotationAsync` revokes the family with `SESSION_EXPIRED`. `ValidateRefreshTokenAsync` returns `false` for idle-expired tokens when the option is enabled.

**Action:** run `1_05_auth_RefreshTokens_LastActivityAt.sql` on existing databases (backfill `LastActivityAt = CreatedAt`); set `RefreshTokenIdleTimeout` in host configuration when required.

### Revoke audit metadata (`auth.Audits`, not token columns)

| Area | Was | Now |
|------|-----|-----|
| Token row (`AccessTokens` / `RefreshTokens`) | sometimes `RevokedByIp` in custom schemas | `RevokedAt` only |
| Audit row on revoke | `RevokedByIp` | `Audits.IpAddress`, `Audits.UserAgent`, `Audits.DeviceFingerprint`, `Audits.RevokedReason` |

Revoke paths pass metadata via `ClientContext` → `RecordTokenRevoked`. There are **no** `RevokedIpAddress` / `RevokedUserAgent` columns on token entities.

**Action:** pass IP, User-Agent, and device fingerprint through `ClientContext` on revoke paths; query `auth.Audits` for forensic detail.

### No `IHeadersContextAccessor` in Cross.Identity

`UserService` no longer reads ambient `LanguageCode` from `IHeadersContextAccessor`.
Cross.Identity no longer depends on `Cross.Headers`.

**Action:** drop Cross.Identity's `Cross.Headers` dependency if used only for Identity.

### Phone numbers: E.164 only

Phone number inputs must be E.164, e.g. `+79161234567`. **`collectForm`** (`type: PhoneNumber`) is the library gate (`PhoneE164`); `UserService` and other steps trust the normalized bag value.

- Accepted: `+` + digits only, no spaces/punctuation; number must be a valid E.164 subscriber number.
- Rejected: national formats (`8916…`), missing `+`, spaces, dashes, parentheses (`+7 (912) …`), and any value that is not already E.164.

The library validates and stores as-is; it does **not** reformat free-form numbers on the way in.

Use the public static helper [`PhoneE164`](../Cross.Identity/Helpers/PhoneE164.cs) (`Cross.Identity.Services.Crypto`) from host / external APIs:

- `IsValid` / `Require` — library entry (already E.164)
- `Normalize` / `NormalizeOrThrow` / `Ensure` — host-side conversion before `ExecuteAsync`

`IPhoneNormalizer` is removed — use `PhoneE164` instead (no DI).

Bag / form / entity / DB use `PhoneNumber` (`UsersAccounts`, `PhoneVerifications`). Bag / form / `Selector` use `PhoneNumber`. Selector alias `phonenumber` still accepted by `UserService`.

**Action:** rely on `collectForm` for flow phone numbers, or normalize in the host with `PhoneE164` when bypassing forms; ensure DB column is `PhoneNumber` on existing databases.

### OTP `channel`: string → `ChannelEnum` (`phone` → `sms`)

`VerifyCodeStep`, `SendCodeStep`, `ResetPasswordStep`, `TokenStep` (where applicable) and `ICodeService.VerifyAsync` take `ChannelEnum` (not `string`).
Flow JSON / custom overrides must use enum names: `email`, `sms` (not `phone`).

`Selector.Bind` + `Selector.ChannelForField`: phone number → `ChannelEnum.Sms`, email → `ChannelEnum.Email`, user name → no channel.

Also: `ChannelEnum.WatsApp` renamed to **`WhatsApp`**.

**Action:** replace `"channel": "phone"` with `"channel": "sms"`; update `WatsApp` / `watsApp` to `WhatsApp`; update callers of `VerifyAsync`.

### `Selector` replaces `resolveBy` / `selectorKey` / `phoneNumberKey` / `userNameKey`

Identity is bound once on `collectForm` via `selector.candidates` (first non-empty wins → `collectForm.Field` / `collectForm.Value`).
Later steps call `Selector.Resolve` — no per-step `resolveBy` / `selectorKey`.

| Area | Was | Now |
|------|-----|-----|
| Flow JSON | `resolveBy`, `selectorKey`, `phoneNumberKey`, `userNameKey` on steps | `collectForm.selector.candidates` only |
| Steps | per-step identity keys | `new Selector()` + bag Field/Value |

**Identity on stock flows:** `Email` / `PhoneNumber` / `UserName` on `Token` / `RequestCode` / `ForgotPassword` / `ResetPassword` / `GetUserId` (and optional on `Register`) via `selector.candidates`.

**Action:** remove obsolete keys from custom flow overrides; ensure `collectForm` declares `selector.candidates` where identity is needed.

### `codeAuth` removed → `verifyCode`

| Area | Was | Now |
|------|-----|-----|
| Step kind | `codeAuth` | **`verifyCode`** |
| Behavior | verify OTP + write UserId | same on `verifyCode` (`userIdKey`, default `UserId`) |
| Bag | `codeAuth.*` | `verifyCode.*` |

**Action:** replace `"kind": "codeAuth"` with `"kind": "verifyCode"`; remap bag keys.

### `forgotPassword` step removed → `sendCode`

| Area | Was | Now |
|------|-----|-----|
| Step kind | `forgotPassword` | **`sendCode`** |
| Stock flow | `main.ForgotPassword.json` → `forgotPassword` | → `sendCode` with `template: reset`, `subject: Reset your password` |
| Bag | `forgotPassword.LastCode` | `sendCode.LastCode` |

Hardcoded `http://localhost:4000` is gone: **`Authentication:ClientUrl`** is required for action links in `SendCodeStep`.

**Action:** replace `"kind": "forgotPassword"` with `"kind": "sendCode"` (+ required `template` / `subject`); map `LastCode` keys; set `Authentication:ClientUrl`.

### `sendCode`: `template` and `subject` are required

| Area | Was | Now |
|------|-----|-----|
| JSON | optional / implied `verify` | **required** `template` + `subject` (`cfg.Str`) |
| Register / RequestCode | often omitted | must set `template: verify`, `subject: Verification Code` |
| ForgotPassword | separate step / `reset` templates | `template: reset`, `subject: Reset your password` |

`template: reset` also appends `email` / `phone` query params (email / phone number) to the action URL; other templates keep code-only URLs.

**Action:** add `template` / `subject` to every custom `sendCode` step.

### `main.ChangePassword` input: `Email` → `Id`

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| Form fields | `Email`, `CurrentPassword`, `NewPassword` | `Id` (Guid string), `CurrentPassword`, `NewPassword` (+ optional client context on `collectForm`) |
| Identity | `resolveBy` / `selectorKey` on steps | `collectForm.selector.candidates: ["Id"]` + `Selector.Resolve` |

`ValidatePasswordAsync` / `SetPasswordAsync` / `GetUserByAsync` accept selector `"Id"`.
`GetUserIdByAsync` does **not** — when the selector is already the id, `PasswordAuthStep` writes it to the bag without a lookup.

**Action:** pass `{ Id, CurrentPassword, NewPassword }` into `FlowOperationEnum.ChangePassword`; update custom flow overrides.

### Communication endpoints flows

New operations (stock `main` flows):

- `CommunicationEndpointsGetAll` — list endpoints for `UserId`
- `CommunicationEndpointSetPreferred` — set preferred endpoint (`UserId` + `EndpointId`)

Host must pass `UserId` in the bag (no ambient auth user).

**Action:** wire routes / clients if you expose these operations; apply matching DDL for communication-endpoint tables (`Infrastructure/Scripts`).

### `main.Token`: invalid credentials → exception (not `is_invalid_code`)

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `TokenStep` on bad password/code | `StepResult.Ok`, bag `token.IsInvalidCode = true`, flow continues to `collectResult` | `StepResult.Fail(NotAuthorizedException)` — flow aborts |
| `collectResult` fields | included `is_invalid_code` | removed; success response is tokens only |

**Action:** map `NotAuthorizedException` to 401 on the host (same as `ChangePassword` / `ResetPassword` verify); stop checking `is_invalid_code` in Token flow clients.

### Password lockout (`Authentication:Lockout`)

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `LockoutEnd` / `AccessFailedCount` / `LockoutEnabled` on `UsersAccounts` | columns only | enforced in `ValidatePasswordAsync` |
| Failed password | always `false`, no counter | increments `AccessFailedCount`; at threshold sets `LockoutEnd` |
| Locked account | ignored | password validation returns `false` until `LockoutEnd` elapses |
| Successful login / `SetPasswordAsync` | no reset | clears counter and `LockoutEnd` |

**Configuration (defaults):** `Lockout:LockoutEnabled` = `true`, `MaxFailedAccessAttempts` = `5`, `LockoutTimeout` = `00:15:00`. Set `MaxFailedAccessAttempts` to `0` to disable counting.

**Action:** configure `Authentication:Lockout` if defaults do not fit; ensure host still applies rate limits (lockout is per-account, not per-IP).

### `sendCode`: unknown identity → `Invalid credentials.` (not `NotFound`)

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `SendCodeStep` when user missing | `NotFoundException` (`User not found.` / `User with given … not found`) | `NotAuthorizedException` (`Invalid credentials.`) — no OTP sent |
| Operational detail | exposed to client | `LogInformation` in `SendCodeStep` (field, identity, underlying reason) |

**Action:** map to 401 like other auth failures; do not rely on 404 for «user does not exist» on ForgotPassword / RequestCode.

### `UsersAccounts.CreatedBy` removed

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `UserAccountEntity.CreatedBy` | `Guid` column (unused; register left default / OAuth wrote `Guid.Empty`) | **removed** |
| DDL | `CreatedBy` on `auth.UsersAccounts` | column dropped |

Self-register and OAuth create accounts without an actor id; the column was never read by the library.

**Action:** run `1_06_auth_UsersAccounts_DropCreatedBy.sql` on existing databases; greenfield `2_01_auth_UsersAccounts.sql` no longer creates `CreatedBy`. Drop any host mappings / queries that reference the column.
