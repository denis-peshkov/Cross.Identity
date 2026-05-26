## Описание flow'ов в Cross.Identity ProcessEngine

Этот документ описывает все flow'ы и их степы (шаги) в каталоге `server/Cross.Identity/ProcessEngine/Definitions/Flows`.

Степы выполняются последовательно, через поле `next`. Поле `start` в JSON указывает, с какого степа начинается выполнение.

---

## `edoctors.Register.json` — регистрация в eDoctors

- **Назначение**: регистрация пользователя eDoctors по email с подтверждением по коду.
- **Степы**:

  - **`collectForm`**
    - Назначение: Собирает форму по перечисленным полям, а также проводит первичную и дополнительные валидации.
    - Поля:
      - `Email` — Email, обязателен.
      - `FirstName` — имя, обязательно, длина 3–128.
      - `LastName` — фамилия, обязательно, длина 3–128.
      - `Password` — пароль, обязателен, длина 8–128.
      - `ConfirmPassword` — повтор пароля, обязателен, длина 8–128.
    - Валидаторы:
      - `equal(Password, ConfirmPassword)` — «Passwords do not match.»
    - Переход: `createUser`.

  - **`createUser`**
    - Создаёт пользователя в системе.
    - Маппинг из результата формы (`collectForm.*`):
      - `Email`
      - `FullName`
      - `Company`
      - `AcceptGetEmails`
      - `AcceptLicenseTerms`
    - `selectorKey: collectForm.Email` — идентификатор пользователя.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправляет код подтверждения по email.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: createUser.selectorKey` (email пользователя).
      - `resolveBy.field: "Email"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Собирает результат для вызвавшей стороны.
    - Маппинг:
      - `LastCode = sendCode.LastCode`.
    - `next: null` — завершение flow.

---

## `game.auth.json` — аутентификация игрока по email‑коду

- **Назначение**: вход в игру по одноразовому коду, высланному по email.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `UserName` — имя/логин пользователя, обязательно.
      - `Code` — одноразовый код, обязателен, длина 4–8.
    - Переход: `next: codeAuth`.
  - **`codeAuth`**
    - Проверяет одноразовый код.
    - Параметры:
      - `channel: "email"`.
      - `identityKey: collectForm.UserName`.
      - `codeKey: collectForm.Code`.
      - `resolveBy.field: "UserName"`.
    - На выходе должен появиться `UserId`.
    - Переход: `next: issueJwt`.
  - **`issueJwt`**
    - Выпускает JWT‑токен для игрока.
    - Параметры:
      - `lifetimeSeconds: 43200` (12 часов).
      - `subKey: codeAuth.UserId`.
      - `claims`:
        - `scope = "game.player"`.
        - `amr = "email_code"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Формирует результат авторизации.
    - Маппинг:
      - `token = issueJwt.Token`.
      - `email = collectForm.Email` (в текущей схеме в форме поля `Email` нет — это потенциальная неточность схемы).

---

## `game.Register.json` — регистрация игрока по email‑коду

- **Назначение**: регистрация нового игрока в игре с подтверждением email‑кода.
- **Степы**:
  - **`collectForm`** (регистрационная форма)
    - `schemaDef.name: "registration"`.
    - Поля:
      - `Email` — Email, обязателен.
      - `UserName` — логин, обязателен, длина 3–32.
      - `FirstName` — имя, необязательно, до 64 символов.
      - `LastName` — фамилия, необязательно, до 64 символов.
      - `BirthDate` — дата рождения, необязательно.
      - `Gender` — пол, необязательно.
      - `AgreeLow` — согласие с политикой/правилами (булевое), обязательно.
      - `AgreeService` — согласие с условиями сервиса, обязательно.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправляет код подтверждения на email.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: "registration.Email"`.
    - Переход: `next: collectForm` (вторая форма).
  - **второй `collectForm`** (форма кода)
    - Форма ввода подтверждающего кода.
    - Поля:
      - `UserName` — логин, обязателен.
      - `Code` — код, обязателен, длина 4–8.
    - Переход: `next: verifyCode`.
  - **`verifyCode`**
    - Проверяет email‑код.
    - Параметры:
      - `channel: "email"`.
      - `identityKey: "verification.UserName"`.
      - `codeKey: "verification.Code"`.
    - Переход: `next: createUser`.
  - **`createUser`**
    - Создаёт пользователя после успешной проверки кода.
    - Маппинг из `registration.*`:
      - `Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `AgreeLow`, `AgreeService`.
    - Переход: `next: issueJwt`.
  - **`issueJwt`**
    - Выпускает JWT‑токен игрока.
    - Параметры:
      - `lifetimeSeconds: 604800` (7 дней).
      - `subKey: "user.Id"`.
      - `claims`:
        - `scope = "game.player"`.
        - `amr = "email_code"`.

---

## `game.request-code.json` — запрос email‑кода для игры

- **Назначение**: отправка кода на email без немедленной регистрации/логина.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправляет код по email.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: collectForm.Email`.
      - `resolveBy.field: "EmailCode"`.
    - `next: null`.

