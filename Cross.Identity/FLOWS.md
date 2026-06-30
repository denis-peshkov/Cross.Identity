## Cross.Identity ProcessEngine flow reference

This document matches JSON in `Cross.Identity/ProcessEngine/Definitions/Flows/`.

### How flows work

- Files are named `{flow}.{operation}.json` (e.g. `license.Token.json`).
- Definition key: `{flow}.{operation}` in lowercase (`license.token`).
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

### All flow files (18)

| Flow | Operation | File |
|------|-----------|------|
| `edoctors` | Register | `edoctors.Register.json` |
| `game` | auth | `game.auth.json` |
| `game` | Register | `game.Register.json` |
| `game` | request-code | `game.request-code.json` |
| `game` | Token | `game.Token.json` |
| `license` | ForgotPassword | `license.ForgotPassword.json` |
| `license` | GetUserId | `license.GetUserId.json` |
| `license` | RefreshToken | `license.RefreshToken.json` |
| `license` | Register | `license.Register.json` |
| `license` | RequestCode | `license.RequestCode.json` |
| `license` | ResetPassword | `license.ResetPassword.json` |
| `license` | Token | `license.Token.json` |
| `license` | TokenByCode | `license.TokenByCode.json` |
| `license` | ExternalLogin | `license.ExternalLogin.json` |
| `license` | ExternalLoginCallback | `license.ExternalLoginCallback.json` |
| `shop` | auth | `shop.auth.json` |
| `shop` | Register | `shop.Register.json` |
| `shop` | request-code | `shop.request-code.json` |

---

## `edoctors.Register.json`

**Purpose:** eDoctors user registration by email with confirmation code delivery.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | Fields: `Email`, `FirstName`, `LastName`, `Password`, `ConfirmPassword`. Validator `equal(Password, ConfirmPassword)`. → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`, `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms` from `collectForm.*`; `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: createUser.selectorKey`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

> The form has no `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms` fields — they are defined only in the `createUser` step `map`.

---

## `game.auth.json`

**Purpose:** game sign-in with a one-time email code.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `UserName`, `Code` (4–8). → `codeAuth` |
| `codeAuth` | codeAuth | `channel: email`, `identityKey: collectForm.UserName`, `codeKey: collectForm.Code`, `resolveBy.field: UserName`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 43200`, `subKey: codeAuth.UserId`, claims: `scope=game.player`, `amr=email_code`. → `collectResult` |
| `collectResult` | collectResult | `token = issueJwt.Token`, `email = collectForm.Email` (no `Email` field in the form). |

---

## `game.Register.json`

**Purpose:** player registration with email code confirmation and JWT issuance.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `AgreeLow`, `AgreeService`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: registration.Email`. → `collectForm` |
| `collectForm` | collectForm | second form: `UserName`, `Code`. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: email`, `identityKey: verification.UserName`, `codeKey: verification.Code`. → `createUser` |
| `createUser` | createUser | map from `registration.*`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 604800`, `subKey: user.Id`, claims: `scope=game.player`, `amr=email_code`. |

> Two `collectForm` steps with the same `kind` — the flow will not load in `ProcessLoader` without renaming steps.

---

## `game.request-code.json`

**Purpose:** send an email code without registration/sign-in.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: EmailCode`. `next: null` |

---

## `game.Token.json`

**Purpose:** access/refresh tokens by email + password.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Password` (8–128). → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `channel: email`; result keys `AccessToken`, `RefreshToken`, `TokenType`, `ExpiresIn`. → `collectResult` |
| `collectResult` | collectResult | OAuth-like response: `access_token`, `refresh_token`, `token_type`, `expires_in`. `next: null` |

---

## `license.ForgotPassword.json`

**Purpose:** start password recovery (send code).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128). → `forgotPassword` |
| `forgotPassword` | forgotPassword | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = forgotPassword.LastCode`. `next: null` |

---

## `license.GetUserId.json`

