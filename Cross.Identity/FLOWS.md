## Cross.Identity ProcessEngine flow reference

This document matches JSON in `Cross.Identity/ProcessEngine/Definitions/Flows/`.

### How flows work

- Files are named `{flow}.{operation}.json` (e.g. `main.Token.json`).
- Definition key: `{flow}.{operation}` in lowercase (`main.token`).
- Code invocation: `IFlowExecutor.ExecuteAsync(input, flow, FlowOperationEnum.Operation, ct)`.
- Steps run in a `next` chain; `start` points to the first step.
- Within one flow, each step `kind` must be **unique** (two `collectForm` steps in one JSON will not load).
- Form data is stored in `Bag` with the prefix `collectForm.{field}` (see `CollectFormStep`).
- Relative keys (`Email`, `passwordKey`) are qualified as `{kind}.{key}`; absolute keys include a dot (`collectForm.Email`).
- **Client context (all flows):** optional `LanguageCode` (exactly 2 letters, e.g. `en` / `ru` / `ro` / `ua`), `IpAddress` (max 64), `UserAgent` (max 512), `DeviceFingerprint` (max 128) on `collectForm`. The **host** sets client metadata from server-side sources before `ExecuteAsync`; on refresh the library compares IP/UA/fingerprint with `Created*` on the token family (see [Client context (host)](#client-context-host)). `LanguageCode` selects notification templates (`SendCodeStep` / `ResetPasswordStep`); stock Ukrainian templates use the `ua` file key — ISO `uk` is accepted as an alias for `ua`; missing/invalid code or missing template file → fallback `en`.
- **Identity (`Email` / `PhoneNumber` / `UserName`):** on `collectForm`, `selector.candidates` picks the first non-empty field into `collectForm.Field` / `collectForm.Value`. Later steps call `Selector.Resolve` (no per-step `selectorKey` / `resolveBy`). OTP send/verify by `UserName` uses the user's **preferred verified** communication endpoint (email or phone address); Email/PhoneNumber use the submitted value directly.

### Client context (host)

Cross.Identity **2.0+** does not use `IHttpContextAccessor` or ambient `HttpContext` inside steps. The **host Web API** fills optional `collectForm.LanguageCode`, then `IpAddress`, `UserAgent`, and `DeviceFingerprint` in the bag (or passes `HostSuppliedLanguageContext` / `HostSuppliedClientContext` into direct APIs); `HostSuppliedLanguageContext.Read(bag)` / `HostSuppliedClientContext.Read(bag)` read whatever the host put there.

**Trusted pipeline**

| Party | Responsibility |
|---|---|
| **Host (Web API)** | Before `IFlowExecutor.ExecuteAsync`, set `collectForm.*` from **server-side** sources. Same sources on login and every refresh. For templates, set `LanguageCode` from product locale / negotiated culture (not raw untrusted body unless you accept that). |
| **Cross.Identity** | Consumes `HostSuppliedClientContext` for audit (`Created*`, revoke metadata), notifications (`ResetPasswordStep` via `ISecurityNotifier`), and session binding. Uses `HostSuppliedLanguageContext` + `INotificationComposer` for template language/branding with fallback to `en`. Brand placeholders come from `Authentication:Notifications` (see [Notification templates](#notification-templates-placeholders)). Does not read `HttpContext` or `Accept-Language`. |

### Notification templates (placeholders)

Stock bodies live under `ProcessEngine/Definitions/Templates/` as `{name}.{lang}.{txt|html}` (SMS OTP uses `{name}-sms`). `INotificationComposer` loads by `LanguageCode` (fallback `en`), then substitutes placeholders.

**Brand / static** — `NotificationComposer.ApplyBrand` from `Authentication:Notifications` (`NotificationOptions`; no built-in library defaults) plus UTC year:

| Placeholder | Source |
|---|---|
| `{{brand}}` | `Notifications:Brand` |
| `{{company}}` | `Notifications:Company` |
| `{{site}}` | `Notifications:Site` |
| `{{fullName}}` | `Notifications:FullName` (signature) |
| `{{supportEmail}}` | `Notifications:SupportEmail` |
| `{{year}}` | `DateTime.UtcNow.Year` |

`ApplyBrand` also replaces `{{logoWidth}}` / `{{logoHeight}}` / `{{imageWidth}}` / `{{imageHeight}}` with `"34"`. Stock templates do **not** use those keys (reserved for host HTML).

**Per-step** — passed into `Compose` by the step (after brand):

| Placeholder | Filled by | Stock templates |
|---|---|---|
| `{{code}}` | `sendCode` / `communicationEndpointAdd` | `verify`, `verify-sms`, `reset`, `reset-sms` |
| `{{email}}` | `sendCode` / `communicationEndpointAdd` | `reset`, `registered` (file only; see below); add also fills for `verify` |
| `{{url}}` | `sendCode` / `communicationEndpointAdd` | `reset`, `registered` (file only) |
| `{{verificationLink}}` | `sendCode` / `communicationEndpointAdd` | `verify` |
| `{{helpLink}}` | `sendCode` / `communicationEndpointAdd` | `verify` |
| `{{expires}}` | `sendCode` / `communicationEndpointAdd` | `verify`, `reset` |
| `{{changedAt}}` | `resetPassword` | `password-changed` |
| `{{ip}}` | `resetPassword` | `password-changed` |

`sendCode` also sets `{{logoLink}}` / `{{imageLink}}` to the same action URL; stock templates do not reference them.

**`registered`:** stock files use brand keys plus `{{email}}` / `{{url}}`. No stock flow sends this template yet (Register still uses `verify` OTP). Hosts that compose `registered` themselves must supply `{{email}}` / `{{url}}` (or equivalent) in `Compose` placeholders.

Unresolved `{{…}}` tokens are left as literal text in the body.

### User-scoped authorization (host responsibility)

Flows that take `UserAccountId` but are **not** token credential operations (`CommunicationEndpointsGetAll`, `CommunicationEndpointSetPreferred`, `CommunicationEndpointAdd`, `CommunicationEndpointSetVerified`, `ExternalLogin` link, `ExternalLoginUnlink`, `ExternalLoginGetAll`, **`ExternalLoginMerge`**, **`LogoutAll`**, **`ChangeAccountEmail`**) **trust** the bag `UserAccountId`. The library does **not** require a refresh token as session proof on these paths.

| Party | Responsibility |
|---|---|
| **Host** | Ensure the caller is allowed to act as that `UserAccountId` before `ExecuteAsync` (e.g. `[Authorize]` + claim/`sub` matches bag id, or map id from the access-token principal and overwrite the bag). |
| **Cross.Identity** | Executes the operation for the given `UserAccountId`. Does not re-check session adequacy for these flows. |

Token lifecycle: `Token` still issues refresh tokens. `RefreshToken` takes refresh-token `Jti` (host resolves from the client refresh token). `Logout` takes access-token `Jti`. `LogoutAll` takes `UserAccountId`.

| Field | Set from (trusted) | Do not use |
|---|---|---|
| `collectForm.LanguageCode` | Product locale / negotiated culture (2 letters) for notification templates | Omit → library uses `en`; `uk` → template key `ua`; missing template file for that language → `en` |
| `collectForm.IpAddress` | `HttpContext.Connection.RemoteIpAddress` after `UseForwardedHeaders` on known proxies | Client JSON/body, raw `X-Forwarded-For` without proxy config |
| `collectForm.UserAgent` | `HttpContext.Request.Headers.User-Agent` | Client-supplied form field |
| `collectForm.DeviceFingerprint` | Host-computed value (cookie, validated SDK id, server session) if the product uses binding | Arbitrary unvalidated client input |

**Session binding (refresh):** non-empty `HostSuppliedClientContext` values are stored as `Created*` on the refresh-token family anchor. On rotation the library compares the current context with that anchor. Mismatch → family revoke (`DEVICE_MISMATCH`, `USER_AGENT_MISMATCH`, or `TOKEN_STOLEN` when two or more dimensions differ). IP is checked only when `Authentication:Jwt:SessionBindingCheckIp` is `true` (`IP_MISMATCH`). When **`SessionBindingCheckIp` is `true`** and the anchor captured metadata, refresh must pass the same trusted `IpAddress` / `UserAgent` / `DeviceFingerprint` as on Token — **`HostSuppliedClientContext.Empty` throws `ValidationException`** (no family revoke). `Empty` remains valid on logout, password change, and revoke APIs.

**Recommended handler pattern** before `IFlowExecutor.ExecuteAsync`:

```csharp
using Cross.Identity.ProcessEngine.Core;

var bag = new Dictionary<string, object?> { /* credentials, tokens, … */ };

// Host-derived metadata — not from the client request body
bag["collectForm.LanguageCode"] = "ru"; // optional; notification templates (fallback en)
bag["collectForm.IpAddress"] = httpContext.Connection.RemoteIpAddress?.ToString();
bag["collectForm.UserAgent"] = httpContext.Request.Headers.UserAgent.ToString();
bag["collectForm.DeviceFingerprint"] = deviceFingerprintFromHost; // optional

await flowExecutor.ExecuteAsync(bag, "main", FlowOperationEnum.Token, ct);
```

Behind a reverse proxy: configure ASP.NET Core `ForwardedHeaders` so `RemoteIpAddress` is correct.

### Operations (`FlowOperationEnum`)

| File (example) | Enum |
|---|---|
| `*.Register.json` | `Register` |
| `*.Token.json` | `Token` |
| `*.VerifyToken.json` | `VerifyToken` |
| `*.RefreshToken.json` | `RefreshToken` |
| `*.RequestCode.json` | `RequestCode` |
| `*.ChangePassword.json` | `ChangePassword` |
| `*.ResetPassword.json` | `ResetPassword` |
| `*.ForgotPassword.json` | `ForgotPassword` |
| `*.GetUserAccountId.json` | `GetUserAccountId` |
| `*.ExternalLogin.json` | `ExternalLogin` |
| `*.ExternalLoginCallback.json` | `ExternalLoginCallback` |
| `*.ExternalLoginUnlink.json` | `ExternalLoginUnlink` |
| `*.ExternalLoginGetAll.json` | `ExternalLoginGetAll` |
| `*.ExternalLoginMerge.json` | `ExternalLoginMerge` |
| `*.Logout.json` | `Logout` |
| `*.LogoutAll.json` | `LogoutAll` |
| `*.CommunicationEndpointsGetAll.json` | `CommunicationEndpointsGetAll` |
| `*.CommunicationEndpointSetPreferred.json` | `CommunicationEndpointSetPreferred` |
| `*.CommunicationEndpointAdd.json` | `CommunicationEndpointAdd` |
| `*.CommunicationEndpointSetVerified.json` | `CommunicationEndpointSetVerified` |
| `*.ChangeAccountEmail.json` | `ChangeAccountEmail` |

### All flow files (21)

| Flow | Operation | File |
|---|---|---|
| `main` | Register | `main.Register.json` |
| `main` | Token | `main.Token.json` |
| `main` | VerifyToken | `main.VerifyToken.json` |
| `main` | RefreshToken | `main.RefreshToken.json` |
| `main` | RequestCode | `main.RequestCode.json` |
| `main` | ChangePassword | `main.ChangePassword.json` |
| `main` | ResetPassword | `main.ResetPassword.json` |
| `main` | ForgotPassword | `main.ForgotPassword.json` |
| `main` | GetUserAccountId | `main.GetUserAccountId.json` |
| `main` | ExternalLogin | `main.ExternalLogin.json` |
| `main` | ExternalLoginCallback | `main.ExternalLoginCallback.json` |
| `main` | ExternalLoginUnlink | `main.ExternalLoginUnlink.json` |
| `main` | ExternalLoginGetAll | `main.ExternalLoginGetAll.json` |
| `main` | ExternalLoginMerge | `main.ExternalLoginMerge.json` |
| `main` | Logout | `main.Logout.json` |
| `main` | LogoutAll | `main.LogoutAll.json` |
| `main` | CommunicationEndpointsGetAll | `main.CommunicationEndpointsGetAll.json` |
| `main` | CommunicationEndpointSetPreferred | `main.CommunicationEndpointSetPreferred.json` |
| `main` | CommunicationEndpointAdd | `main.CommunicationEndpointAdd.json` |
| `main` | CommunicationEndpointSetVerified | `main.CommunicationEndpointSetVerified.json` |
| `main` | ChangeAccountEmail | `main.ChangeAccountEmail.json` |

---

## `main.ForgotPassword.json`

**Purpose:** start password recovery (send code).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` (either); optional client context. `selector.candidates`: Email, PhoneNumber. → `sendCode` |
| `sendCode` | sendCode | `template: reset`, `subject: Reset your password` (delivery via `ResolveOtpTargetAsync`). → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.GetUserAccountId.json`

**Purpose:** get `user_account_id` by identity.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `getUserAccountId` |
| `getUserAccountId` | getUserAccountId | resolves via `Selector`; writes `getUserAccountId.UserAccountId`. Unknown → `Invalid credentials.` (logged). → `collectResult` |
| `collectResult` | collectResult | `user_account_id = getUserAccountId.UserAccountId`. `next: null` |

---

## `main.RefreshToken.json`

**Purpose:** refresh token pair using refresh-token `jti` (`RefreshTokens.Id`).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Jti` (refresh-token JTI Guid string); optional client context. → `refreshToken` |
| `refreshToken` | refreshToken | `jtiKey: collectForm.Jti`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_account_id`. `next: null` |

> **Host:** validate the client refresh token, extract `jti` from the JWT (same value as `RefreshTokens.Id`), then pass `{ Jti, … }` into `ExecuteAsync`. The library does not parse the refresh token string on this path.
>
> **Transaction:** `refreshToken` does not open a DB transaction. The host should wrap the refresh call (same scoped `IdentityContext`) in an external transaction so validation, new-token persistence, and old-token invalidation commit together.
>
> **Session binding:** on refresh, `EnsureRefreshTokenActiveForRotationAsync` compares host-supplied `collectForm` metadata with `Created*` on the refresh-token family anchor. Mismatch revokes the family with `DEVICE_MISMATCH`, `USER_AGENT_MISMATCH`, or `TOKEN_STOLEN` (two or more dimensions). IP is checked only when `Authentication:Jwt:SessionBindingCheckIp` is `true` (`IP_MISMATCH`). When **`SessionBindingCheckIp` is `true`**, the host must populate `collectForm.IpAddress` / `UserAgent` / `DeviceFingerprint` from the **trusted pipeline** (same as Token) — `Empty` → `ValidationException`, not family revoke. Default IP check: disabled. See [Client context (host)](#client-context-host).
>
> **Idle timeout:** when `Authentication:Jwt:RefreshTokenIdleTimeout` is greater than zero, refresh rejects tokens whose `LastActivityAt` is older than the configured window and revokes the family with `SESSION_EXPIRED`. Successful rotation sets `LastActivityAt` on the new refresh row. Default: disabled.
>
> **Concurrency stamp:** `IdentityContext` rotates `ConcurrencyStamp` in `SaveChanges` / `SaveChangesAsync` for all `IHasConcurrencyStamp` entities (users, tokens, verifications, OAuth state, etc.). Hosts need not call `AddInterceptors`; pooled DbContext registration is supported.

---

## `main.Register.json`

**Purpose:** registration with optional password and confirmation code delivery. `Email` / `PhoneNumber` are optional fields with **`atLeastOneRequired`** (OTP channel). `UserName` is an optional login alias (not sufficient alone).

When `Authentication:LockChannelAsEmail` is `true`, `createUser` / `CreateUserAsync` requires an email (`ValidationException`: `Email is required when LockChannelAsEmail is enabled.`) — phone-only registration is rejected before `sendCode`.

**Unverified email / phone (squat — intentional):** `CreateUserAsync` raises `ConflictException` (`Email already exists.` / `PhoneNumber already exists.`) only when the same address is already **verified** on another account. Several accounts may hold the same **unverified** email or phone (filtered unique index on verified contacts only). Each Register creates a **new** `UserAccountId` — not idempotent reuse and not an automatic resend for an existing unverified row. Whoever successfully verifies that address first becomes the owner (`EmailVerified` / `PhoneNumberVerified`); later verifies of the same contact fail the uniqueness guard. Stock Register binds OTP send to `createUser.UserAccountId` (not a re-lookup by email), and OTP verify stays scoped by `userId`, so one squat cannot consume another account's code. Hosts that want “same Guid + resend confirmation” must implement that above the library (or change this contract).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` (`atLeastOneRequired`), optional `UserName` alias, optional `Password` (8–32 when set); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`, `PhoneNumber`, `UserName`; `userAccountIdKey: UserAccountId`. With `LockChannelAsEmail` → email required. Unverified contact squat: new id (see above). → `sendCode` |
| `sendCode` | sendCode | `template: verify`, `subject: Verification Code`, `userAccountIdKey: createUser.UserAccountId` (OTP bound to the just-created account; delivery via `ResolveOtpTargetAsync`). → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserAccountId`. `next: null` |

---

## `main.RequestCode.json`

**Purpose:** send an email code with configurable TTL.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Ttl` (TimeSpan, required); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `sendCode` |
| `sendCode` | sendCode | `template: verify`, `subject: Verification Code`, `ttlKey: collectForm.Ttl` (delivery via `ResolveOtpTargetAsync`). → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.ChangePassword.json`

**Purpose:** change password by `UserAccountId` after validating the current password.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (Guid string, 36), `CurrentPassword` (8–32), `NewPassword` (8–32); optional client context. `selector.candidates`: UserAccountId. → `passwordAuth` |
| `passwordAuth` | passwordAuth | `passwordKey: collectForm.CurrentPassword`. → `resetPassword` |
| `resetPassword` | resetPassword | `passwordKey: collectForm.NewPassword`; notify via `ResolveDeliveryTargetAsync` using template `password-changed` (txt/html). `next: null` |

> Uses the current password as proof of ownership. Unlike `main.ResetPassword`, this flow does **not** require a recovery code.

---

## `main.ResetPassword.json`

**Purpose:** change password after verifying the recovery code.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Code` (6–12), `Password` (8–32); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `verifyCode` |
| `verifyCode` | verifyCode | `codeKey: collectForm.Code`; verify against `ResolveOtpTargetAsync`; writes `verifyCode.UserAccountId`. → `resetPassword` |
| `resetPassword` | resetPassword | `passwordKey: collectForm.Password`; notify via `ResolveDeliveryTargetAsync` using template `password-changed` (txt/html). `next: null` |

> Recovery `Code` must be present, valid, and not expired; otherwise the flow rejects before changing the password.

---

## `main.Token.json`

**Purpose:** tokens by identity and password **or** code (at least one credential required).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Password` (opt., 8–32), `Code` (opt., 6–12); optional client context. Validators: `requiredIf`, `atLeastOneRequired`. `selector.candidates`: Email, PhoneNumber, UserName. → `token` |
| `token` | token | `passwordKey`, `codeKey`. Bag output keys are fixed by `TokenPairIssuer` (`AccessToken`, `RefreshToken`, `TokenType`, `ExpiresIn`, `UserAccountId`). → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_account_id`. Invalid credentials → `NotAuthorizedException` (same as `passwordAuth` / `verifyCode`). `next: null` |

---

## `main.ExternalLogin.json`

**Purpose:** start OAuth (redirect to provider).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (opt.), `UserAccountId` (opt. Guid string); optional client context. → `externalLoginInitiate` |
| `externalLoginInitiate` | externalLoginInitiate | `providerKey`, `returnUrlKey`, `userAccountIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `url = externalLoginInitiate.Url`. `next: null` |

> `UserAccountId` enables account linking when present; the **host** must authorize that id (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)). Omit for normal sign-in / sign-up.

---

## `main.ExternalLoginCallback.json`

**Purpose:** complete OAuth after provider redirect.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `State` (required); `Code` / `Error` (either); `ErrorDescription` (opt.); optional `User` (Apple first-auth form_post JSON, max 4096); optional client context. → `externalLoginComplete` |
| `externalLoginComplete` | externalLoginComplete | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey`, `userKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_account_id`, `is_linking`. `next: null` |

> OAuth callback **requires** a non-empty provider email after profile fetch (Apple `id_token`, Graph/userinfo, etc.). Missing email → `ValidationException` (failed login; no local account / link). Any non-empty provider email is treated as verified (`ExternalOAuthProfile.EmailVerified = true`). Then resolves the user by existing external login, or by a matching **verified** local `UsersAccounts.Email`; otherwise creates a new account (`EmailVerified = true`). Unverified same-email rows are not auto-linked. For explicit linking to a specific account, use `UserAccountId` (host-authorized).
>
> **Link conflict (link mode):** when `UserAccountId` (survivor A) is set and the provider user is already linked to another account B, callback does **not** complete. It mints a one-time row in `auth.AccountMergePendings` (TTL — `ExternalLoginOptions.StateLifetime`) under `TransactionScopeOption.Suppress` on a **factory-created** `IdentityContext` (fresh connection via `IDbContextFactory<IdentityContext>` — host must register the factory), so an ambient host transaction cannot roll back the pending row when the method throws, then throws `ConflictException` via `AccountMergeRequired.Create` (sub-code `alreadyLinked`; `Exception.Data` holds `mergeToken`, account ids, `provider`). With `Cross.ErrorHandlers` **≥7.7.0** that surfaces as HTTP **409**. Hosts that need a structured mergeToken ProblemDetails body map `Exception.Data` themselves (or use `AccountMergeRequired.TryGetMergeToken`). Host should surface confirm UI, then call [`main.ExternalLoginMerge`](#mainexternalloginmergejson). Ordinary validation failures remain `ValidationException` → **406**.
>
> **New registration (no existing external login / no email merge):** email is always present (see above). `CreateUserAsync` sets `UsersAccounts.EmailVerified = true`. The synced email communication endpoint is always `IsVerified = true` (`ExternalProvider` — linked OAuth email trusted without OTP). Subsequent logins / link refresh update `ProviderEmail` and the endpoint.
>
> **Apple:** authorize uses `response_mode=form_post`. Profile comes from token-endpoint `id_token` (`sub` / `email` / `email_verified`). Optional form_post `user` (first authorization only) maps to `DisplayName` via `userKey`.
>
> **Apple `FormCallbackUrl` / form_post:** Apple POSTs `code`, `state`, and (first auth) `user` to `Authentication:ExternalLogin:FormCallbackUrl` (required API / back-end `redirect_uri` for authorize + token). Apple does not use `CallbackUrl`. That URL must be a **server** endpoint that accepts POST, then forwards into this flow (`Code` / `State` / `User` / `Error` / `ErrorDescription`). Query providers use GET on `CallbackUrl` (SPA / front-end). A SPA-only route cannot receive Apple’s form_post. Sample: `CallbackUrl` → `http://localhost:4200/auth/external-login-callback` (SPA); `FormCallbackUrl` → `http://localhost:5164/api/v1/Identity/ExternalLogin/OAuthReturn` (`Sample.Api` POST → `main` / `ExternalLoginCallback`).
>
> **Facebook:** Graph email when present is treated as verified; missing email → rejected.
>
> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`.

---

## `main.ExternalLoginMerge.json`

**Purpose:** confirm merge of absorbed account B into survivor A after an OAuth **link** conflict (`mergeToken` from `ExternalLoginCallback`).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (required Guid — survivor A), `MergeToken` (required, 16–64); optional client context. → `externalLoginMerge` |
| `externalLoginMerge` | externalLoginMerge | `mergeTokenKey`, `userAccountIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `survivor_user_account_id`, `absorbed_user_account_id`, `moved_providers`. `next: null` |

> Host supplies an authorized survivor `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)). `MergeAccountsAsync`:
>
> - Validates token (not expired / not consumed) and that it belongs to this survivor.
> - Moves `auth.UsersExternalLogins` from B → A (drops B’s row when A already has the same provider).
> - Moves `auth.UsersCommunicationEndpoints` from B → A (drops duplicate channel+address on A; at most one preferred on A).
> - Sets B `IsActive = false`, rotates security stamps, revokes B tokens (`ACCOUNT_MERGED`), audits `UserAccountsMerged`, consumes the pending row.
> - Does **not** copy email / password / profile from B to A (email/phone identity merge — later).

---

## `main.ExternalLoginUnlink.json`

**Purpose:** unlink an external OAuth provider from the given user.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (required Guid string), `Provider` (2–32); optional client context. → `externalLoginUnlink` |
| `externalLoginUnlink` | externalLoginUnlink | `providerKey`, `userAccountIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `unlinked = externalLoginUnlink.Unlinked`. `next: null` |

> Host supplies an authorized `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)). Removes the matching row from `auth.UsersExternalLogins` and revokes all tokens for that user (`EXTERNAL_LOGIN_REMOVED`).
>
> **Email endpoint on unlink:** when `ProviderEmail` is non-empty and a matching email endpoint exists for that **current** address (`EntityId`+address, else address; never a stale `EntityId` on another address) that is **not** preferred and the user has **more than one** email endpoint, that endpoint is removed. If `UsersAccounts.Email` matched the removed address, it is retargeted to the remaining preferred email (or cleared). Preferred / sole email → `ValidationException` (host should hide Unlink via `CanUnlink`). Empty `ProviderEmail` → login-only unlink. OAuth email sync clears `EntityId` on superseded address rows. Last-login guard unchanged.

---

## `main.ExternalLoginGetAll.json`

**Purpose:** list enabled OAuth providers and link status for the given user.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (required Guid string); optional client context. → `externalLoginGetAll` |
| `externalLoginGetAll` | externalLoginGetAll | `userAccountIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `account_email`, `providers` (each row includes `CanUnlink`). `next: null` |

> `CanUnlink` is `false` when not connected, when unlinking would remove the last login method, or when the provider email endpoint is preferred / the only email endpoint.

> Host supplies an authorized `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)). A provider is included when it is already linked **or** credentials are configured (`ExternalLoginProviderOptions.IsConfigured`). Disabled-in-options providers are omitted unless linked.

---

## `main.Logout.json`

**Purpose:** revoke the current session identified by access-token `jti` (`USER_LOGOUT`). Host resolves `Jti` from the client access token before `ExecuteAsync`.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `Jti` (required Guid string); optional client context. → `logout` |
| `logout` | logout | `jtiKey: collectForm.Jti`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logout.Revoked`. `next: null` |

> Resolves `FamilyId` from the access-token row and revokes the whole family (all active refresh + access). Missing or already-revoked JTI is a no-op (idempotent).

---

## `main.LogoutAll.json`

**Purpose:** revoke all sessions for the user (`USER_LOGOUT_ALL`). Host must authorize the caller and pass `UserAccountId` (e.g. resolve `sub` from the client’s access token before `ExecuteAsync`).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (required Guid string); optional client context. → `logoutAll` |
| `logoutAll` | logoutAll | `userAccountIdKey: collectForm.UserAccountId`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logoutAll.Revoked`. `next: null` |

> Revokes every active access/refresh token for that user with `USER_LOGOUT_ALL`. The library does not read `AccessToken` — the host maps client credentials to `UserAccountId`.

---

## `main.VerifyToken.json`

**Purpose:** check whether an access token is still valid (crypto + storage + `security_stamp` vs `UserAccount.SecurityStamp`).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `AccessToken` (required, max 2048); optional client context. → `verifyToken` |
| `verifyToken` | verifyToken | `accessTokenKey: collectForm.AccessToken`. → `collectResult` |
| `collectResult` | collectResult | `valid`, `user_account_id`, `jti` (user_account_id/jti only when valid). `next: null` |

> Malformed or cryptographically invalid tokens yield `valid: false` (step `Ok`). Tokens whose `security_stamp` claim no longer matches the account (e.g. after password change) also yield `valid: false`. Database or configuration errors fail the step (exception propagated to the host).

---

## `main.CommunicationEndpointsGetAll.json`

**Purpose:** list communication endpoints for the given user.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId` (required Guid string); optional client context. → `communicationEndpointsGetAll` |
| `communicationEndpointsGetAll` | communicationEndpointsGetAll | `userAccountIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `endpoints`. `next: null` |

> Host must authorize `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)).

---

## `main.CommunicationEndpointSetPreferred.json`

**Purpose:** set the preferred communication endpoint for the given user.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId`, `EndpointId` (required Guid strings); optional client context. → `communicationEndpointSetPreferred` |
| `communicationEndpointSetPreferred` | communicationEndpointSetPreferred | `userAccountIdKey`, `endpointIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `preferred`. `next: null` |

> Host must authorize `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)).

---

## `main.ChangeAccountEmail.json`

**Purpose:** change the account primary email for the given user. When the address matches a linked external login `ProviderEmail`, it is auto-verified (no OTP).

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId`, `Email` (required); optional client context. → `changeAccountEmail` |
| `changeAccountEmail` | changeAccountEmail | `userAccountIdKey`, `emailKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `email`, `email_verified`, `endpoint`. `next: null` |

> Host must authorize `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)).
> Does **not** force `IsPreferred` on the upserted email endpoint (account email ≠ delivery preferred). Preferred stays via `CommunicationEndpointSetPreferred` or upsert bootstrap (verified + no existing preferred). Trusted delivery still requires `IsVerified`.

---

## `main.CommunicationEndpointAdd.json`

**Purpose:** add a **secondary** email or phone (two-step with `CommunicationEndpointSetVerified`).

| Layer | Role |
|---|---|
| Service | `ICommunicationEndpointService.AddAsync` — upsert **unverified** `Manual` endpoint only (no OTP) |
| Step | then send OTP **to that address** (not `ResolveOtpTargetAsync` / preferred contact) |

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId`, exactly one of `Email` / `PhoneNumber`, required `Ttl`; optional client context. → `communicationEndpointAdd` |
| `communicationEndpointAdd` | communicationEndpointAdd | `AddAsync` + OTP send (`verify` / `verify-sms`). → `collectResult` |
| `collectResult` | collectResult | `endpoint`, `channel`, `address`, `LastCode` (DeveloperMode only). `next: null` |

> Host must authorize `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)).
> Already-verified address on this user → `ValidationException`. Address verified on another user / account → `ConflictException`.
> Unverified row may be upserted again (resend). Pair with `CommunicationEndpointSetVerified`.