---

## `game.Token.json` — выдача токена игры по email+пароль

- **Назначение**: получение access/refresh токенов игры по паре email+пароль.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
      - `Password` — пароль, обязателен, длина 8–128.
    - Переход: `next: token`.
  - **`token`**
    - Выполняет операцию получения токенов.
    - Параметры:
      - `selectorKey: collectForm.Email`.
      - `passwordKey: collectForm.Password`.
      - `channel: "email"`.
      - Ключи результата:
        - `accessTokenKey: "AccessToken"`.
        - `refreshTokenKey: "RefreshToken"`.
        - `tokenTypeKey: "TokenType"`.
        - `expiresInKey: "ExpiresIn"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг в «oauth‑подобный» ответ:
      - `access_token = token.AccessToken`.
      - `refresh_token = token.RefreshToken`.
      - `token_type = token.TokenType`.
      - `expires_in = token.ExpiresIn`.
    - `next: null`.

---

---

---

## `license.Auth.json` — аутентификация портала лицензий (пароль + OTP)

- **Назначение**: усиленная авторизация в портале лицензий по паролю и одноразовому коду.
- **Степы**:
  - **`collectForm`**
    - `schemaDef.name: "auth"`.
    - Базовые поля:
      - `Email` — Email, обязателен.
      - `Password` — пароль, обязателен, длина 8–128.
    - `schemaPatch.add`:
      - `OtpCode` — строка, обязателен, длина 4–8.
    - Переход: `next: pwd-auth`.
  - **`passwordAuth`**
    - Проверка пары логин/пароль.
    - Параметры:
      - `selectorField: "Email"`.
      - `selectorKey: "auth.Email"`.
      - `passwordKey: "auth.Password"`.
    - Переход: `next: verify-otp`.
  - **`verifyCode`**
    - Проверка одноразового кода.
    - Параметры:
      - `channel: "email"`.
      - `identityKey: "auth.Email"`.
      - `codeKey: "auth.OtpCode"`.
    - Переход: `next: issue`.
  - **`issueJwt`**
    - Выдаёт JWT для портала лицензий.
    - Параметры:
      - `lifetimeSeconds: 28800` (8 часов).
      - `subKey: "user.Id"`.
      - `claims`:
        - `scope = "license.portal"`.
        - `amr = "pwd+otp"`.

---

## `license.ForgotPassword.json` — запуск восстановления пароля

- **Назначение**: инициировать процесс «забыли пароль» для пользователя портала лицензий.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
    - Переход: `next: forgotPassword`.
  - **`forgotPassword`**
    - Степ, который инициирует процедуру восстановления пароля (обычно отправка ссылки/кода).
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: collectForm.Email`.
    - `next: null`.

---

## `license.GetUser.json` — получение пользователя по email

