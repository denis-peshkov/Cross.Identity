Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

---

## Критично (безопасность)

### 1. OTP plaintext в логах
`CodeService.SendAsync` после отправки пишет в лог `msg.TextBody` — шаблон уже с подставленным `{{code}}`.

**Impact:** компрометация OTP через log aggregation / SIEM / support-доступ; обход лимита попыток.  
**Stock flows:** любой `sendCode` (Register, RequestCode, ForgotPassword).

### 2. `TokenStep` (code-login) не совпадает с каналом `SendCodeStep`
- `SendCodeStep` / `VerifyCodeStep` → `ResolveOtpTargetAsync(userId)` (preferred → email fallback).
- `TokenStep` → `UserService.ValidateCodeAsync`: для selector `Email` / `PhoneNumber` проверяет `TryValidateEmailCodeAsync` / `TryValidatePhoneCodeAsync` **по типу selector**, не по фактическому OTP-target.

**Impact:** code-login в `main.Token` ломается, когда preferred channel ≠ selector (login по Email, OTP ушёл на preferred phone).  
**Stock flows:** `main.Token.json` (ветка Code).

---

## Высокий (логика / auth model)

### 3. `CodeService.VerifyAsync` не привязывает код к `userId`
Поиск последней активной записи по email/phone **без** `entity.UserAccountId == resolvedUserId`. При нескольких unconfirmed аккаунтах с одним адресом (unique index только на confirmed) возможен cross-user accept.

**Impact:** IDOR на уровне OTP.  
**Stock flows:** VerifyCode, ResetPassword (verifyCode), потенциально Token после fix #2.

### 4. Lookup Email/Phone: `FirstOrDefault` без приоритета confirmed
`FindTrackedUserBySelectorAsync` / `GetUserByAsync` — `FirstOrDefault` без `OrderBy EmailConfirmed`. Уникальность email/phone только среди confirmed; несколько unconfirmed + один confirmed допустимы.

**Impact:** login / ForgotPassword / OTP / Reset могут попасть на squat-аккаунт с тем же контактом; усиливает #3.  
**Stock flows:** Token, ForgotPassword, RequestCode, ResetPassword, Register + OAuth create.

### 5. Microsoft OAuth: `EmailVerified` без attestation
`FetchMicrosoftProfileAsync`: `EmailVerified = !string.IsNullOrWhiteSpace(email)`. Google/GitHub проверяют флаг; Microsoft — нет.

**Impact:** auto-link к confirmed локальному аккаунту по UPN/mail без подтверждённого mailbox → account takeover.  
**Stock flows:** ExternalLogin sign-in / auto-link.

### 6. OTP на неподтверждённый email
`CommunicationEndpointService.FindEmailTargetAsync` fallback на `UsersAccounts.Email` **без** `EmailConfirmed`.

**Impact:** OTP/notify на адрес, который пользователь не подтвердил.  
**Stock flows:** SendCode, VerifyCode, ResetPassword notify (при `LockChannelAsEmail` или email-fallback).

### 7. Lockout обходится OTP-логином
`ValidatePasswordAsync` проверяет lockout; `ValidateCodeAsync` — **нет**. После lockout по паролю вход по коду всё ещё возможен.

**Impact:** неполная lockout policy.  
**Stock flows:** Token (code branch), flows с verifyCode.

### 8. User enumeration — разные ответы шагов
| Шаг | Поведение |
|-----|-----------|
| `SendCodeStep` | unknown user → `Invalid credentials.` |
| `SendCodeStep` | known user без channel → `ValidationException` (другой текст) |
| `VerifyCodeStep` / `GetUserIdStep` | `NotFoundException` / «User not found» |
| `main.GetUserId` | явный oracle существования |

**Stock flows:** ForgotPassword, RequestCode, ResetPassword, GetUserId.

### 9. Messenger preferred → SMS с тем же `Address`
`ResolveOtpTargetAsync` → `ToEmailOrSms()`: Telegram/Viber/WhatsApp мапится в `Sms`, address не переписывается на E.164 phone.

**Impact:** OTP не доходит или verify по chat-id.  
**Stock flows:** если хост сделал preferred messenger endpoint.

### 10. Нет fallback на confirmed phone в `ResolveDeliveryTargetAsync`
Цепочка: `LockChannelAsEmail` → preferred verified → email. Нет fallback на `UsersAccounts.PhoneNumber` при `PhoneNumberConfirmed`, если н нет строки в `UsersCommunicationEndpoints`.

**Impact:** ValidationException для phone-only пользователей без synced endpoints.  
**Stock flows:** SendCode, ResetPassword notify.

### 11. Нет rate limiting на отправку OTP
`CodeService` / `SendCodeStep` — нет cooldown / per-identity / per-IP limits в библиотеке.

**Impact:** spam, cost abuse (SMS/email), DoS на identity.  
**Stock flows:** SendCode, ForgotPassword, RequestCode, Register.

### 12. Apple provider в registry, но не реализован
`FetchAppleProfileAsync` → `NotSupportedException`. Initiate строит URL; Complete падает.

**Impact:** broken auth surface, если провайдер enabled в БД.

---

## Средний (противоречия / баги контрактов)

### 13. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.

### 14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается.

### 15. `SecurityStamp` не участвует в валидации JWT
Stamp ротируется при password change / OAuth unlink, но `ValidateAccessTokenAsync` проверяет только DB revoke list + crypto. Defense-in-depth gap.

### 16. `PasswordAlgoEnum.SHA256`
Obsolete; pepper в `HashSha256`/`VerifySha256` **игнорируется**; `NeedsRehashSha256` → false (нет upgrade на Argon2id). Риск при legacy/конфиге.

