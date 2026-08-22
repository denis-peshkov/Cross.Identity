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
- **Client context (all flows):** optional `IpAddress` (max 64), `UserAgent` (max 512), `DeviceFingerprint` (max 128) on `collectForm`. Steps read via `ClientContext.Read(bag)` for token audit, revoke, password change, and unlink paths. **Trusted pipeline is the host’s responsibility** — the library does not read `HttpContext` and treats bag values as trusted (see [Client context (host)](#client-context-host)).
- **Identity (`Email` / `PhoneNumber` / `UserName`):** on `collectForm`, `selector.candidates` picks the first non-empty field into `collectForm.Field` / `collectForm.Value`. Later steps call `Selector.Resolve` (no per-step `selectorKey` / `resolveBy`). OTP send/verify needs Email or PhoneNumber (not UserName alone).

### Client context (host)

Cross.Identity **2.0+** does not use `IHttpContextAccessor` or ambient `HttpContext` inside steps. `IpAddress`, `UserAgent`, and `DeviceFingerprint` are ordinary optional `collectForm` fields; `ClientContext.Read(bag)` only reads what the host put in the bag.

**Trusted pipeline contract**

| Party | Responsibility |
|-------|----------------|
| **Host** | Guarantees a **trusted pipeline**: every flow invocation and direct service call receives `ClientContext` values from server-side metadata, never copied blindly from the client request body. |
| **Cross.Identity (library)** | Consumes `ClientContext` as-is for token audit (`Created*`), revoke audit (`Revoked*`), password/unlink flows, and notification templates. Does **not** re-validate IP/UA/fingerprint and does **not** read `HttpContext`. |

If the host implements the trusted pipeline, audit fields and host-side policy (for example `DEVICE_MISMATCH` / `IP_MISMATCH` on refresh) are meaningful. If the host forwards client-controlled form fields, audit and emails may be misleading — that is a **host integration bug**, not a library defect.

| Field | Set from (trusted) | Do not use |
|-------|-------------------|------------|
| `collectForm.IpAddress` | `HttpContext.Connection.RemoteIpAddress` after `UseForwardedHeaders` on known proxies | Client JSON/body, raw `X-Forwarded-For` without proxy config |
| `collectForm.UserAgent` | `HttpContext.Request.Headers.User-Agent` | Client-supplied form field |
| `collectForm.DeviceFingerprint` | Host-computed value (cookie, SDK, server session) if you use one | Arbitrary client input |

**Recommended pattern** in the API handler, **after** building the flow input dictionary and **before** `IFlowExecutor.ExecuteAsync`:

1. Read IP and User-Agent from `HttpContext` on the server.
2. **Overwrite** `collectForm.IpAddress`, `collectForm.UserAgent`, `collectForm.DeviceFingerprint` in the bag (even if the client sent values in the request body).
3. For direct service calls (`SetPasswordAsync`, `UnlinkAsync`, JWT APIs), pass `new ClientContext(ip, userAgent, deviceFingerprint)` or `ClientContext.Empty` — same trusted sources.

**Library usage notes:**

- Audit columns (`CreatedIpAddress`, `RevokedIpAddress`, …) record whatever the host passed under the trusted-pipeline contract.
- Notification templates (e.g. reset password) may include IP/UA from `ClientContext`.
- Behind a reverse proxy: configure ASP.NET Core `ForwardedHeaders` so `RemoteIpAddress` is correct; do not parse forwarding headers inside the library.
- Reserved revoke reasons such as `DEVICE_MISMATCH` / `IP_MISMATCH` are for **host or product** policy when metadata is trusted; the stock library does not auto-revoke on fingerprint/IP mismatch.

### Operations (`FlowOperationEnum`)

| File (example) | Enum |
|----------------|------|
| `*.Register.json` | `Register` |
| `*.Token.json` | `Token` |
| `*.VerifyToken.json` | `VerifyToken` |
| `*.RefreshToken.json` | `RefreshToken` |
| `*.RequestCode.json` | `RequestCode` |
| `*.ChangePassword.json` | `ChangePassword` |
| `*.ResetPassword.json` | `ResetPassword` |
| `*.ForgotPassword.json` | `ForgotPassword` |
| `*.GetUserId.json` | `GetUserId` |
| `*.ExternalLogin.json` | `ExternalLogin` |
| `*.ExternalLoginCallback.json` | `ExternalLoginCallback` |
| `*.ExternalLoginUnlink.json` | `ExternalLoginUnlink` |
| `*.ExternalLoginGetAll.json` | `ExternalLoginGetAll` |
| `*.Logout.json` | `Logout` |
| `*.LogoutAll.json` | `LogoutAll` |
| `*.CommunicationEndpointsGetAll.json` | `CommunicationEndpointsGetAll` |
| `*.CommunicationEndpointSetPreferred.json` | `CommunicationEndpointSetPreferred` |

### All flow files (17)

| Flow | Operation | File |
|------|-----------|------|
| `main` | Register | `main.Register.json` |
| `main` | Token | `main.Token.json` |
| `main` | VerifyToken | `main.VerifyToken.json` |
| `main` | RefreshToken | `main.RefreshToken.json` |
| `main` | RequestCode | `main.RequestCode.json` |
| `main` | ChangePassword | `main.ChangePassword.json` |
| `main` | ResetPassword | `main.ResetPassword.json` |
| `main` | ForgotPassword | `main.ForgotPassword.json` |
| `main` | GetUserId | `main.GetUserId.json` |
| `main` | ExternalLogin | `main.ExternalLogin.json` |
| `main` | ExternalLoginCallback | `main.ExternalLoginCallback.json` |
| `main` | ExternalLoginUnlink | `main.ExternalLoginUnlink.json` |
| `main` | ExternalLoginGetAll | `main.ExternalLoginGetAll.json` |
| `main` | Logout | `main.Logout.json` |
| `main` | LogoutAll | `main.LogoutAll.json` |
| `main` | CommunicationEndpointsGetAll | `main.CommunicationEndpointsGetAll.json` |
| `main` | CommunicationEndpointSetPreferred | `main.CommunicationEndpointSetPreferred.json` |

---

## `main.ForgotPassword.json`

**Purpose:** start password recovery (send code).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` (either); optional client context. `selector.candidates`: Email, PhoneNumber. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: reset`, `subject: Reset your password`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.GetUserId.json`

**Purpose:** get `user_id` by identity.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `getUserId` |
| `getUserId` | getUserId | resolves via `Selector`; writes `getUserId.UserId`. → `collectResult` |
| `collectResult` | collectResult | `user_id = getUserId.UserId`. `next: null` |

---

## `main.RefreshToken.json`

**Purpose:** refresh token pair using `refresh_token`.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional client context. → `refreshToken` |
| `refreshToken` | refreshToken | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

> **Transaction:** `refreshToken` does not open a DB transaction. The host should wrap the refresh call (same scoped `IdentityContext`) in an external transaction so validation, new-token persistence, and old-token invalidation commit together.
>
> **Concurrency interceptor:** `IdentityContext` registers `ConcurrencyStampInterceptor` in `OnConfiguring` (hosts need not call `AddInterceptors`). It rotates `ConcurrencyStamp` on `SaveChanges` for all `IHasConcurrencyStamp` entities (users, tokens, verifications, OAuth state, etc.).

---

## `main.Register.json`

**Purpose:** registration by email + password with confirmation code delivery.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (required), optional `PhoneNumber`, `UserName`, `Password` (8–128); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`, `PhoneNumber`, `UserName`; `userIdKey: UserId`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: verify`, `subject: Verification Code`. → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserId`. `next: null` |

---

## `main.RequestCode.json`

**Purpose:** send an email code with configurable TTL.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Ttl` (TimeSpan); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: verify`, `subject: Verification Code`, `ttlKey: collectForm.Ttl`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.ChangePassword.json`

**Purpose:** change password by user id after validating the current password.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Id` (Guid string, 36), `CurrentPassword`, `NewPassword`; optional client context. `selector.candidates`: Id. → `passwordAuth` |
| `passwordAuth` | passwordAuth | `passwordKey: collectForm.CurrentPassword`. → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `passwordKey: collectForm.NewPassword`. `next: null` |

> Uses the current password as proof of ownership. Unlike `main.ResetPassword`, this flow does **not** require a recovery code.

---

## `main.ResetPassword.json`

**Purpose:** change password after verifying the recovery code.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Code` (8–128), `Password` (8–128); optional client context. `selector.candidates`: Email, PhoneNumber, UserName. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: email`, `codeKey: collectForm.Code`; writes `verifyCode.UserId`. → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `passwordKey: collectForm.Password`. `next: null` |

> Recovery `Code` must be present, valid, and not expired; otherwise the flow rejects before changing the password.

---

## `main.Token.json`

**Purpose:** tokens by identity and password **or** code (at least one credential required).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Password` (opt., 8–32), `Code` (opt., 4–32); optional client context. Validators: `requiredIf`, `atLeastOneRequired`. `selector.candidates`: Email, PhoneNumber, UserName. → `token` |
| `token` | token | `passwordKey`, `codeKey`, `channel: email`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `main.ExternalLogin.json`

**Purpose:** start OAuth (redirect to provider).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (opt.), `UserId` (opt. Guid string), `RefreshToken` (required when `UserId` is set); optional client context. → `externalLoginInitiate` |
| `externalLoginInitiate` | externalLoginInitiate | `providerKey`, `returnUrlKey`, `userIdKey`, `refreshTokenKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `url = externalLoginInitiate.Url`. `next: null` |

> `UserId` enables account linking when present; the host must supply the authenticated user’s id **and** a valid `RefreshToken` for that user (the library validates the token before starting OAuth). Omit both for normal sign-in / sign-up.

---

## `main.ExternalLoginCallback.json`

**Purpose:** complete OAuth after provider redirect.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `State` (required); `Code` / `Error` (either); `ErrorDescription` (opt.); optional client context. → `externalLoginComplete` |
| `externalLoginComplete` | externalLoginComplete | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_linking`. `next: null` |

> OAuth callback resolves the user by existing external login, or — when emails match a **confirmed** local account — only if the provider attests a verified email (`EmailVerified`). Unconfirmed email rows do not block registration or OAuth: verified OAuth creates a new confirmed account alongside any unconfirmed rows. Without a verified provider email, merge is rejected. For explicit linking to a specific account, use `UserId` + `RefreshToken`.
>
> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`, see release plan §B.

---

## `main.ExternalLoginUnlink.json`

**Purpose:** unlink an external OAuth provider from the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string), `RefreshToken` (required), `Provider` (2–32); optional client context. → `externalLoginUnlink` |
| `externalLoginUnlink` | externalLoginUnlink | `providerKey`, `userIdKey`, `refreshTokenKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `unlinked = externalLoginUnlink.Unlinked`. `next: null` |

> Host supplies `UserId` and a valid `RefreshToken` for that user (session proof). Removes the matching row from `auth.UsersExternalLogins` and revokes all tokens for that user (`EXTERNAL_LOGIN_REMOVED`).

---

## `main.ExternalLoginGetAll.json`

**Purpose:** list enabled OAuth providers and link status for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string), `RefreshToken` (required); optional client context. → `externalLoginGetAll` |
| `externalLoginGetAll` | externalLoginGetAll | `userIdKey`, `refreshTokenKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `account_email`, `providers`. `next: null` |

> Host supplies `UserId` and a valid `RefreshToken` for that user. A provider is included when it is already linked **or** credentials are configured (`ExternalLoginProviderOptions.IsConfigured`). Disabled-in-options providers are omitted unless linked.

---

## `main.Logout.json`

**Purpose:** revoke the current session (presented refresh token).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional client context. → `logout` |
| `logout` | logout | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logout.Revoked`. `next: null` |

> Revokes the refresh token and access tokens in the same session (family) with `USER_LOGOUT`. Missing or already-revoked tokens are a no-op (idempotent).

---

## `main.LogoutAll.json`

**Purpose:** revoke all sessions for the user (prove ownership via refresh token).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional client context. → `logoutAll` |
| `logoutAll` | logoutAll | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logoutAll.Revoked`. `next: null` |

> Proves session ownership via the refresh token, then revokes every active access/refresh token for that user with `USER_LOGOUT_ALL`.

---

## `main.VerifyToken.json`

**Purpose:** check whether an access token is still valid in storage (not revoked / not expired).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `AccessToken` (required, max 2048); optional client context. → `verifyToken` |
| `verifyToken` | verifyToken | `accessTokenKey: collectForm.AccessToken`. → `collectResult` |
| `collectResult` | collectResult | `valid`, `user_id`, `jti` (user_id/jti only when valid). `next: null` |

> Malformed tokens yield `valid: false` (no error). Does not refresh or revoke.

---

## `main.CommunicationEndpointsGetAll.json`

**Purpose:** list communication endpoints for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string), `RefreshToken` (required); optional client context. → `communicationEndpointsGetAll` |
| `communicationEndpointsGetAll` | communicationEndpointsGetAll | `userIdKey`, `refreshTokenKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `endpoints`. `next: null` |

---

## `main.CommunicationEndpointSetPreferred.json`

**Purpose:** set the preferred communication endpoint for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId`, `RefreshToken`, `EndpointId` (required); optional client context. → `communicationEndpointSetPreferred` |
| `communicationEndpointSetPreferred` | communicationEndpointSetPreferred | `userIdKey`, `endpointIdKey`, `refreshTokenKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `preferred`. `next: null` |

---

## `kind` reference (registered factories)

| kind | Purpose |
|------|---------|
| `collectForm` | Collect and validate form fields; optional `selector.candidates` |
| `collectResult` | Map `Bag` fields to API response |
| `createUser` | Create user |
| `sendCode` | Send OTP (email/SMS); required `channel` / `template` / `subject` (`reset` also adds email and phone number to the action URL) |
| `verifyCode` | Verify OTP and write `UserId` to the bag |
| `passwordAuth` | Verify identity + password; writes `UserId` |
| `resetPassword` | Set new password (identity from `Selector`) |
| `getUserId` | Find user, return `UserId` |
| `token` | Issue access/refresh tokens |
| `refreshToken` | Refresh using refresh_token (host must wrap in an external DB transaction) |
| `externalLoginInitiate` | OAuth redirect URL |
| `externalLoginComplete` | OAuth callback, issue tokens |
| `externalLoginUnlink` | Unlink OAuth provider from current user |
| `externalLoginGetAll` | List OAuth providers + link status for current user |
| `logout` | Revoke current refresh token (`USER_LOGOUT`) |
| `logoutAll` | Revoke all tokens for user (`USER_LOGOUT_ALL`) |
| `verifyToken` | Validate access token; return `valid` (+ `user_id` / `jti` when valid) |
| `communicationEndpointsGetAll` | List user communication endpoints |
| `communicationEndpointSetPreferred` | Set preferred communication endpoint |

### Form validators (`schemaDef.validators`)

| kind | Description |
|------|-------------|
| `equal` | Two fields must be equal |
| `notEqual` | Two fields must not be equal |
| `oneOf` | Value from a list |
| `requiredIf` | Conditional required field |
| `exactlyOneRequired` | Exactly one of the fields |
| `atLeastOneRequired` | At least one of the fields |

Schema can be set via `schema` (name in `IFormSchemaProvider`), `schemaDef` (inline), or `schemaPatch` (add/remove/override/rename).
