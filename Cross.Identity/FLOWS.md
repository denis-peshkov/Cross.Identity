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

### Operations (`FlowOperationEnum`)

| File (example) | Enum |
|----------------|------|
| `*.Register.json` | `Register` |
| `*.Token.json` | `Token` |
| `*.TokenByCode.json` | `TokenByCode` |
| `*.RefreshToken.json` | `RefreshToken` |
| `*.RequestCode.json` | `RequestCode` |
| `*.ResetPassword.json` | `ResetPassword` |
| `*.ForgotPassword.json` | `ForgotPassword` |
| `*.GetUserId.json` | `GetUserId` |
| `*.ExternalLogin.json` | `ExternalLogin` |
| `*.ExternalLoginCallback.json` | `ExternalLoginCallback` |

### All flow files (10)

| Flow | Operation | File |
|------|-----------|------|
| `main` | ForgotPassword | `main.ForgotPassword.json` |
| `main` | GetUserId | `main.GetUserId.json` |
| `main` | RefreshToken | `main.RefreshToken.json` |
| `main` | Register | `main.Register.json` |
| `main` | RequestCode | `main.RequestCode.json` |
| `main` | ResetPassword | `main.ResetPassword.json` |
| `main` | Token | `main.Token.json` |
| `main` | TokenByCode | `main.TokenByCode.json` |
| `main` | ExternalLogin | `main.ExternalLogin.json` |
| `main` | ExternalLoginCallback | `main.ExternalLoginCallback.json` |

---

## `main.ForgotPassword.json`

**Purpose:** start password recovery (send code).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128). → `forgotPassword` |
| `forgotPassword` | forgotPassword | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = forgotPassword.LastCode`. `next: null` |

---

## `main.GetUserId.json`

**Purpose:** get `user_id` by email.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`. → `getUserId` |
| `getUserId` | getUserId | `selectorField: Email`, `selectorKey: collectForm.Email`. → `collectResult` |
| `collectResult` | collectResult | `user_id = getUserId.UserId`. `next: null` |

---

## `main.RefreshToken.json`

**Purpose:** refresh token pair using `refresh_token`.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048). → `refreshToken` |
| `refreshToken` | refreshToken | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

> **Transaction:** `refreshToken` does not open a DB transaction. The host should wrap the refresh call (same scoped `IdentityContext`) in an external transaction so validation, new-token persistence, and old-token invalidation commit together.

---

## `main.Register.json`

**Purpose:** registration by email + password with confirmation code delivery.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `Password` (8–128). → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`; `userIdKey: UserId`, `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: createUser.selectorKey`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserId`. `next: null` |

---

## `main.RequestCode.json`

**Purpose:** send an email code with configurable TTL.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Ttl` (TimeSpan). → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: collectForm.Email`, `ttlKey: collectForm.Ttl`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `main.ResetPassword.json`

**Purpose:** change password by email after verifying the recovery code.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (required, 8–128), `Password` (8–128). → `verifyCode` |
| `verifyCode` | verifyCode | `channel: email`, `identityKey: collectForm.Email`, `codeKey: collectForm.Code`. → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `resolveBy.field: Email`. `next: null` |

> Recovery `Code` must be present, valid, and not expired; otherwise the flow rejects before changing the password.

---

## `main.Token.json`

**Purpose:** tokens by email and password **or** code (at least one required).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `Password` (opt., 8–32), `Code` (opt., 4–32). Validators: `requiredIf`, `atLeastOneRequired`. → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `codeKey`, `channel: email`, `resolveBy` (field, required, caseInsensitive). → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `main.TokenByCode.json`

**Purpose:** tokens by email + code only.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (4–32). → `token` |
| `token` | token | `selectorKey`, `codeKey`, `channel: email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `main.ExternalLogin.json`

**Purpose:** start OAuth (redirect to provider).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (opt., up to 512), `LinkUserId` (opt. Guid string). → `initiateExternalLogin` |
| `initiateExternalLogin` | initiateExternalLogin | `providerKey: collectForm.Provider`, `returnUrlKey: collectForm.ReturnUrl`, `linkUserIdKey: collectForm.LinkUserId`. → `collectResult` |
| `collectResult` | collectResult | `url = initiateExternalLogin.Url`. `next: null` |

> `LinkUserId` enables account linking when present and must match the authenticated principal (`sub` / NameIdentifier); omit for normal sign-in / sign-up.

---

## `main.ExternalLoginCallback.json`

**Purpose:** complete OAuth after provider redirect.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `State` (required); `Code` / `Error` (either via `requiredIf`/`atLeastOneRequired`); `ErrorDescription` (opt.). → `completeExternalLogin` |
| `completeExternalLogin` | completeExternalLogin | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_linking`. `next: null` |

> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`, see release plan §B.

---

## `kind` reference (registered factories)

| kind | Purpose |
|------|---------|
| `collectForm` | Collect and validate form fields |
| `collectResult` | Map `Bag` fields to API response |
| `createUser` | Create user |
| `sendCode` | Send OTP (email/SMS) |
| `verifyCode` | Verify OTP |
| `codeAuth` | Verify OTP + authenticate |
| `passwordAuth` | Verify email + password |
| `forgotPassword` | Start password recovery |
| `resetPassword` | Set new password |
| `getUserId` | Find user, return `UserId` |
| `token` | Issue access/refresh tokens |
| `refreshToken` | Refresh using refresh_token (host must wrap in an external DB transaction) |
| `initiateExternalLogin` | OAuth redirect URL |
| `completeExternalLogin` | OAuth callback, issue tokens |

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
