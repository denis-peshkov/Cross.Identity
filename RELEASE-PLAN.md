Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Release/docs/категории тестов не трогаю.

---

## Критично (безопасность)

### 1. ~~OAuth takeover по email~~ ✅ закрыто
~~`ExternalLoginService.ResolveOrCreateUserAsync`: при логине без `UserId` (link) ищется аккаунт по `profile.Email` и сразу привязывается provider. **Нет проверки**, что email у жертвы подтверждён или что он принадлежит OAuth-субъекту.~~

~~При регистрации жертвы с `victim@corp.com` (email не подтверждён) атакующий логинится через OAuth с тем же email → попадает в чужой аккаунт. Плюс `EmailConfirmed = true` при создании через OAuth.~~

**Исправлено (2.0):** auto-link по email только при `profile.EmailVerified` к **подтверждённому** локальному аккаунту. Уникальность email и phone в БД — только среди `EmailConfirmed` / `PhoneNumberConfirmed = true`; неподтверждённые регистрации не блокируют владельца.

### 2. ~~Account linking без аутентификации~~ ✅ закрыто
~~`main.ExternalLogin.json`: опциональный `UserId` в bag → `ExternalLoginInitiateStep` → state. **Библиотека не проверяет**, что вызывающий — этот пользователь.~~

~~Атакующий передаёт `UserId` жертвы, проходит OAuth своим аккаунтом → provider привязан к чужому аккаунту.~~

**Исправлено (2.0):** при linking обязателен `RefreshToken` того же пользователя (`requiredIf` в `main.ExternalLogin.json`, `EnsureLinkRefreshTokenAsync` в `ExternalLoginService`). См. `docs/BREAKING.md` → From 1.10.x to 2.0.0.

### 3. ~~IDOR на flows с `UserId` из bag~~ ✅ закрыто
~~То же для:~~
~~- `ExternalLoginUnlink` / `ExternalLoginGetAll`~~
~~- `CommunicationEndpointsGetAll` / `CommunicationEndpointSetPreferred`~~

~~Библиотека **не делает authz** — только доверяет `UserId` из input. Без проверки bearer на хосте — отвязка OAuth, чтение/смена endpoints чужого пользователя.~~

**Исправлено (2.0):** на всех перечисленных flows обязателен `RefreshToken`, принадлежащий `UserId` (`EnsureRefreshTokenBelongsToUserAsync` в `IJwtTokenService`). См. `docs/BREAKING.md` → From 1.10.x to 2.0.0.

### 4. ~~Brute-force OTP в `CodeService.VerifyAsync`~~ ✅ закрыто
~~Поиск: `Email == identity && TokenHash == codeHash`. При неверном коде строка **не находится** → `Attempts` **не растёт** → `MaxAttempts` не работает.~~

~~`ResetPassword` / `VerifyCodeStep` идут через `CodeService`. Для 6-значного SMS — неограниченный перебор.~~

**Исправлено (2.0):** поиск по email/phone (последняя активная запись); `Attempts++` только при совпадении identity и неверном коде; чужой identity не трогает счётчик. Логика как в `UserService.TryValidate*CodeAsync`.

### 5. ~~Logout не убивает access token~~ ✅ закрыто
~~`RevokeRefreshTokenForLogoutAsync` — revoke **только refresh**. Access tokens той же сессии живут до `ExpiresAt`.~~

~~`LogoutAll` вызывает `RevokeAllTokensForUserAsync` и revoke access — **несимметрично**: single logout слабее, чем ожидает пользователь.~~

**Исправлено (2.0):** `RevokeRefreshTokenForLogoutAsync` отзывает access tokens той же `FamilyId` с `USER_LOGOUT`; другие сессии пользователя не затрагиваются.

### 6. ~~`System.Random` для OTP~~ ✅ закрыто
~~`CodeGeneratorHelper.GenerateNumericCode` / `GenerateCode` — не CSPRNG. Для коротких SMS-кодов это слабое место (особенно при массовой выдаче).~~

**Исправлено (2.0):** OTP генерируется через `RandomNumberGenerator.GetInt32` в `CodeGeneratorHelper`.

