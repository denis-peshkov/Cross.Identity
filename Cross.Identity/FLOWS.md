## Cross.Identity ProcessEngine flow reference

This document matches JSON in `Cross.Identity/ProcessEngine/Definitions/Flows/`.

### How flows work

- Files are named `{flow}.{operation}.json` (e.g. `main.Token.json`).
- Definition key: `{flow}.{operation}` in lowercase (`main.token`).
- Code invocation: `IFlowExecutor.ExecuteAsync(input, flow, FlowOperationEnum.Operation, ct)`.
- Steps run in a `next` chain; `start` points to the first step.
- Within one flow, each step `kind` must be **unique** (two `collectForm` steps in one JSON will not load).
- Form data is stored in `Bag` with the prefix `collectForm.{field}` (see `CollectFormStep`).
- Relative keys (`Email`, `selectorKey`) are qualified as `{kind}.{key}`; absolute keys include a dot (`collectForm.Email`).
- **Client context (all flows):** optional `IpAddress` (max 64), `UserAgent` (max 512), `DeviceFingerprint` (max 128) on `collectForm`. Later steps read via `ClientContext.Read(bag)` (`collectForm.IpAddress` / `UserAgent` / `DeviceFingerprint`) for token audit and revoke/password/unlink paths.
- **Identity (`Email` / `PhoneNumber` / `UserName`):** optional on `Register` (`PhoneNumber`, `UserName` via `createUser.map`). On `Token` / `RequestCode` / `ResetPassword` / `GetUserId` — at least one of `Email|PhoneNumber|UserName`; on `ForgotPassword` — `Email|PhoneNumber` only. Preference Email → PhoneNumber → UserName (`phoneNumberKey` / `userNameKey`). OTP send/verify needs Email or PhoneNumber (not UserName alone).

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

### All flow files (15)

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

---

## `main.ForgotPassword.json`

**Purpose:** start password recovery (send code).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` (either); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: reset`, `subject: Reset your password`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.GetUserId.json`

**Purpose:** get `user_id` by email.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `getUserId` |
| `getUserId` | getUserId | `selectorField: Email`, `selectorKey: collectForm.Email`. → `collectResult` |
| `collectResult` | collectResult | `user_id = getUserId.UserId`. `next: null` |

---

## `main.RefreshToken.json`

**Purpose:** refresh token pair using `refresh_token`.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `refreshToken` |
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
| `collectForm` | collectForm | `Email` (required), optional `PhoneNumber` (E.164), `Password` (8–128); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`; `userIdKey: UserId`, `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: verify`, `subject: Verification Code`. → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserId`. `next: null` |

---

## `main.RequestCode.json`

**Purpose:** send an email code with configurable TTL.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Ttl` (TimeSpan); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `template: verify`, `subject: Verification Code`, `ttlKey: collectForm.Ttl`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.ChangePassword.json`

**Purpose:** change password by user id after validating the current password.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (Guid string, 36), `CurrentPassword` (8–128), `NewPassword` (8–128); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `passwordAuth` |
| `passwordAuth` | passwordAuth | `selectorField: Id`, `selectorKey: collectForm.UserId`, `passwordKey: collectForm.CurrentPassword`. → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `selectorKey: collectForm.UserId`, `passwordKey: collectForm.NewPassword`, `resolveBy.field: Id`. `next: null` |

> Uses the current password as proof of ownership. Unlike `main.ResetPassword`, this flow does **not** require a recovery code.
> `resetPassword` notifies via `channel` using `selectorKey` as the destination (for `Id` + `email` that is the Guid string, not the account email).

---

## `main.ResetPassword.json`

**Purpose:** change password by email after verifying the recovery code.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Code` (required, 8–128), `Password` (8–128); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: email`, `identityKey: collectForm.Email`, `codeKey: collectForm.Code`. → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `resolveBy.field: Email`. `next: null` |

> Recovery `Code` must be present, valid, and not expired; otherwise the flow rejects before changing the password.

---

## `main.Token.json`

**Purpose:** tokens by email and password **or** code (at least one required).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` / `PhoneNumber` / `UserName` (any), `Password` (opt., 8–32), `Code` (opt., 4–32); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. Validators: `requiredIf`, `atLeastOneRequired`. → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `codeKey`, `channel: email`, `resolveBy` (field, required, caseInsensitive). → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `main.ExternalLogin.json`