---

## `main.CommunicationEndpointSetVerified.json`

**Purpose:** finish adding a secondary email/phone after OTP (pair with `CommunicationEndpointAdd`).

| Layer | Role |
|---|---|
| Step | verify OTP for the submitted email/phone |
| Service | `ICommunicationEndpointService.SetVerifiedAsync` — mark `Manual` endpoint **verified** (no OTP check inside the service) |

When the address matches (or fills) the account primary email/phone, also sets `UsersAccounts` verification. Preferred only via upsert bootstrap (no existing preferred); otherwise unchanged.

| Step | kind | Details |
|---|---|---|
| `collectForm` | collectForm | `UserAccountId`, exactly one of `Email` / `PhoneNumber`, required `Code`; optional client context. → `communicationEndpointSetVerified` |
| `communicationEndpointSetVerified` | communicationEndpointSetVerified | Verify OTP → `SetVerifiedAsync`. → `collectResult` |
| `collectResult` | collectResult | `endpoint`, `channel`, `address`, `verified`. `next: null` |

> Host must authorize `UserAccountId` (see [User-scoped authorization](#user-scoped-authorization-host-responsibility)).
> Invalid/expired code → `ValidationException`. Preferred stays via `CommunicationEndpointSetPreferred` (or upsert bootstrap when this is the first verified endpoint).

---

## `kind` reference (registered factories)

| kind | Purpose |
|---|---|
| `collectForm` | Collect and validate form fields; optional `selector.candidates` |
| `collectResult` | Map `Bag` fields to API response |
| `createUser` | Create user |
| `sendCode` | Send OTP; required `template` / `subject`. Optional `userAccountIdKey` (e.g. `createUser.UserAccountId` on Register) binds the code to that account; when omitted, identity is resolved by selector. Channel/address from `ResolveOtpTargetAsync` (`LockChannelAsEmail` → preferred verified → account email → account phone; unverified contacts allowed for confirmation). For **SMS**, the step loads `{template}-sms` (short body; already-`-sms` names unchanged); email keeps `{template}`. Action link path uses the **base** template name (`verify` → `/verify?code=…`; `reset` → `/reset-password?code=…`); when selector is Email / PhoneNumber, append `email` / `phone` (identity for deep links; OTP channel is still resolved on verify). Unknown identity / missing `userAccountIdKey` value / no OTP channel → `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information. |
| `verifyCode` | Verify OTP and write `UserAccountId` to the bag. Unknown identity / invalid code / no OTP channel → `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information. |
| `getUserAccountId` | Resolve user id into the bag. Unknown identity → `NotAuthorizedException` (`Invalid credentials.`); real reason logged at Information. |
| `passwordAuth` | Verify identity + password; writes `UserAccountId` |
| `resetPassword` | Set new password (identity from `Selector`); notify with `password-changed` templates |
| `token` | Issue access/refresh tokens |
| `refreshToken` | Refresh using refresh-token `Jti` (`jtiKey`; host validates the client refresh JWT and extracts `jti`; wrap in an external DB transaction) |
| `externalLoginInitiate` | OAuth redirect URL |
| `externalLoginComplete` | OAuth callback, issue tokens |
| `externalLoginUnlink` | Unlink OAuth provider from current user |
| `externalLoginGetAll` | List OAuth providers + link status for current user |
| `logout` | Revoke current session by access-token `Jti` (`USER_LOGOUT`) |
| `logoutAll` | Revoke all tokens for user (`USER_LOGOUT_ALL`) |
| `verifyToken` | Validate access token; return `valid` (+ `user_account_id` / `jti` when valid) |
| `communicationEndpointsGetAll` | List user communication endpoints |
| `communicationEndpointSetPreferred` | Set preferred communication endpoint |
| `communicationEndpointAdd` | `AddAsync` (unverified Manual) + send OTP to that address |
| `communicationEndpointSetVerified` | Verify OTP, then `SetVerifiedAsync` (mark Manual verified) |
| `changeAccountEmail` | Change account primary email via `IUserService.ChangeAccountEmailAsync`; auto-verify when address matches linked provider email |

### Form validators (`schemaDef.validators`)

| kind | Description |
|---|---|
| `equal` | Two fields must be equal |
| `notEqual` | Two fields must not be equal |
| `oneOf` | Value from a list |
| `requiredIf` | Conditional required field |
| `exactlyOneRequired` | Exactly one of the fields |
| `atLeastOneRequired` | At least one of the fields |

Schema can be set via `schema` (name in `IFormSchemaProvider`), `schemaDef` (inline), or `schemaPatch` (add/remove/override/rename).