### 7. ~~GitHub: email без `verified`~~ ✅ закрыто (вместе с #1)
~~`FetchGitHubProfileAsync` берёт primary email из `/user/emails` **без** проверки `verified: true`. Усиливает takeover по email (#1).~~

**Исправлено (2.0):** GitHub profile использует только `verified: true` emails (primary verified, иначе первый verified).

---

## Высокий (логика / auth model)

### 8. ~~`IsActive` не проверяется~~ ✅ закрыто
~~Поле есть в `UserAccountEntity`, выставляется при создании, но **нигде не читается** в `ValidatePasswordAsync`, `ValidateCodeAsync`, OAuth, `TokenPairIssuer`. Деактивированный пользователь продолжает логиниться.~~

**Исправлено (2.0):** `UserAccountGuard` + проверки в `UserService`, `CodeService`, `TokenPairIssuer`, `ExternalLoginService.CompleteAsync`, `JwtTokenService` (validate access/refresh и rotation).

### 9. ~~Lockout не реализован~~ ✅ закрыто
~~`LockoutEnd`, `LockoutEnabled`, `AccessFailedCount` в entity — **мертвые колонки**. Брутфорс пароля не ограничен на уровне библиотеки.~~

**Исправлено (2.0):** `ValidatePasswordAsync` — проверка lockout, `AccessFailedCount++` при неверном пароле, lockout по `Authentication:Lockout` (`MaxFailedAccessAttempts`, `LockoutTimeout`); сброс при успешном входе и `SetPasswordAsync`.

### 10. ~~Два несовместимых пути верификации OTP~~ ✅ закрыто (вместе с #4)
~~| Путь | Где | Поведение |~~
~~| `UserService.ValidateCodeAsync` | `TokenStep` | По `userId`, attempts работают |~~
~~| `CodeService.VerifyAsync` | `VerifyCodeStep`, reset | По identity+hash в WHERE, attempts сломаны |~~

**Исправлено (2.0):** `CodeService.VerifyAsync` — поиск по identity, `Attempts++` при неверном коде; логика согласована с `UserService`.

### 11. ~~Старые OTP остаются валидными~~ ✅ закрыто
~~`CodeService.SendAsync` **добавляет** новую запись, старые не инвалидирует.~~

**Исправлено (2.0):** при resend активные коды для того же email/phone истекают (`ExpiresAt = now`); verify принимает только последнюю неиспользованную непросроченную запись. TTL по-прежнему ограничивает срок жизни каждого кода.

### 12. ~~Refresh rotation без атомарности~~ ✅ принято (контракт хоста)
~~`RefreshTokenStep`: check → issue → invalidate. Между шагами нет транзакции/блокировки внутри библиотеки. Параллельные refresh с одним token — окно с несколькими активными парами; при replay — mass revoke family (DoS сессий).~~

**Принято (2.0):** библиотека **не** открывает DB-транзакцию в `RefreshTokenStep`; хост оборачивает refresh (тот же scoped `IdentityContext`) во **внешнюю транзакцию**, чтобы validate + issue + invalidate коммитились вместе. Документация: `FLOWS.md` → `main.RefreshToken.json`, XML на `RefreshTokenStep`.

**Остаётся осознанный edge:** два **параллельных** refresh с одним token — race до commit (две активные пары или `REPLAY_DETECTED` + revoke family). Это не закрывается одной транзакцией на запрос; нужен pessimistic lock / сериализация rotation — вне scope stock-библиотеки. Replay detection (`REPLAY_DETECTED`) — намеренный trade-off против theft race.

### 13. ~~`ClientContext` полностью client-controlled~~ ✅ принято (контракт хоста)
~~`IpAddress`, `UserAgent`, `DeviceFingerprint` из bag/form. Audit, revoke metadata, письма (`ResetPasswordStep`) — **подделываемы** вызывающим кодом. Это не баг контракта 2.0, но forensic/notification integrity слабая.~~

**Принято (2.0):** **trusted pipeline** — ответственность **хоста**; библиотека не читает `HttpContext` и считает `ClientContext` / `collectForm.*` уже доверенными. Хост перезаписывает `IpAddress` / `UserAgent` / `DeviceFingerprint` из server-side metadata перед `ExecuteAsync`. Документация: `FLOWS.md` → Client context (host), `README.md`, `BREAKING.md`, XML на `ClientContext`.

### 13b. ~~Session binding на refresh не реализован~~ ✅ закрыто
~~Enum `DEVICE_MISMATCH` / `IP_MISMATCH` / `USER_AGENT_MISMATCH` / `TOKEN_STOLEN` без сравнения при rotation.~~

**Исправлено (2.0):** колонки `Created*` на `RefreshTokens`; при refresh сравнение `ClientContext` (host-supplied metadata из trusted pipeline) с family anchor. `LastActivityAt` + `Authentication:Jwt:RefreshTokenIdleTimeout` — idle-check при refresh (`SESSION_EXPIRED`). PreDeployment `1_04` / `1_05` (3 провайдера).

---

## Средний (противоречия / баги контрактов)

### 14. ~~UserName + Code не работает~~ ✅ закрыто
~~`main.Token.json` / `main.ResetPassword.json` разрешают `UserName` в selector, но:~~
~~- `ValidateCodeAsync` — только Email/PhoneNumber~~
~~- `VerifyCodeStep` для UserName → `ChannelForField` = null → fallback `email` → verify по email == username~~

**Исправлено (2.0):** `ValidateCodeAsync` для `UserName` резолвит OTP-канал через `CommunicationEndpoints` и проверяет код по `userId`. `VerifyCodeStep` для `UserName` делегирует в `ValidateCodeAsync`; `SendCodeStep` шлёт на preferred verified endpoint.

### 15. ~~Лимит пароля: Register 128, Token 32~~ ✅ закрыто
~~`main.Register.json`: password `max: 128`. `main.Token.json`: `max: 32`. Пользователь с длинным паролем **не войдёт** через login flow.~~

**Исправлено (2.0):** `max: 32` для всех полей `type: Password` в stock flows (`Register`, `Token`, `ResetPassword`, `ChangePassword`).

### 16. ~~`TokenStep` при неверных credentials → `Ok`~~ ✅ закрыто
~~`IsInvalidCode=true`, `StepResult.Ok` (есть `// todo`). `PasswordAuthStep` бросает `NotAuthorizedException`. Разная семантика; хост без проверки флага может считать login успешным.~~

**Исправлено (2.0):** `TokenStep` → `StepResult.Fail(NotAuthorizedException)`; `is_invalid_code` убран из `main.Token` `collectResult`.

### 17. ~~User enumeration в `SendCodeStep`~~ ✅ закрыто
~~`GetUserIdByAsync` → `NotFoundException("User not found.")` на ForgotPassword/RequestCode. Перебор email/phone.~~

**Исправлено (2.0):** неизвестный identity → `StepResult.Fail(NotAuthorizedException("Invalid credentials."))` без отправки кода; деталь (`NotFoundException` message и т.п.) — `LogInformation` в `SendCodeStep`.

### 18. ~~Messenger-каналы в `SendCode`~~ ✅ закрыто
~~`ChannelEnum.Telegram/Viber/WhatsApp` в `SendAsync` → `default: break` — **молча** ничего не отправляет и не падает. TO-DO в продукте, в коде — тихий no-op.~~

**Исправлено (2.0):** `CodeService.SendAsync` → `NotSupportedException` для каналов без OTP (`SupportsOtp` / messenger). Доставка OTP через `ResolveOtpTargetAsync` (messenger → SMS).

### 19. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.

### 20. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра.

### 21. ~~`VerifyTokenStep` глотает исключения~~ ✅ закрыто
~~Любая ошибка (DB, decrypt) → `valid: false`. Operational failure неотличим от invalid token.~~

**Исправлено (2.0):** только `SecurityTokenException` / `ArgumentException` / `FormatException` → `Valid = false`, `StepResult.Ok`. Остальные ошибки (DB, конфиг и т.д.) не маскируются как invalid token — пробрасываются через `StepResult.Fail` / исключение executor.

### 22. ~~Sync-over-async~~ ✅ принято (осознанный trade-off)
~~`GetClaimValueFromJweToken` → `ValidateTokenAsync(...).GetAwaiter().GetResult()`. Теоретический deadlock при «липком» `SynchronizationContext` (legacy ASP.NET, UI).~~

**Принято (2.0):** sync `GetClaimValue` для JWE оставляем — отдельный async API не добавляем. В типичном хосте (**ASP.NET Core**, без request-scoped `SynchronizationContext`) deadlock практически не воспроизводится. Основной сценарий — JWS refresh (`RefreshTokenStep`, 3 части) без async I/O; JWE access в `VerifyTokenStep` уже проходит `ValidateAccessTokenAsync` до `GetClaimValue`. Риск — блокировка worker thread на decrypt при редком sync-вызове JWE; для stock flows допустимо.

### 23. `PasswordAlgoEnum.SHA256`
Obsolete, но всё ещё в коде — слабый алгоритм при старой конфигурации.

### 24. `TwoFactorEnabled` — мёртвое поле
В entity, в auth pipeline не используется.

---

## Низкий (техдолг / несогласованности)

- ~~Мёртвые ключи в `main.Token.json` (`accessTokenKey`, …)~~ ✅ удалены (`channel` тоже — factory не использовал).
- Закомментированный `IJwtIssuer` в `IJwtTokenService.cs`.
- ~~`CreatedBy` не заполняется при register~~ ✅ удалено (колонка убрана из entity/DDL; PreDeployment `1_06`).
- `AuditService.Record` без `SaveChanges` — audit теряется при ошибке между revoke и commit.
- `DeveloperMode` → `LastCode` в bag (утечка OTP в dev, если включить в prod).
- `ReturnUrl` в OAuth state без allowlist — open redirect на стороне хоста, если тот редиректит вслепую.

---

## Что в библиотеке уже нормально

- Crypto `ValidateAccessTokenAsync` (signature + DB `jti`).
- Refresh replay → family revoke (`REPLAY_DETECTED`).
- OAuth state one-time use + expiry.
- Refresh token hash в DB, не plain text.
- E.164 gate на `collectForm` PhoneNumber.
- `TokenPairIssuer` централизует выдачу пары.

---

## Приоритет фиксов (если чинить)

1. **S1–S4:** OTP attempts + `RandomNumberGenerator` + OAuth email policy + logout access revoke *(account linking — ✅)*
2. **S5–S7:** `IsActive`, единый OTP verify, инвалидация старых кодов
3. **Контракты:** UserName в flows, password max, `TokenStep` fail vs ok
4. **Архитектура:** concurrent refresh lock (опционально), 2FA *(refresh transaction — ✅ контракт хоста; lockout — ✅)*

Могу начать с п.1 (OTP + OAuth) — это самый острый блок.