**Purpose:** start OAuth (redirect to provider).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (opt., up to 512), `UserId` (opt. Guid string); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `externalLoginInitiate` |
| `externalLoginInitiate` | externalLoginInitiate | `providerKey: collectForm.Provider`, `returnUrlKey: collectForm.ReturnUrl`, `userIdKey: collectForm.UserId`. → `collectResult` |
| `collectResult` | collectResult | `url = externalLoginInitiate.Url`. `next: null` |

> `UserId` enables account linking when present; the host must supply the authenticated user’s id (the library does not read `HttpContext`). Omit for normal sign-in / sign-up.

---

## `main.ExternalLoginCallback.json`

**Purpose:** complete OAuth after provider redirect.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `State` (required); `Code` / `Error` (either via `requiredIf`/`atLeastOneRequired`); `ErrorDescription` (opt.); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `externalLoginComplete` |
| `externalLoginComplete` | externalLoginComplete | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_linking`. `next: null` |

> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`, see release plan §B.

---

## `main.ExternalLoginUnlink.json`

**Purpose:** unlink an external OAuth provider from the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string), `Provider` (2–32); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `externalLoginUnlink` |
| `externalLoginUnlink` | externalLoginUnlink | `providerKey: collectForm.Provider`, `userIdKey: collectForm.UserId`. → `collectResult` |
| `collectResult` | collectResult | `unlinked = externalLoginUnlink.Unlinked`. `next: null` |

> Host supplies `UserId` from the authenticated principal. Removes the matching row from `auth.UsersExternalLogins` and revokes all tokens for that user (`EXTERNAL_LOGIN_REMOVED`).

---

## `main.ExternalLoginGetAll.json`

**Purpose:** list enabled OAuth providers and link status for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `externalLoginGetAll` |
| `externalLoginGetAll` | externalLoginGetAll | `userIdKey: collectForm.UserId`. → `collectResult` |
| `collectResult` | collectResult | `account_email`, `providers`. `next: null` |

> Host supplies `UserId`. A provider is included when it is already linked **or** credentials are configured (`ExternalLoginProviderOptions.IsConfigured`). Disabled-in-options providers are omitted unless linked.

---

## `main.Logout.json`

**Purpose:** revoke the current session (presented refresh token).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `logout` |
| `logout` | logout | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logout.Revoked`. `next: null` |

> Revokes the refresh token with `USER_LOGOUT`. Missing or already-revoked tokens are a no-op (idempotent).

---

## `main.LogoutAll.json`

**Purpose:** revoke all sessions for the user (prove ownership via refresh token).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `logoutAll` |
| `logoutAll` | logoutAll | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logoutAll.Revoked`. `next: null` |

> Proves session ownership via the refresh token, then revokes every active access/refresh token for that user with `USER_LOGOUT_ALL`.

---

## `main.VerifyToken.json`

**Purpose:** check whether an access token is still valid in storage (not revoked / not expired).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `AccessToken` (required, max 2048); optional `IpAddress`, `UserAgent`, `DeviceFingerprint`. → `verifyToken` |
| `verifyToken` | verifyToken | `accessTokenKey: collectForm.AccessToken`. → `collectResult` |
| `collectResult` | collectResult | `valid`, `user_id`, `jti` (user_id/jti only when valid). `next: null` |

> Malformed tokens yield `valid: false` (no error). Does not refresh or revoke.

---

## `kind` reference (registered factories)

| kind | Purpose |
|------|---------|
| `collectForm` | Collect and validate form fields |
| `collectResult` | Map `Bag` fields to API response |
| `createUser` | Create user |
| `sendCode` | Send OTP (email/SMS); required `channel` / `template` / `subject` (`reset` also adds email/phone to the action URL) |
| `verifyCode` | Verify OTP |
| `codeAuth` | Verify OTP + authenticate |
| `passwordAuth` | Verify email + password |
| `resetPassword` | Set new password |
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