- **Назначение**: найти пользователя портала лицензий по email.
- **Степы**:
  - **`collectForm`**
    - `schemaDef.name: "get"`.
    - Поля:
      - `Email` — Email, обязателен.
    - Переход: `next: getUser`.
  - **`getUser`**
    - Ищет пользователя по указанному email.
    - Параметры:
      - `selectorField: "Email"`.
      - `selectorKey: "get.Email"`.
    - `next: null` — результатом flow будет найденный пользователь.

---

## `license.RefreshToken.json` — обновление токена по refresh_token

- **Назначение**: получить новую пару токенов по действующему refresh_token.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `RefreshToken` — строка, обязателен, длина 32–2048.
    - Переход: `next: refreshToken`.
  - **`refreshToken`**
    - Генерация новых токенов.
    - Параметры:
      - `refreshTokenKey: "collectForm.RefreshToken"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг результата:
      - `access_token = refreshToken.AccessToken`.
      - `refresh_token = refreshToken.RefreshToken`.
      - `token_type = refreshToken.TokenType`.
      - `expires_in = refreshToken.ExpiresIn`.
      - `user_id = refreshToken.UserId`.
    - `next: null`.

---

## `license.Register.json` — простая регистрация по email+пароль с кодом

- **Назначение**: регистрация пользователя портала по email+пароль с подтверждением email‑кода.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен.
      - `Password` — пароль, обязателен, длина 8–128.
      - `ConfirmPassword` — повтор пароля, обязателен, длина 8–128.
    - Валидаторы:
      - `equal(Password, ConfirmPassword)` — «Passwords do not match.»
    - Переход: `next: createUser`.
  - **`createUser`**
    - Создаёт пользователя.
    - Маппинг:
      - `Email = collectForm.Email`.
      - `Password = collectForm.Password`.
    - Параметры:
      - `userIdKey: "UserId"`.
      - `selectorKey: "collectForm.Email"`.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправка кода подтверждения на email.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: "createUser.selectorKey"`.
      - `resolveBy.field: "Email"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг результата:
      - `LastCode = sendCode.LastCode`.
      - `UserId = createUser.UserId`.
    - `next: null`.

---

## `license.register1.json` — расширенная регистрация с подтверждением email‑кода

- **Назначение**: регистрация с дополнительными полями профиля и обязательным подтверждением email.
- **Степы**:
  - **`collectForm`** (регистрационная форма)
    - Назначение: ___
    - Поля:
      - `Email` — Email, обязателен.
      - `FullName` — ФИО, обязательно, длина 3–128.
      - `Company` — компания, обязательно, длина 2–128.
      - `Password` — пароль, обязателен, длина 8–128.
      - `ConfirmPassword` — повтор пароля, обязателен, длина 8–128.
      - `AcceptGetEmails` — согласие на рассылку, опционально.
      - `AcceptLicenseTerms` — согласие с лицензионными условиями, обязательно.
    - Валидаторы:
      - `equal(Password, ConfirmPassword)` — «Passwords do not match.»
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправка email‑кода.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: "collectForm.Email"`.
    - Переход: `next: ver-form`.
  - **`collectForm`** (форма верификации, `name: "ver-form"`)
    - Поля:
      - `Email` — Email, обязателен.
      - `Code` — код, обязателен, длина 4–8.
    - Переход: `next: verifyCode`.
  - **`verifyCode`**
    - Проверка email‑кода.
    - Параметры:
      - `channel: "email"`.
      - `identityKey: "verification.Email"`.
      - `codeKey: "verification.Code"`.
    - Переход: `next: createUser`.
  - **`createUser`**
    - Создаёт пользователя после подтверждения.
    - Маппинг из `registration.*`:
      - `Email`, `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms`.
    - Переход: `next: issueJwt`.
  - **`issueJwt`**
    - JWT для портала лицензий.
    - Параметры:
      - `lifetimeSeconds: 43200`.
      - `subKey: "user.Id"`.
      - `claims`:
        - `scope = "license.portal"`.
        - `amr = "email_code"`.

---

## `license.RequestCode.json` — запрос email‑кода

