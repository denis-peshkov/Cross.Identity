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
- **Client context (all flows):** optional `IpAddress` (max 64), `UserAgent` (max 512), `DeviceFingerprint` (max 128) on `collectForm`. Later steps read via `ClientContext.Read(bag)` for token audit and revoke/password/unlink paths.
- **Identity (`Email` / `PhoneNumber` / `UserName`):** on `collectForm`, `selector.candidates` picks the first non-empty field into `collectForm.Field` / `collectForm.Value`. Later steps call `Selector.Resolve` (no per-step `selectorKey` / `resolveBy`). OTP send/verify needs Email or PhoneNumber (not UserName alone).

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

> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`, see release plan §B.

---

## `main.ExternalLoginUnlink.json`

**Purpose:** unlink an external OAuth provider from the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string), `Provider` (2–32); optional client context. → `externalLoginUnlink` |
| `externalLoginUnlink` | externalLoginUnlink | `providerKey`, `userIdKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `unlinked = externalLoginUnlink.Unlinked`. `next: null` |

> Host supplies `UserId` from the authenticated principal. Removes the matching row from `auth.UsersExternalLogins` and revokes all tokens for that user (`EXTERNAL_LOGIN_REMOVED`).

---

## `main.ExternalLoginGetAll.json`

**Purpose:** list enabled OAuth providers and link status for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId` (required Guid string); optional client context. → `externalLoginGetAll` |
| `externalLoginGetAll` | externalLoginGetAll | `userIdKey: collectForm.UserId`. → `collectResult` |
| `collectResult` | collectResult | `account_email`, `providers`. `next: null` |

> Host supplies `UserId`. A provider is included when it is already linked **or** credentials are configured (`ExternalLoginProviderOptions.IsConfigured`). Disabled-in-options providers are omitted unless linked.

---

## `main.Logout.json`

**Purpose:** revoke the current session (presented refresh token).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048); optional client context. → `logout` |
| `logout` | logout | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `revoked = logout.Revoked`. `next: null` |

> Revokes the refresh token with `USER_LOGOUT`. Missing or already-revoked tokens are a no-op (idempotent).

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
| `collectForm` | collectForm | `UserId` (required Guid string); optional client context. → `communicationEndpointsGetAll` |
| `communicationEndpointsGetAll` | communicationEndpointsGetAll | `userIdKey: collectForm.UserId`. → `collectResult` |
| `collectResult` | collectResult | `endpoints`. `next: null` |

---

## `main.CommunicationEndpointSetPreferred.json`

**Purpose:** set the preferred communication endpoint for the given user.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserId`, `EndpointId` (Guid strings); optional client context. → `communicationEndpointSetPreferred` |
| `communicationEndpointSetPreferred` | communicationEndpointSetPreferred | `userIdKey`, `endpointIdKey` from `collectForm.*`. → `collectResult` |
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