### 17. `ChangePassword` по `Id` без session proof
Достаточно Guid + current password; refresh token не требуется. При утечке UUID + password — смена пароля (entropy UUID mitigates).

### 18. OAuth `ReturnUrl` без allowlist
Произвольный `ReturnUrl` в state. Open redirect на стороне хоста, если тот редиректит вслепую.

### 19. `CodeService.VerifyAsync` — code без trim
`UserService.ValidateCodeAsync` делает `code.Trim()`; `CodeService.VerifyAsync` — нет. Minor mismatch на whitespace.

---

## Низкий (техдолг / несогласованности)

- `TwoFactorEnabled` — мёртвое поле в entity, в auth pipeline не используется.
- `DeveloperMode` → `LastCode` в bag + skip send (`Authentication:DeveloperMode`). Утечка OTP через API response, если включить в prod.
- `AuditService.Record` без собственного `SaveChanges` — audit теряется, если caller не закоммитит.
- Закомментированный `IJwtIssuer` в `IJwtTokenService.cs`.
- Закомментированные legacy-поля в `UserAccountEntity` (`PasswordSalt`, `PasswordHash`, …).
- Inconsistent code trimming (#19).

---

## Принято (осознанный trade-off / контракт хоста)

### Refresh rotation без атомарности
`RefreshTokenStep`: validate → issue → invalidate без DB-транзакции внутри библиотеки. Хост оборачивает refresh во **внешнюю транзакцию** (`FLOWS.md`).

**Edge:** параллельные refresh с одним token — race до commit; `REPLAY_DETECTED` + family revoke — намеренный trade-off.

### Sync-over-async в `GetClaimValue` (JWE)
`GetClaimValueFromJweToken` → `GetAwaiter().GetResult()`. В ASP.NET Core deadlock маловероятен; основной путь — JWS refresh без async I/O.

### `ClientContext` — trusted pipeline хоста
`IpAddress` / `UserAgent` / `DeviceFingerprint` из bag/form. Библиотека не читает `HttpContext`; хост перезаписывает из server-side metadata.

### Delivery channel resolution (новая модель)
OTP/notify: `Authentication:LockChannelAsEmail` → preferred verified endpoint → email fallback. Stock JSON больше не задаёт `channel` на send/verify/reset steps. Selector field (Email vs Phone) **не** определяет канал доставки — только identity lookup (#2, #6).

### Публичные half-validate API (#13, #14)
Контракт для второго шага после crypto (JwtBearer / `ValidateAccessTokenAsync`), не для standalone auth.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| OAuth takeover по email | auto-link только при `EmailVerified` + local confirmed |
| Account linking без auth | linking требует `RefreshToken` того же user |
| IDOR на flows с `UserId` | `EnsureRefreshTokenBelongsToUserAsync` на endpoints/OAuth unlink/getAll |
| OTP attempts в `CodeService` | поиск по identity; `Attempts++` при неверном коде |
| Logout access revoke | `RevokeRefreshTokenForLogoutAsync` отзывает access той же `FamilyId` |
| CSPRNG для OTP | `RandomNumberGenerator.GetInt32` в `CodeGeneratorHelper` |
| GitHub email verified | только `verified: true` emails |
| `IsActive` | `UserAccountGuard` + checks в auth pipeline |
| Lockout пароля | `ValidatePasswordAsync` + `Authentication:Lockout` |
| Старые OTP при resend | supersede active codes (`ExpiresAt = now`) |
| Session binding на refresh | `Created*` на refresh + compare `ClientContext` |
| Refresh idle timeout | `LastActivityAt` + `RefreshTokenIdleTimeout` |
| UserName + Code | `ResolveOtpTargetAsync` + unified verify path |
| Password max 32 | все stock Password fields |
| `TokenStep` fail vs ok | `NotAuthorizedException` на bad credentials |
| SendCode enumeration (unknown) | `Invalid credentials.` без отправки |
| Messenger SendCode no-op | `NotSupportedException` |
| `VerifyTokenStep` swallow | только token-format errors → `Valid=false` |
| Мёртвые ключи `main.Token.json` | удалены |
| `CreatedBy` | колонка удалена |

---

## Что в библиотеке уже нормально

- Crypto `ValidateAccessTokenAsync` (signature + DB `jti`).
- Refresh replay → family revoke (`REPLAY_DETECTED`).
- OAuth state one-time use + expiry.
- Refresh token hash в DB, не plain text.
- External login link/unlink требует refresh token.
- Unlink last method without password blocked.
- Password change revokes all tokens + `SecurityStamp` rotation.
- E.164 gate на `collectForm` PhoneNumber.
- `TokenPairIssuer` централизует выдачу пары.
- SendCode ↔ VerifyCode channel resolution (`ResolveOtpTargetAsync`) согласованы между собой.
- Argon2id/PBKDF2 password hashing + pepper (не SHA256).
- OTP hash storage, max 3 attempts, supersede on resend.

---

## Приоритет фиксов

1. **C1–C2:** убрать OTP из логов; выровнять `TokenStep` code-verify с `ResolveOtpTargetAsync`.
2. **H3–H7:** bind `VerifyAsync` к userId; lookup prefer confirmed; Microsoft `EmailVerified`; email fallback только confirmed; lockout на code path.
3. **H8–H12:** enumeration; messenger→SMS mapping; phone fallback; rate limits; Apple guard.
4. **M13–M19:** API docs / SecurityStamp claim; SHA256 migration; ChangePassword session proof.
