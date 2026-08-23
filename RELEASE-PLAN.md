Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

---

## Критично (безопасность)

### 1. OTP plaintext в логах
`CodeService.SendAsync` после отправки пишет в лог `msg.TextBody` — шаблон уже с подставленным `{{code}}`.

**Impact:** компрометация OTP через log aggregation / SIEM / support-доступ; обход лимита попыток.  
**Stock flows:** любой `sendCode` (Register, RequestCode, ForgotPassword).

### 2. ~~`TokenStep` (code-login) не совпадает с каналом `SendCodeStep`~~ ✅ закрыто
~~`SendCodeStep` / `VerifyCodeStep` → `ResolveOtpTargetAsync`; `ValidateCodeAsync` для Email/Phone проверял verification по типу selector.~~

**Исправлено (2.0):** `UserService.ValidateCodeAsync` всегда резолвит OTP-target через `ResolveOtpTargetAsync` (как SendCode/VerifyCode), независимо от selector (`Email` / `PhoneNumber` / `UserName`).

---

## Высокий (логика / auth model)

### 3. ~~`CodeService.VerifyAsync` не привязывает код к `userId`~~ ✅ закрыто
~~Поиск по email/phone без `UserAccountId == resolvedUserId` → cross-user OTP accept при дубликатах адреса.~~

**Исправлено (2.0):** `VerifyAsync(Guid userId, ChannelEnum, identity, code)`; lookup требует `UserAccountId == userId`. `VerifyCodeStep` передаёт resolved user id.

### 4. ~~Lookup Email/Phone: `FirstOrDefault` без приоритета confirmed~~ ✅ закрыто
~~`FindTrackedUserBySelectorAsync` / `GetUserByAsync` — `FirstOrDefault` без `OrderBy EmailConfirmed`.~~

**Исправлено (2.0):** `PreferConfirmedContact` — для Email/Phone `OrderByDescending(EmailConfirmed|PhoneNumberConfirmed)` перед `FirstOrDefault`.

### 5. ~~Microsoft OAuth: `EmailConfirmed` без attestation~~ ✅ закрыто
~~`FetchMicrosoftProfileAsync`: `EmailConfirmed = !string.IsNullOrWhiteSpace(email)`.~~

**Исправлено (2.0):** Graph `/me` только для id/name (+ fallback email); `ExternalOAuthProfile.EmailConfirmed` только из OIDC `email_verified` на `https://graph.microsoft.com/oidc/userinfo` (как Google).

### 6. ~~OTP/notify на неподтверждённый email~~ ✅ закрыто
~~`FindEmailTargetAsync` fallback на `UsersAccounts.Email` без `EmailConfirmed` для всех resolve.~~

**Исправлено (2.0):** разделены пути:
- **`ResolveOtpTargetAsync`** — fallback на account email **разрешён и без** `EmailConfirmed` (иначе нельзя подтвердить только что добавленный email).
- **`ResolveDeliveryTargetAsync`** (notify, напр. после reset password) — fallback **только** при `EmailConfirmed`.

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
OTP: `Authentication:LockChannelAsEmail` → preferred verified → account email (**в т.ч. unconfirmed** — подтверждение email). Notify (`ResolveDeliveryTargetAsync`): тот же порядок, но account email **только** при `EmailConfirmed` (#6). Stock JSON больше не задаёт `channel` на send/verify/reset steps. Selector field (Email vs Phone) **не** определяет канал доставки — только identity lookup (#2).

### Публичные half-validate API (#13, #14)
Контракт для второго шага после crypto (JwtBearer / `ValidateAccessTokenAsync`), не для standalone auth.

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
2. **H3–H7:** ~~bind `VerifyAsync` к userId~~; ~~lookup prefer confirmed~~; ~~Microsoft `EmailConfirmed`~~; ~~email fallback только confirmed (notify) / OTP allow unconfirmed~~; lockout на code path.
3. **H8–H12:** enumeration; messenger→SMS mapping; phone fallback; rate limits; Apple guard.
4. **M13–M19:** API docs / SecurityStamp claim; SHA256 migration; ChangePassword session proof.
