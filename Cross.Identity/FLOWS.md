## Описание flow'ов Cross.Identity ProcessEngine

Документ соответствует JSON в каталоге `Cross.Identity/ProcessEngine/Definitions/Flows/`.

### Как устроены flow'ы

- Файл именуется `{flow}.{operation}.json` (например, `license.Token.json`).
- Ключ дефиниции: `{flow}.{operation}` в нижнем регистре (`license.token`).
- Вызов из кода: `IFlowExecutor.ExecuteAsync(input, flow, FlowOperationEnum.Operation, ct)`.
- Степы выполняются по цепочке `next`; поле `start` указывает первый шаг.
- В рамках одного flow каждый `kind` шага должен быть **уникален** (два `collectForm` в одном JSON не загрузятся).
- Данные формы сохраняются в `Bag` с префиксом `collectForm.{поле}` (см. `CollectFormStep`).
- Относительные ключи (`Email`, `selectorKey`) квалифицируются как `{kind}.{ключ}`; абсолютные — с точкой (`collectForm.Email`).

### Операции (`FlowOperationEnum`)

| Файл (пример) | Enum |
|---------------|------|
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

### Все flow-файлы (18)

| Flow | Операция | Файл |
|------|----------|------|
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

**Назначение:** регистрация пользователя eDoctors по email с отправкой кода подтверждения.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | Поля: `Email`, `FirstName`, `LastName`, `Password`, `ConfirmPassword`. Валидатор `equal(Password, ConfirmPassword)`. → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`, `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms` из `collectForm.*`; `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: createUser.selectorKey`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

> В форме нет полей `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms` — они заданы только в `map` шага `createUser`.

---

## `game.auth.json`

**Назначение:** вход в игру по одноразовому email-коду.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `UserName`, `Code` (4–8). → `codeAuth` |
| `codeAuth` | codeAuth | `channel: email`, `identityKey: collectForm.UserName`, `codeKey: collectForm.Code`, `resolveBy.field: UserName`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 43200`, `subKey: codeAuth.UserId`, claims: `scope=game.player`, `amr=email_code`. → `collectResult` |
| `collectResult` | collectResult | `token = issueJwt.Token`, `email = collectForm.Email` (поля `Email` в форме нет). |

---

## `game.Register.json`

**Назначение:** регистрация игрока с подтверждением email-кода и выдачей JWT.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `AgreeLow`, `AgreeService`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: registration.Email`. → `collectForm` |
| `collectForm` | collectForm | вторая форма: `UserName`, `Code`. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: email`, `identityKey: verification.UserName`, `codeKey: verification.Code`. → `createUser` |
| `createUser` | createUser | map из `registration.*`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 604800`, `subKey: user.Id`, claims: `scope=game.player`, `amr=email_code`. |

> Два шага `collectForm` с одинаковым `kind` — flow не пройдёт загрузку в `ProcessLoader` без переименования шагов.

---

## `game.request-code.json`

**Назначение:** отправка email-кода без регистрации/логина.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: EmailCode`. `next: null` |

---

## `game.Token.json`

**Назначение:** access/refresh токены по email + пароль.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email` (8–128), `Password` (8–128). → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `channel: email`; ключи результата `AccessToken`, `RefreshToken`, `TokenType`, `ExpiresIn`. → `collectResult` |
| `collectResult` | collectResult | OAuth-подобный ответ: `access_token`, `refresh_token`, `token_type`, `expires_in`. `next: null` |

---

## `license.ForgotPassword.json`

**Назначение:** инициировать восстановление пароля (отправка кода).

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email` (8–128). → `forgotPassword` |
| `forgotPassword` | forgotPassword | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = forgotPassword.LastCode`. `next: null` |

---

## `license.GetUserId.json`

**Назначение:** получить `user_id` по email.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`. → `getUserId` |
| `getUserId` | getUserId | `selectorField: Email`, `selectorKey: collectForm.Email`. → `collectResult` |
| `collectResult` | collectResult | `user_id = getUserId.UserId`. `next: null` |

---

## `license.RefreshToken.json`

**Назначение:** обновление пары токенов по `refresh_token`.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `RefreshToken` (32–2048). → `refreshToken` |
| `refreshToken` | refreshToken | `refreshTokenKey: collectForm.RefreshToken`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

---

## `license.Register.json`

**Назначение:** регистрация по email + пароль с отправкой кода подтверждения.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`, `Password` (8–128). → `createUser` |
| `createUser` | createUser | map: `Email`, `Password`; `userIdKey: UserId`, `selectorKey: collectForm.Email`. → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: createUser.selectorKey`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode`, `UserId`. `next: null` |

---

## `license.RequestCode.json`

**Назначение:** отправка email-кода с настраиваемым TTL.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email` (8–128), `Ttl` (TimeSpan). → `sendCode` |
| `sendCode` | sendCode | `channel: email`, `selectorKey: collectForm.Email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `LastCode = sendCode.LastCode`. `next: null` |

---

## `license.ResetPassword.json`

**Назначение:** смена пароля по email (опционально с кодом).

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (опц., 8–128), `Password` (8–128). → `resetPassword` |
| `resetPassword` | resetPassword | `channel: email`, `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `resolveBy.field: Email`. `next: null` |

---

## `license.Token.json`