- **Назначение**: отправка email‑кода с заданным временем жизни (TTL).
- **Степы**:
  - **`collectForm`**
    - Назначение: ...
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
      - `Ttl` — TimeSpan, обязателен.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправка кода.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: "collectForm.Email"`.
      - `resolveBy.field: "Email"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг:
      - `LastCode = sendCode.LastCode`.
    - Переход: `null`.

---

## `license.ResetPassword.json` — отправка кода для сброса пароля

- **Назначение**: отправить код для последующей смены пароля.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправка кода для сброса пароля.
    - Параметры:
      - `channel: "email"`.
      - `selectorKey: "reset.Email"`.
    - `next: null`.

---

## `license.Token.json` — получение токена по паролю или коду

- **Назначение**: универсальный endpoint, который выдаёт токен либо по паролю, либо по коду (обязательно хотя бы одно).
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
      - `Password` — пароль, опционально, длина 8–128.
      - `Code` — одноразовый код, опционально.
    - Валидаторы:
      - `requiredIf`:
        - если `Code == ""`, то `Password` обязателен;
        - если `Password == ""`, то `Code` обязателен.
      - `atLeastOneRequired(["Password", "Code"])` — хотя бы одно из двух полей должно быть заполнено.
    - Переход: `next: token`.
  - **`token`**
    - Выдача токенов по email + (пароль или код).
    - Параметры:
      - `selectorKey: "collectForm.Email"`.
      - `passwordKey: "collectForm.Password"`.
      - `codeKey: "collectForm.Code"`.
      - `channel: "email"`.
      - `resolveBy.field: "Email"`.
      - Ключи результата:
        - `accessTokenKey: "AccessToken"`.
        - `refreshTokenKey: "RefreshToken"`.
        - `tokenTypeKey: "TokenType"`.
        - `expiresInKey: "ExpiresIn"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг:
      - `access_token = token.AccessToken`.
      - `refresh_token = token.RefreshToken`.
      - `token_type = token.TokenType`.
      - `expires_in = token.ExpiresIn`.
    - `next: null`.

---

## `license.TokenByCode.json` — получение токена только по коду

- **Назначение**: выдача токена, когда у клиента есть только email и код (без пароля).
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Email` — Email, обязателен, длина 8–128.
      - `Code` — код, опционально (по схеме), длина 8–128.
    - Переход: `next: token`.
  - **`token`**
    - Операция выдачи токена по email+код.
    - Параметры:
      - `selectorKey: "collectForm.Email"`.
      - `codeKey: "collectForm.Code"`.
      - `channel: "email"`.
      - `resolveBy.field: "Email"`.
    - Переход: `next: collectResult`.
  - **`collectResult`**
    - Маппинг:
      - `access_token = token.AccessToken`.
      - `refresh_token = token.RefreshToken`.
      - `token_type = token.TokenType`.
      - `expires_in = token.ExpiresIn`.
      - `user_id = token.UserId`.
    - `next: null`.

---

## `shop.auth.json` — аутентификация магазина по SMS‑коду

