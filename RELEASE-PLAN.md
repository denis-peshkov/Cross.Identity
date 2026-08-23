Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

---

## Средний (противоречия / баги контрактов)

### 13. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.

### 14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается.

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
OTP: `Authentication:LockChannelAsEmail` → preferred verified → account email → account phone (unconfirmed allowed for OTP confirm). Notify: тот же порядок, email/phone только confirmed. Stock JSON больше не задаёт `channel` на send/verify/reset steps. Selector field (Email vs Phone) **не** определяет канал доставки — только identity lookup.

### Публичные half-validate API (#13, #14)
Контракт для второго шага после crypto (JwtBearer / `ValidateAccessTokenAsync`), не для standalone auth.

### OTP plaintext в логах (#1) — принято
`CodeService.SendAsync` логирует `TextBody` с подставленным кодом. Хост обязан не утекать логи / SIEM; в prod не включать verbose notifier logs.

### Messenger preferred → SMS (#9) — принято
`ToEmailOrSms()` меняет только канал; address остаётся как в endpoint. Хост не ставит preferred messenger с chat-id, пока нет messenger sender / remapping на E.164.

### Apple в registry без реализации (#12) — принято
`FetchAppleProfileAsync` → `NotSupportedException`. Не включать Apple в `Providers` / options до реализации.

### `main.GetUserId` existence oracle — принято
Успех возвращает `{ user_id }` и раскрывает существование пользователя. Продуктовое решение; шаги SendCode/VerifyCode/GetUserId на reject дают единый `Invalid credentials.`.

### `PasswordAlgoEnum.SHA256` (#16) — принято
Obsolete; pepper в `HashSha256`/`VerifySha256` игнорируется; нет auto-upgrade на Argon2id. Default path — Argon2id/PBKDF2 + pepper. SHA256 только для явного legacy; хост не должен включать его для новых хешей.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| OAuth takeover по email | auto-link только при `profile.EmailConfirmed` + local confirmed |
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
| #2 TokenStep ↔ SendCode channel | `ValidateCodeAsync` → `ResolveOtpTargetAsync` |
| #3 VerifyAsync без userId | `VerifyAsync(userId, …)` + `UserAccountId` match |
| #4 Lookup без PreferConfirmed | `OrderByDescending` confirmed перед FirstOrDefault |
| #5 Microsoft EmailConfirmed | только OIDC `email_verified` (userinfo) |
| #6 OTP vs notify email | OTP: unconfirmed OK; notify: confirmed only |
| #7 Lockout на OTP-login | `ValidateCodeAsync` lockout как у password |
| #8 Enumeration на шагах | единый `Invalid credentials.` + log |
| #10 Phone fallback | account phone после email (OTP/notify rules) |
| #11 OTP send rate limit | `Authentication:OtpSendRateLimit` в `CodeService.SendAsync` |
| #15 SecurityStamp в JWT | claim `security_stamp` + check в ValidateAccess/Refresh |

---

## Что в библиотеке уже нормально

- Crypto `ValidateAccessTokenAsync` (signature + DB `jti` + `security_stamp`).
- Refresh replay → family revoke (`REPLAY_DETECTED`).
- OAuth state one-time use + expiry.
- Refresh token hash в DB, не plain text.
- External login link/unlink требует refresh token.
- Unlink last method without password blocked.
- Password change revokes all tokens + `SecurityStamp` rotation (stamp also in JWT; mismatch fails validate).
- E.164 gate на `collectForm` PhoneNumber.
- `TokenPairIssuer` централизует выдачу пары.
- SendCode ↔ VerifyCode channel resolution (`ResolveOtpTargetAsync`) согласованы между собой.
- Argon2id/PBKDF2 password hashing + pepper (не SHA256).
- OTP hash storage, max 3 attempts, supersede on resend.

---

## Приоритет фиксов

1. **M13–M14, M17–M19:** half-validate API docs; ChangePassword session proof; OAuth ReturnUrl; OTP trim.