**Purpose:** get `user_id` by email.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`. → `getUserId` |
| `getUserId` | getUserId | `selectorField: Email`, `selectorKey: collectForm.Email`. → `collectResult` |
| `collectResult` | collectResult | `user_id = getUserId.UserId`. `next: null` |

---

## `license.RefreshToken.json`

**Purpose:** refresh token pair using `refresh_token`.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `RefreshToken` (32–2048). → `refreshToken` |
| `refreshToken` | refreshToken | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

---

## `license.Register.json`

**Purpose:** registration by email + password with confirmation code delivery.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `Password` (8–128). → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`; `userIdKey: UserId`, `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: createUser.selectorKey`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserId`. `next: null` |

---

## `license.RequestCode.json`

**Purpose:** send an email code with configurable TTL.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Ttl` (TimeSpan). → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `license.ResetPassword.json`

**Purpose:** change password by email (optionally with code).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (opt., 8–128), `Password` (8–128). → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `resolveBy.field: Email`. `next: null` |

---

## `license.Token.json`

**Purpose:** tokens by email and password **or** code (at least one required).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `Password` (opt., 8–32), `Code` (opt., 4–32). Validators: `requiredIf`, `atLeastOneRequired`. → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `codeKey`, `channel: email`, `resolveBy` (field, required, caseInsensitive). → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

---

## `license.TokenByCode.json`

**Purpose:** tokens by email + code only.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (4–32). → `token` |
| `token` | token | `selectorKey`, `codeKey`, `channel: email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `license.ExternalLogin.json`

**Purpose:** start OAuth (redirect to provider).

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (opt., up to 512). → `initiateExternalLogin` |
| `initiateExternalLogin` | initiateExternalLogin | `providerKey: collectForm.Provider`, `returnUrlKey: collectForm.ReturnUrl`, `linkUserIdKey: collectForm.LinkUserId`. → `collectResult` |
| `collectResult` | collectResult | `url = initiateExternalLogin.Url`. `next: null` |

> `LinkUserId` is not in the form schema but may be passed in the input payload for account linking.

---

## `license.ExternalLoginCallback.json`

**Purpose:** complete OAuth after provider redirect.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Code`, `State` (required), `Error`, `ErrorDescription` (opt.). → `completeExternalLogin` |
| `completeExternalLogin` | completeExternalLogin | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey` from `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_linking`. `next: null` |

> Between `ExternalLogin` and `ExternalLoginCallback`, `ExternalLoginService` stores one-time OAuth state in `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Provider and callback configuration — `Authentication:ExternalLogin`, see release plan §B.

---

## `shop.auth.json`

**Purpose:** shop sign-in by phone and SMS code.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Phone`, `Code` (4–8). → `codeAuth` |
| `codeAuth` | codeAuth | `channel: phone`, `identityKey: auth.Phone`, `codeKey: auth.Code`, `resolveBy.field: Phone`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 43200`, `subKey: user.Id`, claims: `scope=shop.customer`, `amr=sms_code`. |

---

## `shop.Register.json`

**Purpose:** customer registration with SMS confirmation.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Email`, `UserName`, `FirstName`, `LastName`, `Phone`. → `sendCode` |
| `sendCode` | sendCode | `channel: phone`, `selectorKey: registration.Phone`. → `collectForm` |
| `collectForm` | collectForm | `Phone`, `Code`. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: phone`, `identityKey: verification.Phone`, `codeKey: verification.Code`. → `createUser` |
| `createUser` | createUser | map from `registration.*`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 1209600`, `subKey: user.Id`, claims: `scope=shop.customer`, `amr=sms_code`. |

> As in `game.Register.json`, two `collectForm` steps with one `kind` are incompatible with the current `ProcessLoader`.

---

## `shop.request-code.json`

**Purpose:** request an SMS code for a phone number.

| Step | kind | Details |
|------|------|---------|
| `collectForm` | collectForm | `Phone`. → `sendCode` |
| `sendCode` | sendCode | `channel: phone`, `selectorKey: request.Phone`. `next: null` |

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
| `refreshToken` | Refresh using refresh_token |
| `initiateExternalLogin` | OAuth redirect URL |
| `completeExternalLogin` | OAuth callback, issue tokens |

JSON also uses `issueJwt`, but a separate `IssueJwtStepFactory` is **not registered** in `AddCrossIdentity` — flows `game.auth`, `game.Register`, `shop.auth`, `shop.Register` with this step will not run until the step is implemented and registered.

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