- **Назначение**: вход в магазин по телефону и SMS‑коду.
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Phone` — номер телефона, обязателен.
      - `Code` — одноразовый код, обязателен, длина 4–8.
    - Переход: `next: codeAuth`.
  - **`codeAuth`**
    - Проверка SMS‑кода.
    - Параметры:
      - `channel: "phone"`.
      - `identityKey: "auth.Phone"`.
      - `codeKey: "auth.Code"`.
      - `resolveBy.field: "Phone"`.
    - Переход: `next: issueJwt`.
  - **`issueJwt`**
    - Выдаёт JWT для покупателя.
    - Параметры:
      - `lifetimeSeconds: 43200`.
      - `subKey: "user.Id"`.
      - `claims`:
        - `scope = "shop.customer"`.
        - `amr = "sms_code"`.

---

## `shop.Register.json` — регистрация в магазине по SMS‑коду

- **Назначение**: регистрация нового покупателя с подтверждением телефона через SMS‑код.
- **Степы**:
  - **`collectForm`** (регистрационная форма)
    - Поля:
      - `Email` — Email, обязателен.
      - `UserName` — логин, обязателен, длина 3–32.
      - `FirstName` — имя, опционально.
      - `LastName` — фамилия, опционально.
      - `Phone` — телефон, обязателен.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправляет SMS‑код на указанный телефон.
    - Параметры:
      - `channel: "phone"`.
      - `selectorKey: "registration.Phone"`.
    - Переход: `next: collectForm` (форма верификации).
  - **второй `collectForm`** (форма подтверждения, `name: "verification"`)
    - Поля:
      - `Phone` — телефон, обязателен.
      - `Code` — код, обязателен, длина 4–8.
    - Переход: `next: verifyCode`.
  - **`verifyCode`**
    - Проверка SMS‑кода.
    - Параметры:
      - `channel: "phone"`.
      - `identityKey: "verification.Phone"`.
      - `codeKey: "verification.Code"`.
    - Переход: `next: createUser`.
  - **`createUser`**
    - Создаёт пользователя после подтверждения кода.
    - Маппинг из `registration.*`:
      - `Email`, `UserName`, `FirstName`, `LastName`, `Phone`.
    - Переход: `next: issueJwt`.
  - **`issueJwt`**
    - JWT для покупателя магазина.
    - Параметры:
      - `lifetimeSeconds: 1209600` (14 дней).
      - `subKey: "user.Id"`.
      - `claims`:
        - `scope = "shop.customer"`.
        - `amr = "sms_code"`.

---

## `shop.request-code.json` — запрос SMS‑кода

- **Назначение**: отправить SMS‑код на указанный телефон (например, перед регистрацией или логином).
- **Степы**:
  - **`collectForm`**
    - Поля:
      - `Phone` — телефон, обязателен.
    - Переход: `next: sendCode`.
  - **`sendCode`**
    - Отправка SMS‑кода.
    - Параметры:
      - `channel: "phone"`.
      - `selectorKey: "request.Phone"`.
    - `next: null`.

---

## Консолидированная сводка по степам (`kind`)

Ниже степы сгруппированы по `kind` с перечислением всех мест использования в flow’ах.

---

### Степ `collectForm`

- **Назначение**: описание и валидация формы (поля + валидаторы), сбор пользовательского ввода и сохранение его в контекст.
- **Общие параметры**:
  - `schemaDef.name` (опционально) — имя формы в контексте (`registration`, `auth`, `get`, `verification` и т.д.).
  - `schemaDef.fields` — набор полей с типами (`Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `Phone`, `Password`, `Code`, `RefreshToken`, `Ttl`, флаги согласия и т.п.).
  - `schemaDef.validators` / `schemaPatch` — декларативная валидация (равенство, requiredIf, atLeastOneRequired, добавление полей).