**Назначение:** токены по email и паролю **или** коду (хотя бы одно обязательно).

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`, `Password` (опц., 8–32), `Code` (опц., 4–32). Валидаторы: `requiredIf`, `atLeastOneRequired`. → `token` |
| `token` | token | `selectorKey`, `passwordKey`, `codeKey`, `channel: email`, `resolveBy` (field, required, caseInsensitive). → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`. `next: null` |

---

## `license.TokenByCode.json`

**Назначение:** токены только по email + коду.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email` (8–128), `Code` (4–32). → `token` |
| `token` | token | `selectorKey`, `codeKey`, `channel: email`, `resolveBy.field: Email`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_invalid_code`. `next: null` |

---

## `license.ExternalLogin.json`

**Назначение:** начало OAuth (редирект на провайдера).

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Provider` (2–32), `ReturnUrl` (опц., до 512). → `initiateExternalLogin` |
| `initiateExternalLogin` | initiateExternalLogin | `providerKey: collectForm.Provider`, `returnUrlKey: collectForm.ReturnUrl`, `linkUserIdKey: collectForm.LinkUserId`. → `collectResult` |
| `collectResult` | collectResult | `url = initiateExternalLogin.Url`. `next: null` |

> Поле `LinkUserId` не в схеме формы, но может передаваться во входном payload для привязки аккаунта.

---

## `license.ExternalLoginCallback.json`

**Назначение:** завершение OAuth после редиректа провайдера.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Code`, `State` (обяз.), `Error`, `ErrorDescription` (опц.). → `completeExternalLogin` |
| `completeExternalLogin` | completeExternalLogin | `codeKey`, `stateKey`, `errorKey`, `errorDescriptionKey` из `collectForm.*`. → `collectResult` |
| `collectResult` | collectResult | `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`, `is_linking`. `next: null` |

> Между `ExternalLogin` и `ExternalLoginCallback` `ExternalLoginService` хранит одноразовый OAuth state в `auth.ExternalLoginStates` (TTL — `ExternalLoginOptions.StateLifetime`). Настройка провайдеров и callback — `Authentication:ExternalLogin`, см. release-план §B.

---

## `shop.auth.json`

**Назначение:** вход в магазин по телефону и SMS-коду.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Phone`, `Code` (4–8). → `codeAuth` |
| `codeAuth` | codeAuth | `channel: phone`, `identityKey: auth.Phone`, `codeKey: auth.Code`, `resolveBy.field: Phone`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 43200`, `subKey: user.Id`, claims: `scope=shop.customer`, `amr=sms_code`. |

---

## `shop.Register.json`

**Назначение:** регистрация покупателя с SMS-подтверждением.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Email`, `UserName`, `FirstName`, `LastName`, `Phone`. → `sendCode` |
| `sendCode` | sendCode | `channel: phone`, `selectorKey: registration.Phone`. → `collectForm` |
| `collectForm` | collectForm | `Phone`, `Code`. → `verifyCode` |
| `verifyCode` | verifyCode | `channel: phone`, `identityKey: verification.Phone`, `codeKey: verification.Code`. → `createUser` |
| `createUser` | createUser | map из `registration.*`. → `issueJwt` |
| `issueJwt` | issueJwt | `lifetimeSeconds: 1209600`, `subKey: user.Id`, claims: `scope=shop.customer`, `amr=sms_code`. |

> Как и в `game.Register.json`, два `collectForm` с одним `kind` несовместимы с текущим `ProcessLoader`.

---

## `shop.request-code.json`

**Назначение:** запрос SMS-кода на телефон.

| Шаг | kind | Детали |
|-----|------|--------|
| `collectForm` | collectForm | `Phone`. → `sendCode` |
| `sendCode` | sendCode | `channel: phone`, `selectorKey: request.Phone`. `next: null` |

---

## Справочник по `kind` (зарегистрированные фабрики)

| kind | Назначение |
|------|------------|
| `collectForm` | Сбор и валидация полей формы |
| `collectResult` | Маппинг полей `Bag` в ответ API |
| `createUser` | Создание пользователя |
| `sendCode` | Отправка OTP (email/SMS) |
| `verifyCode` | Проверка OTP |
| `codeAuth` | Проверка OTP + аутентификация |
| `passwordAuth` | Проверка email + пароль |
| `forgotPassword` | Старт восстановления пароля |
| `resetPassword` | Установка нового пароля |
| `getUserId` | Поиск пользователя, возврат `UserId` |
| `token` | Выдача access/refresh токенов |
| `refreshToken` | Обновление по refresh_token |
| `initiateExternalLogin` | URL редиректа OAuth |
| `completeExternalLogin` | Callback OAuth, выдача токенов |

В JSON также встречается `issueJwt`, но отдельная фабрика `IssueJwtStepFactory` в `AddCrossIdentity` **не регистрируется** — flow'ы `game.auth`, `game.Register`, `shop.auth`, `shop.Register` с этим шагом не выполнятся, пока шаг не будет реализован и зарегистрирован.

### Валидаторы форм (`schemaDef.validators`)

| kind | Описание |
|------|----------|
| `equal` | Равенство двух полей |
| `notEqual` | Неравенство двух полей |
| `oneOf` | Значение из списка |
| `requiredIf` | Условная обязательность |
| `exactlyOneRequired` | Ровно одно из полей |
| `atLeastOneRequired` | Хотя бы одно из полей |

Схему можно задать через `schema` (имя в `IFormSchemaProvider`), `schemaDef` (inline) или `schemaPatch` (add/remove/override/rename).
