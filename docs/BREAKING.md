# Breaking changes (NuGet consumers)

Breaking changes for **Cross.Identity**, grouped by **from → to** package version.
Sections are **newest first** (top) → **oldest last** (bottom). When skipping releases, apply every intervening section **from oldest to newest** (bottom-up through the relevant range), e.g. `1.4 → 1.10` = `≤1.4→1.5`, then `1.5→1.6`, … through `1.9→1.10`.

| Upgrade path         | Section                                     |
|----------------------|---------------------------------------------|
| `2.2.0` → `2.3.0+`   | [From 2.2.0 to 2.3.0](#from-220-to-230)     |
| `2.1.1` → `2.2.0+`   | [From 2.1.1 to 2.2.0](#from-211-to-220)     |
| `2.0.x` → `2.1.1+`   | [From 2.0.x to 2.1.1](#from-20x-to-211)     |
| `1.10.x` → `2.0.0+`  | [From 1.10.x to 2.0.0](#from-110x-to-200)   |
| `1.9.x` → `1.10.0+`  | [From 1.9.x to 1.10.0](#from-19x-to-1100)   |
| `1.8.x` → `1.9.0+`   | no breaking API / flow-contract changes |
| `1.7.x` → `1.8.0+`   | no breaking API / flow-contract changes |
| `1.6.x` → `1.7.0+`   | [From 1.6.x to 1.7.0](#from-16x-to-170)     |
| `1.5.x` → `1.6.0+`   | [From 1.5.x to 1.6.0](#from-15x-to-160)     |
| `≤ 1.4.x` → `1.5.0+` | [From ≤1.4.x to 1.5.0](#from-14x-to-150)    |

There was no `2.1.0` package — next published release after [v2.0.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.0.0) is [v2.1.1](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.1.1).

DB scripts: [`Infrastructure/Scripts/README.md`](../Infrastructure/Scripts/README.md).

Breaking-change details live **only** in this file. [`Cross.Identity/config.nuspec`](../Cross.Identity/config.nuspec) `releaseNotes` should link here and must not duplicate the versioned sections.

When shipping a new breaking change: insert a **From X.Y.Z to A.B.C** section **at the top** of the versioned sections (and a matching TOC row), and prefix the **PR title** with `BREAKING:`.

---

## From 2.2.0 to 2.3.0

Release: [v2.3.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.3.0).

### `main.ChangePassword` input: `Id` → `UserAccountId`

| Area | Was (2.2.0) | Now (2.3.0+) |
|------|-------------|--------------|
| Form field / selector | `Id` (Guid string) | `UserAccountId` (Guid string) |
| Flow input bag | `{ Id, CurrentPassword, NewPassword, … }` | `{ UserAccountId, CurrentPassword, NewPassword, … }` |

`ValidatePasswordAsync` / `SetPasswordAsync` / `GetUserAccountIdByAsync` accept selector `"UserAccountId"` (aliases `"Id"` / `"UserId"`).

**Action:** pass `{ UserAccountId, CurrentPassword, NewPassword }` into `FlowOperationEnum.ChangePassword`; update custom flow overrides.

### `main.Logout`: `RefreshToken` → `Jti`

| Area | Was (2.2.0) | Now (2.3.0+) |
|------|-------------|--------------|
| Flow input bag | `{ RefreshToken, … }` | `{ Jti, … }` (access-token JTI Guid string) |
| Stock `logout` step | `refreshTokenKey` → `RevokeRefreshTokenForLogoutAsync` | `jtiKey` → `RevokeSessionForLogoutAsync` |

**Action:** host extracts `jti` from the client access token, then `ExecuteAsync({ Jti, … })`.

### `main.LogoutAll`: `RefreshToken` → `UserAccountId`

| Area | Was (2.2.0) | Now (2.3.0+) |
|------|-------------|--------------|
| Flow input bag | `{ RefreshToken, … }` | `{ UserAccountId, … }` |
| Session proof | library validated refresh token | **host** authorizes caller and passes `UserAccountId` (e.g. from access-token `sub`) |
| Stock `logoutAll` step | `refreshTokenKey` → `RevokeAllTokensForLogoutAsync` | `userAccountIdKey` → `RevokeAllTokensForUserAsync` (`USER_LOGOUT_ALL`) |
| `IJwtTokenService.RevokeAllTokensForLogoutAsync` | refresh → revoke all | **removed** — use `RevokeAllTokensForUserAsync(userAccountId, USER_LOGOUT_ALL, …)` |

**Action:** host resolves `UserAccountId` before `FlowOperationEnum.LogoutAll`; replace direct `RevokeAllTokensForLogoutAsync` calls with `RevokeAllTokensForUserAsync`.

### `main.RefreshToken`: `RefreshToken` → `Jti`

| Area | Was (2.2.0) | Now (2.3.0+) |
|------|-------------|--------------|
| Flow input bag | `{ RefreshToken, … }` (compact JWT string) | `{ Jti, … }` (refresh-token JTI Guid string — `RefreshTokens.Id`) |
| Stock `refreshToken` step | `refreshTokenKey` → hash lookup + stamp claim | `jtiKey` → row lookup by `RefreshTokens.Id` |

**Action:** host validates the client refresh token (cookie/body), extracts `jti` from the JWT (equals `RefreshTokens.Id`), then `ExecuteAsync({ Jti, … })`.

### `IJwtTokenService`: removed unused APIs

The following members are **removed** (not used by stock flows/steps). Host token validation/revoke by compact string must be implemented in the host or a custom wrapper.

| Removed |
|---------|
| `ValidateRefreshTokenAsync` |
| `EnsureRefreshTokenBelongsToUserAsync` |
| `EnsureRefreshTokenActiveForRotationAsync(string, …)` |
| `GetRefreshTokenAsync(string, …)` |
| `InvalidateRefreshTokenAsync(string, string, …)` |
| `RevokeRefreshTokenForLogoutAsync` |
| `RevokeAccessTokenAsync` |
| `RevokeRefreshTokenFamilyAsync` |
| `CleanupExpiredAccessTokensAsync` |

**Kept:** `GenerateIdToken`, `GenerateAccessTokenAsync`, `GenerateRefreshTokenAsync`, `ValidateAccessTokenAsync`, `ValidateAccessTokenJtiAsync` (`JwtBearer` / host), `EnsureRefreshTokenActiveForRotationAsync(Guid, …)`, `GetRefreshTokenByIdAsync`, `InvalidateRefreshTokenAsync(Guid, Guid, …)`, `GetClaimValue`, `RevokeSessionForLogoutAsync`, `RevokeAllTokensForUserAsync`, `CleanupExpiredRefreshTokensAsync`.

---
## From 2.1.1 to 2.2.0

Release: [v2.2.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.2.0) ([PR #19](https://github.com/denis-peshkov/Cross.Identity/pull/19)).

### User-scoped APIs: no library RefreshToken session proof

User-scoped operations no longer take a refresh token as library session proof. The host must authorize the caller for `UserAccountId` **before** `ExecuteAsync` / direct service calls (e.g. `[Authorize]` + claim/`sub` match, or overwrite bag id from the access-token principal).

| Area | Was (2.1.1) | Now (2.2.0+) |
|------|-------------|--------------|
| `ICommunicationEndpointService.GetAllAsync` | `(userAccountId, refreshToken, …)` | `(userAccountId, …)` — **no** `refreshToken` |
| `ICommunicationEndpointService.SetPreferredAsync` | `(userAccountId, endpointId, refreshToken, hostCtx, …)` | `(userAccountId, endpointId, hostCtx, …)` |
| `IExternalLoginService.InitiateAsync` (link) | `userAccountId` + `refreshToken` session proof | `userAccountId` only; host authorizes |
| `IExternalLoginService.UnlinkAsync` | `(provider, userAccountId, refreshToken, hostCtx, …)` | `(provider, userAccountId, hostCtx, …)` |
| `IExternalLoginService.GetAllAsync` | `(userAccountId, refreshToken, …)` | `(userAccountId, …)` |
| Stock flows `collectForm` | required `RefreshToken` on user-scoped flows | **`UserAccountId` only** (optional client context still allowed) |
| Stock steps | called `EnsureRefreshTokenBelongsToUserAsync` | **do not** call it on these paths |
| `IJwtTokenService.EnsureRefreshTokenBelongsToUserAsync` | used by stock user-scoped steps | **optional** host helper (API unchanged) |

**Affected stock flows:** `main.CommunicationEndpointsGetAll`, `main.CommunicationEndpointSetPreferred`, `main.ExternalLogin` (when linking), `main.ExternalLoginUnlink`, `main.ExternalLoginGetAll`.

**Unchanged:** `Token` / `RefreshToken` / `Logout` / `LogoutAll` still use refresh tokens as operation payload / session lifecycle.

**Action:**
1. Stop sending `RefreshToken` on the flows / direct APIs above; keep sending an authorized `UserAccountId`.
2. Ensure the host Web API authorizes that id before calling Cross.Identity (access token / principal).
3. Optionally call `EnsureRefreshTokenBelongsToUserAsync` yourself if you still want refresh-based proof outside stock steps.
4. Update any custom flow overrides / step factories that still pass `refreshTokenKey` into the removed parameters.

---
## From 2.0.x to 2.1.1

Release: [v2.1.1](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.1.1) ([PR #18](https://github.com/denis-peshkov/Cross.Identity/pull/18)).

### `ConcurrencyStamp` rotation: interceptor → `IdentityContext.SaveChanges`

| Area | Was (2.0.x) | Now (2.1.1+) |
|------|-------------|--------------|
| Rotation | `ConcurrencyStampInterceptor` via `IdentityContext.OnConfiguring` | `IdentityContext.SaveChanges` / `SaveChangesAsync` |
| Public type | `ConcurrencyStampInterceptor` | **removed** |
| Host `AddInterceptors` | optional (auto-attached) | **not required** |
| Pooled DbContext | `OnConfiguring` + `AddInterceptors` breaks `AddDbContextPool` / `AddPooledDbContextFactory` | supported |

**Action:** remove any host `.AddInterceptors(…ConcurrencyStampInterceptor…)` if present; keep registering `IdentityContext` as before (`AddDbContext` or pooled).

**Bulk concurrency contract** (unchanged intent; wording clarified with SaveChanges-based rotation): `ExecuteUpdateAsync` / `ExecuteDeleteAsync` bypass `SaveChanges` and automatic stamp handling. Filter by the **original** `ConcurrencyStamp`, check the **affected-row count** (0 = conflict), and assign a **new** stamp only via `ExecuteUpdateAsync` (`SetProperty`). `ExecuteDeleteAsync` cannot set a stamp — use it only in the WHERE. Prefer tracked `SaveChanges` when possible.

---
## From 1.10.x to 2.0.0

Release: [v2.0.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.0.0) ([PR #16](https://github.com/denis-peshkov/Cross.Identity/pull/16)).

### No `IHttpContextAccessor` / ambient `HttpContext` → `HostSuppliedClientContext`

Cross.Identity no longer reads `HttpContext` for the authenticated user, client IP, or User-Agent.
Hosts must pass explicit flow inputs. Host ASP.NET still registers `IHttpContextAccessor` for its own handlers/cookies.

Public JWT and related APIs take a single non-nullable [`HostSuppliedClientContext`](../Cross.Identity/ProcessEngine/Core/HostSuppliedClientContext.cs)
(`IpAddress`, `UserAgent`, `DeviceFingerprint`). Use `HostSuppliedClientContext.Empty` when unknown.
Flow steps read metadata via `HostSuppliedClientContext.Read(bag)` from `collectForm.*`
(no per-step `ipAddressKey` / `userAgentKey` / `deviceFingerprintKey`).
The host must supply trusted values via the **trusted pipeline** (see below); the library does not validate them.

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `JwtTokenService` ctor | `(IdentityContext, IOptionsSnapshot, IHttpContextAccessor)` | `(IdentityContext, IOptionsSnapshot)` |
| JWT issue / refresh invalidate / logout / logout-all / family / user revoke | IP/UA from `HttpContext` | `HostSuppliedClientContext hostSuppliedClientContext` |
| `IUserService.SetPasswordAsync` | `(selector, value, password, ct)` | `(selector, value, password, HostSuppliedClientContext hostSuppliedClientContext, ct)` |
| `IExternalLoginService.UnlinkAsync` | `(provider, ct)` — principal from `HttpContext` | `(provider, Guid userId, string refreshToken, HostSuppliedClientContext hostSuppliedClientContext, ct)` |
| `IExternalLoginService.GetAllAsync` | `(ct)` — principal from `HttpContext` | `(Guid userId, string refreshToken, ct)` |
| `ICommunicationEndpointService.GetAllAsync` | `(Guid userId, ct)` | `(Guid userId, string refreshToken, ct)` |
| `ICommunicationEndpointService.SetPreferredAsync` | `(Guid userId, Guid endpointId, HostSuppliedClientContext, ct)` | `(Guid userId, Guid endpointId, string refreshToken, HostSuppliedClientContext, ct)` |
| `ICommunicationEndpointService.UpsertAsync` | public on `ICommunicationEndpointService` | **removed** — internal `ICommunicationEndpointUpsertService` (library-only: OAuth sync, post-OTP account sync) |
| `ICommunicationEndpointService.SyncAccountContactsAsync` | public on `ICommunicationEndpointService` | **removed** — internal `ICommunicationEndpointUpsertService` (library-only: `UserService` after OTP verify) |
| User-scoped flows (`ExternalLogin` link, `ExternalLoginUnlink`, `ExternalLoginGetAll`, `CommunicationEndpoints*`) | bag `UserAccountId` trusted without session proof | bag `UserAccountId` + **`RefreshToken`**; `IJwtTokenService.EnsureRefreshTokenBelongsToUserAsync` |
| OAuth sign-in auto-link by email | any matching `UsersAccounts.Email` | only when provider email is verified (`ExternalOAuthProfile.EmailVerified`); links to **verified** account only |
| `UsersAccounts.Email` uniqueness | unique on `Email` (all rows) | unique only when `EmailVerified = 1` (filtered index); multiple unverified rows allowed |
| `UsersAccounts.PhoneNumber` uniqueness | unique on `PhoneNumber` (all rows) | unique only when `PhoneNumberVerified = 1` (filtered index); multiple unverified rows allowed |
| Lookup by Email / PhoneNumber | `FirstOrDefault` (undefined which row) | **prefer** `EmailVerified` / `PhoneNumberVerified` (`GetUserByAsync` / password / code paths) |
| `AddExternalLogin` DI | `TryAddSingleton<IHttpContextAccessor>` | Removed — host registers accessor if needed |

**Flow bag keys (optional unless noted):** `IpAddress`, `UserAgent`, and `DeviceFingerprint` on **all** main flows (`collectForm`);
**required** `UserAccountId` + **`RefreshToken`** on `ExternalLoginUnlink` / `ExternalLoginGetAll` / `CommunicationEndpointsGetAll` / `CommunicationEndpointSetPreferred`;
**optional** `UserId` on `ExternalLogin` (account link; formerly `LinkUserId`); when `UserAccountId` is set, **`RefreshToken` is required** and must belong to that user.

**Action:** fill bags from the host handler; pass `new HostSuppliedClientContext(ip, ua, deviceFingerprint)` or `HostSuppliedClientContext.Empty` into JWT / password / unlink APIs; rename `LinkUserId` → `UserAccountId` in bags, flow JSON (`userAccountIdKey`), and `auth.ExternalLoginStates`.

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

Flow bag keys remain `IpAddress` / `UserAgent` / `DeviceFingerprint` (host → `HostSuppliedClientContext`).

**Access token** issue metadata is **not** denormalized on the token row. It is written to **`auth.Audits`** via `AuditService.RecordTokenIssued` (`IpAddress`, `UserAgent`, `DeviceFingerprint`, `EntityId` = access-token jti).

**Refresh token** issue: same audit row **plus** non-empty `HostSuppliedClientContext` values are copied to `Created*` on the refresh row (family anchor inherited on rotation).

**Action:** run `1_04_auth_RefreshTokens_SessionBinding.sql` on existing databases; greenfield `2_01_auth_RefreshTokens.sql` already includes `Created*`.

### Session binding: IP check opt-in (`SessionBindingCheckIp`)

| Area | Now |
|------|-----|
| `Authentication:Jwt:SessionBindingCheckIp` | When `true`, refresh compares family anchor IP with current request IP; mismatch → `IP_MISMATCH`. Default: `false` (device fingerprint and User-Agent are still checked when captured). When `true` and the family anchor has binding data, refresh must pass the same trusted `HostSuppliedClientContext` as on Token — not `Empty` — or `ValidationException` (no family revoke). |

**Action:** set `SessionBindingCheckIp: true` in host configuration when strict IP binding is required (e.g. fixed-IP clients). On refresh, populate `collectForm.IpAddress` / `UserAgent` / `DeviceFingerprint` from the trusted pipeline (same as login). Leave default for NAT/mobile-friendly refresh.

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

Revoke paths pass metadata via `HostSuppliedClientContext` → `RecordTokenRevoked`. There are **no** `RevokedIpAddress` / `RevokedUserAgent` columns on token entities.

**Action:** pass IP, User-Agent, and device fingerprint through `HostSuppliedClientContext` on revoke paths; query `auth.Audits` for forensic detail.

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

`ChannelEnum` uses **`WhatsApp`** (legacy typo `WatsApp` removed). Messenger values: `Telegram`, `WhatsApp`, `Viber` (stored as `smallint` on endpoints).

**Action:** use `ChannelEnum.WhatsApp` in host C#; stock flow JSON has **no** `channel` (see [Delivery channel](#delivery-channel-preferred--email--lockchannelasemail)); replace legacy `"channel": "phone"` with `"sms"` only in old custom overrides.

### `Selector` replaces `resolveBy` / `selectorKey` / `phoneNumberKey` / `userNameKey`

Identity is bound once on `collectForm` via `selector.candidates` (first non-empty wins → `collectForm.Field` / `collectForm.Value`).
Later steps call `Selector.Resolve` — no per-step `resolveBy` / `selectorKey`.

| Area | Was | Now |
|------|-----|-----|
| Flow JSON | `resolveBy`, `selectorKey`, `phoneNumberKey`, `userNameKey` on steps | `collectForm.selector.candidates` only |
| Steps | per-step identity keys | `new Selector()` + bag Field/Value |

**Identity on stock flows:** `Email` / `PhoneNumber` / `UserName` on `Token` / `RequestCode` / `ForgotPassword` / `ResetPassword` / `GetUserAccountId` (and optional on `Register`) via `selector.candidates`.

**Action:** remove obsolete keys from custom flow overrides; ensure `collectForm` declares `selector.candidates` where identity is needed.

### `codeAuth` removed → `verifyCode`

| Area | Was | Now |
|------|-----|-----|
| Step kind | `codeAuth` | **`verifyCode`** |
| Behavior | verify OTP + write UserId | same on `verifyCode` (`userAccountIdKey`, default `UserId`) |
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

Action URL path and identity query params depend on `template` — see [`SendCodeStep` action URL by `template`](#sendcodestep-action-url-by-template).

**Action:** add `template` / `subject` to every custom `sendCode` step.

### `main.ChangePassword` input: `Email` → `Id`

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| Form fields | `Email`, `CurrentPassword`, `NewPassword` | `Id` (Guid string), `CurrentPassword`, `NewPassword` (+ optional client context on `collectForm`) |
| Identity | `resolveBy` / `selectorKey` on steps | `collectForm.selector.candidates: ["Id"]` + `Selector.Resolve` |

`ValidatePasswordAsync` / `SetPasswordAsync` / `GetUserByAsync` accept selector `"Id"`.
`GetUserAccountIdByAsync` does **not** — when the selector is already the id, `PasswordAuthStep` writes it to the bag without a lookup.

**Action:** pass `{ Id, CurrentPassword, NewPassword }` into `FlowOperationEnum.ChangePassword`; update custom flow overrides.

### Communication endpoints flows

New operations (stock `main` flows):

- `CommunicationEndpointsGetAll` — list endpoints for `UserAccountId` (+ **`RefreshToken`**)
- `CommunicationEndpointSetPreferred` — set preferred endpoint (`UserAccountId` + `EndpointId` + **`RefreshToken`**)

Host must pass `UserAccountId` + session proof in the bag (no ambient auth user).

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
| `LockoutEnd` / `AccessFailedCount` / `LockoutEnabled` on `UsersAccounts` | columns only | enforced in `ValidatePasswordAsync` **and** `ValidateCodeAsync` (Token code-login) |
| Failed password / OTP code | always `false`, no counter | increments `AccessFailedCount`; at threshold sets `LockoutEnd` |
| Locked account | ignored | password **and** code-login validation returns `false` until `LockoutEnd` elapses |
| Successful login / `SetPasswordAsync` | no reset | clears counter and `LockoutEnd` |

**Configuration (defaults):** `Lockout:LockoutEnabled` = `true`, `MaxFailedAccessAttempts` = `5`, `LockoutTimeout` = `00:15:00`. Set `MaxFailedAccessAttempts` to `0` to disable counting.

**Note:** `VerifyCodeStep` (ForgotPassword / ResetPassword recovery) does not apply lockout — recovery remains available while the account is locked for sign-in.

**Action:** configure `Authentication:Lockout` if defaults do not fit; ensure host still applies **IP** rate limits (account lockout and OTP send limits are per-account / per destination, not per-IP).

### OTP send rate limit (`Authentication:OtpSendRateLimit`)

| Area | Was | Now |
|------|-----|-----|
| OTP resend | unlimited | cooldown + optional window cap in `CodeService.SendAsync` |
| Defaults | — | `Cooldown` = `00:01:00`, `MaxSendsPerWindow` = `5`, `Window` = `01:00:00` |
| Disable | — | `Cooldown` = `00:00:00` and `MaxSendsPerWindow` = `0` |
| Exceeded | — | `ValidationException` (`Please wait…` / `Too many verification codes…`) |

**Action:** bind `Authentication:OtpSendRateLimit` in host config; map `ValidationException` from sendCode to 400/429 as appropriate. Per-IP throttling remains a host concern.

### `sendCode` / `verifyCode` / `getUserAccountId`: unknown identity → `Invalid credentials.` (not `NotFound`)

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `SendCodeStep` when user missing | `NotFoundException` (`User not found.` / `User with given … not found`) | `NotAuthorizedException` (`Invalid credentials.`) — no OTP sent |
| `SendCodeStep` when user exists but no OTP channel | `ValidationException` (distinct message) | `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information |
| `VerifyCodeStep` when user missing / bad code / no OTP channel | `KeyNotFoundException` / distinct messages | `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information |
| `GetUserAccountIdStep` when user missing | `KeyNotFoundException` (`User not found.`) | `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information |

**Action:** map to 401 like other auth failures; do not rely on 404 for «user does not exist» on ForgotPassword / RequestCode / ResetPassword / GetUserAccountId.

### Delivery channel: preferred / email / `LockChannelAsEmail`

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| Stock JSON `channel` on `sendCode` / `verifyCode` / `resetPassword` | required enum | **removed** — ignored if present in overrides |
| Channel selection | largely login field + JSON `channel` | `ResolveDeliveryTargetAsync` / `ResolveOtpTargetAsync` |
| Order | field-based | `Authentication:LockChannelAsEmail` → preferred verified → email → **phone** |
| Account contact fallback | any `UsersAccounts.Email` | **OTP**: unverified email **or** phone allowed. **Notify**: only `EmailVerified` / `PhoneNumberVerified` |
| API | `ResolveDeliveryChannelAsync` / `ResolveOtpChannelAsync` | **`ResolveDeliveryTargetAsync` / `ResolveOtpTargetAsync`** → `DeliveryTarget` (channel + address) |
| `VerifyCodeStep` | `Selector.ChannelForField` / `CodeService` by login value | same resolved OTP target as send |

**Action:** bind `Authentication:LockChannelAsEmail` (already under `Authentication` in host config); stop relying on flow `channel`; update callers of the old resolve APIs; ensure users have a preferred verified endpoint or an email for OTP.

### `ICodeService.VerifyAsync` requires `userId`

| Area | Was | Now |
|------|-----|-----|
| Signature | `VerifyAsync(channel, identity, code, ct)` | `VerifyAsync(userId, channel, identity, code, ct)` |
| Lookup | latest active row by email/phone only | same **and** `UserAccountId == userId` |

**Action:** pass the resolved user id (stock `VerifyCodeStep` already does). Prevents accepting another account's OTP when the same unverified email/phone exists on multiple rows.

### Microsoft OAuth: `EmailVerified` requires OIDC attestation

| Area | Was | Now |
|------|-----|-----|
| `ExternalOAuthProfile` flag | `EmailConfirmed` | **`EmailVerified`** (aligns with `UsersAccounts.EmailVerified`) |
| Profile source | Graph `/me` only | Graph `/me` (id, displayName, fallback email) + OIDC `https://graph.microsoft.com/oidc/userinfo` |
| Provider attestation | `true` when Graph `mail` / UPN non-empty | `true` only when userinfo has **non-empty `email`** and `email_verified: true` (Graph fallback email alone never verifies) |

Graph `mail` / `userPrincipalName` alone no longer trigger auto-link to a verified local account. `email_verified` without an OIDC `email` claim also does **not** verify a Graph-derived address (common on Entra work accounts).

**Action:** ensure the Microsoft app registration token has scopes that allow OIDC userinfo (`openid` `email` `profile` `User.Read` — already the library default). Expect new Microsoft users without OIDC `email` + `email_verified` to land as unverified / without email auto-link until the mailbox is attested.

### `EmailConfirmed` / `PhoneNumberConfirmed` → `EmailVerified` / `PhoneNumberVerified`

| Area | Was | Now |
|------|-----|-----|
| `UserAccountEntity` | `EmailConfirmed`, `PhoneNumberConfirmed` | **`EmailVerified`**, **`PhoneNumberVerified`** |
| `ExternalOAuthProfile` | `EmailConfirmed` | **`EmailVerified`** |
| DDL `auth.UsersAccounts` | same old column names | renamed columns; filtered unique indexes use `EmailVerified` / `PhoneNumberVerified` |

**Action:** on existing databases run, in order: `1_07_auth_UsersAccounts_RenameConfirmedToVerified.sql`, then `1_08_auth_UsersAccounts_EmailVerifiedUnique.sql`, then `1_09_auth_UsersAccounts_PhoneNumberVerifiedUnique.sql`. Do **not** edit or re-run `1_02` / `1_03` (PreDeployment scripts are append-only). Update host EF mappings / queries. Greenfield `2_01_auth_UsersAccounts.sql` already uses `*Verified`.

### `UsersCommunicationEndpoints`: one preferred endpoint per user

| Area | Was | Now |
|------|-----|-----|
| DB constraint | app-only (`SetPreferredAsync` clears others) | filtered unique index `UX_auth_UsersCommunicationEndpoints_User_Preferred` on `UserAccountId` where `IsPreferred` |
| EF | no index | `UserCommunicationEndpointEntityConfiguration` matches DDL |

**Action:** run `1_10_auth_UsersCommunicationEndpoints_PreferredUnique.sql` on existing databases (dedupes duplicate preferred rows, then creates index). Greenfield `2_01_auth_UsersCommunicationEndpoints.sql` already includes the index.

### `UsersExternalLogins.UserExternalLoginId`: `bigint` → `uuid` / `UNIQUEIDENTIFIER`

| Area | Was (1.x / early 2.0 deploy) | Now (2.0+) |
|------|------------------------------|------------|
| PK column | `BIGINT IDENTITY` / `bigint` / `AUTO_INCREMENT` | **`uuid` / `UNIQUEIDENTIFIER` / `CHAR(36)`** |
| EF `UserExternalLoginEntity.Id` | `Guid` (always) | unchanged |
| Optional column | `LastUsedAt` | **`UpdatedAt`** (renamed when present) |

Migration assigns a **new random Guid per row** (not deterministic from old id). Related rows are updated when possible:

- `auth.Audits` where `EntityType = UserExternalLogin` and `EntityId` was the old numeric id as text
- `auth.UsersCommunicationEndpoints` with `Source = ExternalProvider`, `Channel = Email`, matching `Address` to `UsersExternalLogins.ProviderEmail`

**Action:** on existing databases with a `bigint` PK, run `1_11_auth_UsersExternalLogins_UserExternalLoginIdToGuid.sql` (after `1_10`). Greenfield `2_01_auth_UsersExternalLogins.sql` already uses Guid PK. Host code must not persist old numeric external-login ids.

### `UsersAccounts.CreatedBy` removed

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| `UserAccountEntity.CreatedBy` | `Guid` column (unused; register left default / OAuth wrote `Guid.Empty`) | **removed** |
| DDL | `CreatedBy` on `auth.UsersAccounts` | column dropped |

Self-register and OAuth create accounts without an actor id; the column was never read by the library.

**Action:** run `1_06_auth_UsersAccounts_DropCreatedBy.sql` on existing databases; greenfield `2_01_auth_UsersAccounts.sql` no longer creates `CreatedBy`. Drop any host mappings / queries that reference the column.

### Access / refresh JWT: `security_stamp` claim

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| Access / refresh JWT claims | no account stamp | `security_stamp` (`ClaimConstants.SecurityStamp`) = current `UsersAccounts.SecurityStamp` |
| `ValidateAccessTokenAsync` | crypto + `jti` row + `IsActive` | same **and** claim must match account stamp when stamp is set |
| `ValidateRefreshTokenAsync` / refresh rotation | hash + idle/session binding + `IsActive` | same **and** stamp claim must match |
| `ValidateAccessTokenJtiAsync` | `ValidateAccessTokenJtiAsync(jti, ct)` — `jti` + `IsActive` only | **`ValidateAccessTokenJtiAsync(jti, securityStamp, ct)`** — same + stamp match; no-stamp overload removed |

Caller-supplied `security_stamp` claims are stripped on issue — the library always embeds the DB value.

**Action:** after password change / OAuth unlink, expect existing access/refresh JWTs to fail validate even if revoke were skipped. In `OnTokenValidated`, pass the `security_stamp` claim into `ValidateAccessTokenJtiAsync(jti, stamp, ct)` (or use `ValidateAccessTokenAsync`). Update custom `IJwtTokenService` implementations — the `(jti, ct)` overload is gone.

### Flow bag keys: `UserId` → `UserAccountId` / `user_id` → `user_account_id`

| Area | Was (2.0 early) | Now (2.0+) |
|------|-----------------|------------|
| Step config JSON | `userIdKey` | `userAccountIdKey` |
| Step property | `UserIdKey` | `UserAccountIdKey` |
| Default bag suffix / step output | `UserId` (e.g. `createUser.UserId`, `token.UserId`) | `UserAccountId` |
| `collectForm` field (OAuth / endpoints flows) | `UserId` | `UserAccountId` |
| `collectResult` PascalCase output | `UserId` | `UserAccountId` |
| `collectResult` snake_case output | `user_id` | `user_account_id` (value path e.g. `token.UserAccountId`) |
| `IdentityConstants` | `UserId` → `"user_id"` | `UserAccountId` → `"user_account_id"` |

**Unchanged (license JWT, separate from identity flows):** claim `"user_id"` and `License.UserId` — do not rename to `user_account_id` / `UserAccountId`.

**Unchanged:** selector alias `"UserId"` on `GetUserAccountIdByAsync` / `GetUserByAsync` (maps to account id like `"UserAccountId"` / `"Id"`).

**Action:** rename bag keys and flow JSON; update host handlers and custom flow overrides; replace `userIdKey` with `userAccountIdKey` and response field `user_id` with `user_account_id` on **identity flow** outputs only.

### Operation / step / service rename: `GetUserId` → `GetUserAccountId`

| Area | Was (2.0 early) | Now (2.0+) |
|------|-----------------|------------|
| `FlowOperationEnum` / route | `GetUserId` | `GetUserAccountId` (`/api/identity/main/GetUserAccountId`) |
| Flow file | `main.GetUserId.json` | `main.GetUserAccountId.json` |
| Step kind (JSON) | `getUserId` | `getUserAccountId` |
| Step / factory types | `GetUserIdStep`, `GetUserIdStepFactory` | `GetUserAccountIdStep`, `GetUserAccountIdStepFactory` |
| `IUserService` | `GetUserIdByAsync` | `GetUserAccountIdByAsync` |

**Action:** update client routes and enum literals; rename custom flow overrides and step configs (`"kind": "getUserAccountId"`); replace `GetUserIdByAsync` in host code and mocks.

### Type rename: `ClientContext` → `HostSuppliedClientContext`

| Area | Was (2.0 early / 1.10 docs) | Now (2.0+) |
|------|-------------------------------|------------|
| Type / file | `ClientContext`, `ClientContext.cs` | `HostSuppliedClientContext`, `HostSuppliedClientContext.cs` |
| Sentinel | `ClientContext.Empty` | `HostSuppliedClientContext.Empty` |
| Flow bag reader | `ClientContext.Read(bag)` | `HostSuppliedClientContext.Read(bag)` |
| JWT / password / unlink API parameter | `ClientContext clientContext` | `HostSuppliedClientContext hostSuppliedClientContext` |

**Action:** rename type, static members, and parameter names in host code; update `cref` / imports. Bag keys (`collectForm.IpAddress`, …) are unchanged.

### Verified contact duplicate → `ConflictException`

| Area | Was | Now |
|------|-----|-----|
| `UserService.CreateUserAsync` (verified email/phone, duplicate username) | `InvalidOperationException` | **`ConflictException`** |
| `UserAccountGuard.EnsureNoOtherVerifiedEmailAsync` / `…PhoneAsync` (OTP confirm) | `InvalidOperationException` | **`ConflictException`** |

**Action:** map duplicate contact/username to HTTP **409** (or your host policy for `ConflictException`); do not treat as `InvalidOperationException` / 500.

### `SendCodeStep` action URL by `template`

| `template` (flow JSON) | Action link (`{{url}}`, `{{verificationLink}}`, …) |
|------------------------|-----------------------------------------------------|
| `verify` (Register, RequestCode, …) | `{Authentication:ClientUrl}/verify?code=…` + `&email=` / `&phone=` when selector is Email / PhoneNumber |
| `reset` (ForgotPassword) | `{Authentication:ClientUrl}/reset-password?code=…` + `&email=` / `&phone=` when selector is Email / PhoneNumber |

Query params carry **identity** for SPA deep links; OTP **channel** is still resolved server-side via `ResolveOtpTargetAsync` on verify (not from the URL).

**Action:** host SPA routes `/verify` and `/reset-password` must read `code` + optional `email` / `phone` from the query string.

### `ICodeService.SendAsync` requires `Guid userAccountId`

| Area | Was (1.10) | Now (2.0+) |
|------|------------|------------|
| Signature | `SendAsync(…, string userId, …)` | **`SendAsync(…, Guid userAccountId, …)`** — same id type as `VerifyAsync` |

`ICodeService` is internal; update only if you replace/mock the OTP service or call it from custom steps.

**Action:** pass `Guid` (stock `SendCodeStep` already does).

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
## From 1.6.x to 1.7.0

### External OAuth step type rename

Flow operations stay `ExternalLogin` / `ExternalLoginCallback`. Step **type** names in JSON and DI changed:

| Was (1.6) | Now (1.7+) |
|-----------|------------|
| `InitiateExternalLogin` | `ExternalLoginInitiate` |
| `CompleteExternalLogin` | `ExternalLoginComplete` |

**Action:** update custom flow overrides that reference the old step types; stock `main.ExternalLogin*.json` already use the new names.

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
| Enum / route | `GetUser` | `GetUserId` (→ `GetUserAccountId` in 2.0; see [2.0 section](#from-110x-to-200)) |
| Flow file | `license.GetUser.json` / `main.GetUser.json` | `main.GetUserAccountId.json` |

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
- `RowVersion` → `ConcurrencyStamp` (`IHasConcurrencyStamp`; through 2.0 rotated via interceptor, from [2.1.1](#from-20x-to-211) via `IdentityContext.SaveChanges`); update host EF mappings and SQL scripts accordingly.
- `RefreshToken.AbsoluteExpiresAt` — add column and backfill.

### Dependencies

- JWT: `Microsoft.IdentityModel.JsonWebTokens`
- Messaging / pepper: `Cross.Messaging`, `Cross.PepperVault` (NuGet)
- Align `Cross.ErrorHandlers` / `Cross.Headers` if the app pins older versions

---