- **Использования (по flow)**:
  - **`edoctors.Register.json`**
    - Собирает форму регистрации: `Email`, `FirstName`, `LastName`, `Password`, `ConfirmPassword`.
    - Валидатор: равенство `Password` и `ConfirmPassword`.
  - **`game.auth.json`**
    - Форма входа по коду: `UserName`, `Code` (4–8 символов).
  - **`game.Register.json`**
    - Регистрация (`schemaDef.name: "registration"`): `Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `AgreeLow`, `AgreeService`.
    - Вторая форма (код): `UserName`, `Code`.
  - **`game.request-code.json`**
    - Форма запроса кода: `Email`.
  - **`game.Token.json`**
    - Форма логина по паролю: `Email`, `Password`.
  - **`license.Auth.json`**
    - Форма авторизации (`name: "auth"`): `Email`, `Password` + через `schemaPatch` добавляется `OtpCode`.
  - **`license.ForgotPassword.json`**
    - Форма «забыли пароль»: `Email`.
  - **`license.GetUser.json`**
    - Форма поиска пользователя (`name: "get"`): `Email`.
  - **`license.RefreshToken.json`**
    - Форма обновления токена: `RefreshToken`.
  - **`license.Register.json`**
    - Простая регистрация: `Email`, `Password`, `ConfirmPassword` + валидатор равенства паролей.
  - **`license.register1.json`**
    - Регистрационная форма: `Email`, `FullName`, `Company`, `Password`, `ConfirmPassword`, `AcceptGetEmails`, `AcceptLicenseTerms` + проверка совпадения паролей.
    - Форма верификации (`name: "ver-form"`): `Email`, `Code`.
  - **`license.RequestCode.json`**
    - Форма запроса кода: `Email`, `Ttl` (TimeSpan).
  - **`license.ResetPassword.json`**
    - Форма для сброса пароля: `Email`.
  - **`license.Token.json`**
    - Универсальная форма токена: `Email` + опциональные `Password` и `Code` с валидаторами `requiredIf` и `atLeastOneRequired`.
  - **`license.TokenByCode.json`**
    - Форма токена по коду: `Email`, `Code`.
  - **`shop.auth.json`**
    - Форма входа по SMS: `Phone`, `Code`.
  - **`shop.Register.json`**
    - Регистрация: `Email`, `UserName`, `FirstName`, `LastName`, `Phone`.
    - Форма верификации (`name: "verification"`): `Phone`, `Code`.
  - **`shop.request-code.json`**
    - Форма запроса SMS‑кода: `Phone`.

---

### Степ `createUser`

- **Назначение**: создать пользователя на основе ранее собранных форм, выставив служебные ключи в контексте.
- **Общие параметры**:
  - `map` — какие поля формы попасть в команду создания пользователя (`Email`, `FullName`, `Company`, `Password`, `Phone`, флаги согласий и т.п.).
  - `userIdKey` (опционально) — куда сохранить идентификатор пользователя в контексте.
  - `selectorKey` (опционально) — ключ‑идентификатор (обычно email), который используется для дальнейших степов (`sendCode`, `token`).
- **Использования**:
  - **`edoctors.Register.json`**
    - Берёт из `collectForm.*`: `Email`, `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms`.
    - Сохраняет `selectorKey = collectForm.Email` для отправки кода.
  - **`game.Register.json`**
    - Берёт из `registration.*`: `Email`, `UserName`, `FirstName`, `LastName`, `BirthDate`, `Gender`, `AgreeLow`, `AgreeService`.
  - **`license.Register.json`**
    - Берёт `Email`, `Password` из формы.
    - Сохраняет `userIdKey: "UserId"` и `selectorKey: collectForm.Email`.
  - **`license.register1.json`**
    - Берёт из `registration.*`: `Email`, `FullName`, `Company`, `AcceptGetEmails`, `AcceptLicenseTerms`.
  - **`shop.Register.json`**
    - Берёт из `registration.*`: `Email`, `UserName`, `FirstName`, `LastName`, `Phone`.

---

### Степ `sendCode`

- **Назначение**: сгенерировать и отправить одноразовый код (email или SMS) и сохранить его в контекст.
- **Общие параметры**:
  - `channel` — `"email"` или `"phone"`.
  - `selectorKey` — откуда взять идентификатор (email/phone) для отправки.
  - `resolveBy.field` (опционально) — поле, по которому код будет резолвиться/искаться.
  - На выходе часто есть `LastCode`, доступный для `collectResult`.
- **Использования**:
  - **Email‑канал**:
    - `edoctors.Register.json`: подтверждение email нового пользователя.
    - `game.Register.json`: отправка кода регистрации игроку.
    - `game.request-code.json`: запрос игрового кода по email (`resolveBy.field: "EmailCode"`).
    - `game.Token.json`: не используется.
    - `license.Register.json`: отправка кода после создания пользователя.
    - `license.register1.json`: код подтверждения email для регистрации.
    - `license.RequestCode.json`: отправка кода с заданным TTL, `resolveBy.field: "Email"`.
    - `license.ResetPassword.json`: код для сброса пароля (источник `reset.Email`).
  - **SMS‑канал**:
    - `shop.Register.json`: отправка SMS‑кода на `registration.Phone`.
    - `shop.request-code.json`: запрос SMS‑кода на `request.Phone`.

---

### Степы `verifyCode` и `codeAuth`

- **Назначение**: проверить ранее отправленный одноразовый код.
- **Общие параметры**:
  - `channel` — `"email"` или `"phone"`.
  - `identityKey` — откуда взять идентификатор (email/phone/логин).
  - `codeKey` — откуда взять введённый код.
  - `resolveBy.field` (у `codeAuth`) — по какому полю искать код (например, `UserName`, `Phone`).
- **Особенность**:
  - `verifyCode` — чистая проверка кода (подтверждение контакта).
  - `codeAuth` — дополнительно завершает процесс аутентификации и связывает проверку с пользователем (`UserId`).
- **Использования `verifyCode`**:
  - `game.Register.json`:
    - Подтверждение логина игрока через email‑код.
    - `identityKey: verification.UserName`, `codeKey: verification.Code`, `channel: "email"`.
  - `license.Auth.json`:
    - Проверка OTP‑кода портала лицензий.
    - `identityKey: auth.Email`, `codeKey: auth.OtpCode`, `channel: "email"`.
  - `license.register1.json`:
    - Верификация email при регистрации.
    - `identityKey: verification.Email`, `codeKey: verification.Code`, `channel: "email"`.
  - `shop.Register.json`:
    - Подтверждение телефона при регистрации.
    - `identityKey: verification.Phone`, `codeKey: verification.Code`, `channel: "phone"`.
- **Использования `codeAuth`**:
  - `game.auth.json`:
    - Вход в игру по email‑коду.
    - `identityKey: collectForm.UserName`, `codeKey: collectForm.Code`, `channel: "email"`, `resolveBy.field: "UserName"`.
  - `shop.auth.json`:
    - Вход в магазин по SMS‑коду.
    - `identityKey: auth.Phone`, `codeKey: auth.Code`, `channel: "phone"`, `resolveBy.field: "Phone"`.

---

### Степ `passwordAuth`

- **Назначение**: проверить логин/пароль и аутентифицировать пользователя.
- **Общие параметры**:
  - `selectorField` — по какому полю искать пользователя (например, `"Email"`).
  - `selectorKey` — путь до значения этого поля в форме (например, `auth.Email`).
  - `passwordKey` — путь до введённого пароля.
- **Использования**:
  - **`license.Auth.json`**
    - Проверка email+пароль перед OTP.
    - `selectorField: "Email"`, `selectorKey: "auth.Email"`, `passwordKey: "auth.Password"`.

---

### Степ `forgotPassword`

- **Назначение**: инициировать процесс восстановления пароля (как правило, отправка ссылки/кода с дальнейшим flow).
- **Использования**:
  - **`license.ForgotPassword.json`**
    - Канал: `email`.
    - `selectorKey: collectForm.Email`.

---

### Степ `getUser`

- **Назначение**: получить данные пользователя по указанному полю.
- **Общие параметры**:
  - `selectorField` — имя поля (например, `"Email"`).
  - `selectorKey` — путь до значения поля в форме (например, `get.Email`).
- **Использования**:
  - **`license.GetUser.json`**
    - Получение пользователя по email, введённому в форме `get`.

---

### Степ `token`

- **Назначение**: унифицированная операция выдачи токенов (access/refresh и вспомогательные поля) на основе разных способов аутентификации.
- **Общие параметры**:
  - `selectorKey` — где взять идентификатор (обычно email).
  - `passwordKey` (опционально) — пароль для проверки.
  - `codeKey` (опционально) — одноразовый код для входа по коду.
  - `channel` — способ аутентификации (например, `"email"`).
  - `resolveBy.field` — какое поле используется для поиска субъекта.
  - Имена выходных значений:
    - `accessTokenKey`, `refreshTokenKey`, `tokenTypeKey`, `expiresInKey`.
- **Использования**:
  - **`game.Token.json`**
    - Вход игрока по email+пароль.
    - `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `channel: "email"`.
  - **`license.Token.json`**
    - Универсальный вход по email + (пароль или код).
    - `selectorKey: collectForm.Email`, `passwordKey: collectForm.Password`, `codeKey: collectForm.Code`, `channel: "email"`, `resolveBy.field: "Email"`.
  - **`license.TokenByCode.json`**
    - Вход только по email+код.
    - `selectorKey: collectForm.Email`, `codeKey: collectForm.Code`, `channel: "email"`, `resolveBy.field: "Email"`.

---

### Степ `refreshToken`

- **Назначение**: обмен refresh_token на новую пару access/refresh токенов и дополнительные данные.
- **Общие параметры**:
  - `refreshTokenKey` — путь до строки refresh_token в контексте.
- **Использования**:
  - **`license.RefreshToken.json`**
    - `refreshTokenKey: "collectForm.RefreshToken"`.

---

### Степ `issueJwt`

- **Назначение**: выпустить JWT‑токен на основе уже аутентифицированного пользователя/сессии.
- **Общие параметры**:
  - `lifetimeSeconds` — время жизни токена в секундах.
  - `subKey` — откуда взять subject (`user.Id`, `codeAuth.UserId` и т.п.).
  - `claims` — дополнительный набор клеймов (scope, amr и др.).
- **Использования**:
  - **`game.auth.json`**
    - Игровой токен по email‑коду.
    - `lifetimeSeconds: 43200`, `subKey: codeAuth.UserId`, `claims.scope = "game.player"`, `claims.amr = "email_code"`.
  - **`game.Register.json`**
    - Игровой токен после регистрации.
    - `lifetimeSeconds: 604800`, `subKey: user.Id`, `scope: "game.player"`, `amr: "email_code"`.
  - **`license.Auth.json`**
    - Токен портала после пароля + OTP.
    - `lifetimeSeconds: 28800`, `subKey: user.Id`, `scope: "license.portal"`, `amr: "pwd+otp"`.
  - **`license.register1.json`**
    - Токен портала после регистрации по email‑коду.
    - `lifetimeSeconds: 43200`, `subKey: user.Id`, `scope: "license.portal"`, `amr: "email_code"`.
  - **`shop.auth.json`**
    - Токен магазина по SMS‑коду.
    - `lifetimeSeconds: 43200`, `subKey: user.Id`, `scope: "shop.customer"`, `amr: "sms_code"`.
  - **`shop.Register.json`**
    - Токен магазина после регистрации по SMS‑коду.
    - `lifetimeSeconds: 1209600`, `subKey: user.Id`, `scope: "shop.customer"`, `amr: "sms_code"`.

---

### Степ `refreshToken` + `collectResult` / `token` + `collectResult`

- **Назначение**: привести внутренний формат данных токенов/сессии к внешнему контракту API.
- **Общие принципы**:
  - `collectResult` не меняет бизнес‑данные, а только мапит имена полей.
  - Для токенов почти везде используется структура `access_token`, `refresh_token`, `token_type`, `expires_in`, иногда `user_id`.
- **Использования `collectResult`**:
  - **`edoctors.Register.json`**
    - Возвращает `LastCode` для подтверждения регистрации.
  - **`game.auth.json`**
    - Возвращает `token` и (логически) email пользователя.
  - **`game.Token.json`**, **`license.Token.json`**, **`license.TokenByCode.json`**
    - Мапят внутренние поля `token.*` в стандартный набор полей ответа.
  - **`license.RefreshToken.json`**
    - Мапит `refreshToken.*` в `access_token`, `refresh_token`, `token_type`, `expires_in`, `user_id`.
  - **`license.RequestCode.json`**
    - Возвращает `LastCode` для отладки/возможного повторного использования.
  - **`license.Register.json`**
    - Возвращает `LastCode` и `UserId` после регистрации.
