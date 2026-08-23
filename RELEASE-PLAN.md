Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

**CodeRabbit (local CLI, 2026-08-23):** `coderabbit review --committed --base origin/dev --dir Cross.Identity` — Free plan limit 150 files; полный diff 293 → scope только `Cross.Identity/` (40 findings: 22 major / 18 minor). Сырой лог: `/tmp/cr-identity.jsonl`.

---

## Средний (противоречия / баги контрактов)

### 13. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.

### 14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается.

### 20. PII в логах SendCode / GetUserId / ValidateCode (CR)
`SendCodeStep`, `GetUserIdStep`, `UserService` — в Information/Warning пишется raw `selector.Value` (email/phone). Маскировать или логировать `userId`.

### 22. Confirm contact по selector field, не OTP-каналу (CR)
`UserService.ValidateCodeAsync` — после успеха confirm идёт по `field` (Email/Phone selector), а не по `otpTarget.Channel` — можно подтвердить не тот контакт.

### 23. OTP supersede без `userId` (CR)
`CodeService` — supersede active codes фильтрует только по email/phone; добавить `UserAccountId` в predicate.

### 24. SMS destination `ToLowerInvariant` (CR)
`CodeService.SendAsync` — lowercasing destination для всех каналов; Verify для SMS только `Trim` — риск mismatch.

### 25. Lockout: счётчик после истечения окна (CR)
`UserAccountLockout.RecordFailedAccess` — не сбрасывает `AccessFailedCount`, когда `LockoutEnd` уже истёк, перед новым fail.

### 26. `AuditEntityType` discriminator collision (CR)
`LinkedMessenger` / `UserCommunicationEndpoint` / `ExternalLoginState` — одинаковые numeric values.

### 27. Preferred endpoint unique index (CR)
`UserCommunicationEndpointEntityConfiguration` — нет filtered unique index: один `IsPreferred` на user.

### 28. SendCode action URL всегда `/reset-password` (CR)
`SendCodeStep.BuildActionUrl` — path не зависит от `Template` (verify/register vs reset).

### 29. OAuth unverified email collision (CR)
`ExternalLoginService` — даже при `profile.EmailVerified` отвергать merge, если локальный email уже есть как unverified row.

### 30. `ICodeService.SendAsync` `userId: string` (CR)
`SendAsync(string userId)` vs `VerifyAsync(Guid)` — выровнять на `Guid`.

### 31. `GetUserIdByAsync` nullability (CR)
Документ: null-on-missing; сигнатура `Task<string>` без `?`.

### 32. `UserAccountGuard` → `InvalidOperationException` (CR)
Conflict на email/phone бросает `InvalidOperationException` вместо Conflict/Validation.

### 33. `Bag` nullable `Convert.ChangeType` (CR)
`Bag.Get` / `TryGet` — для `T?` value-types передавать underlying type в `Convert.ChangeType`.

### 34. Session IP binding не конфигурируется (CR)
`JwtTokenService` — IP-only mismatch как opt-in (NAT/mobile), не всегда hard-fail.

### 35. `WatsApp` obsolete alias (CR)
`ChannelEnum` — вернуть obsolete `WatsApp` для source/serialization compat (сейчас breaking rename в 2.0).

### 36. Password max 32 → 128 (CR) — конфликт с закрытым
`main.Token.json` / `ChangePassword` — CR хочет max 128; **уже закрыто** как Password max 32.

### 37. ClientContext fields в ExternalLogin form (CR) — конфликт с принятым
`main.ExternalLogin.json` — CR: убрать Ip/UA/Fingerprint из form; **принято** как host collectForm → `ClientContext`.

### 38. `AuditEntity` хранит Ip/UA/Fingerprint (CR) — спорно
CR: минимизировать PII; сейчас намеренно для revoke forensics (`Audits`).

### 39. Idle revoke double-audit? (CR)
`HandleRefreshTokenIdleExpiredAsync` — presented token может аудититься/ревокаться дважды при family revoke.

### 40. Выбор типа канала коммуникации (из TO-DO)
Каркас есть (`ChannelEnum`, endpoints, resolve через preferred). Нет отдельного user-facing flow «выбери тип канала»; OTP реально только Email/Sms (`SupportsOtp`). Нужен явный контракт/flow выбора канала (или документировать, что канал = тип preferred endpoint).

### 41. Мессенджер + верификация / бот (из TO-DO)
`Telegram` / `Viber` / `WhatsApp` + `LinkedMessenger` и preferred уже в модели. Нет: sender в мессенджер, flow привязки/верификации через бота. Сейчас messenger preferred → SMS (`ToEmailOrSms`) / SendCode → `NotSupportedException` (см. принятое #9).

---

## Низкий (техдолг / несогласованности)

- `TwoFactorEnabled` — мёртвое поле в entity, в auth pipeline не используется.
- `DeveloperMode` → `LastCode` в bag + skip send (`Authentication:DeveloperMode`). Утечка OTP через API response, если включить в prod.
- `AuditService.Record` без собственного `SaveChanges` — audit теряется, если caller не закоммитит.
- Закомментированный `IJwtIssuer` в `IJwtTokenService.cs`.
- Закомментированные legacy-поля в `UserAccountEntity` (`PasswordSalt`, `PasswordHash`, …).

### CodeRabbit minor (XML / style / hygiene)
- XML docs: `NotificationMessage` (`param`→`summary`), `CodeGeneratorHelper.GenerateHash`, `ChannelEnumExtensions`, `IdentityContext` DbSets, `UserExternalLoginEntity`, `UserAccount.CommunicationEndpoints`, endpoint/phone/refresh/state entities, `Configure`, `IJwtTokenService` ClientContext wording («Optional» → sentinel).
- `JsonHelpers`: после `Enum.TryParse` требовать `Enum.IsDefined`.
- `PhoneE164`: `_pattern`/`_util`; braces; catch только `NumberParseException`.
- `ChannelEnumExtensions.PhoneChannels` — сделать `private` (mutation).
- `UserService.CreateUserAsync`: PhoneNumber через `ToString()` как Email/UserName.
- `JwtTokenService` idle path: не дублировать audit/revoke presented token (#39 related).

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
OTP: `Authentication:LockChannelAsEmail` → preferred verified → account email → account phone (unverified allowed for OTP confirm). Notify: тот же порядок, email/phone только verified. Stock JSON больше не задаёт `channel` на send/verify/reset steps. Selector field (Email vs Phone) **не** определяет канал доставки — только identity lookup.

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

### `ChangePassword` без session proof (#17) — принято
Достаточно `Id` + current password (как при логине). Refresh не требуется: энтропия Guid + знание пароля — достаточный proof; session proof — опционально на стороне хоста (`[Authorize]`).

### OAuth `ReturnUrl` (#18) — принято (контракт хоста)
Библиотека только хранит `ReturnUrl` в OAuth state и отдаёт обратно; HTTP-redirect не делает. Allowlist / relative-only / запрет open redirect — ответственность хоста.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| OAuth takeover по email | auto-link только при `profile.EmailVerified` + local verified |
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
| #4 Lookup без PreferVerified | `OrderByDescending` verified перед FirstOrDefault |
| #5 Microsoft EmailVerified | только OIDC `email` + `email_verified` (не Graph fallback) |
| #21 Microsoft verified без OIDC email | Graph mail не verified при `email_verified` без userinfo `email` |
| #6 OTP vs notify email | OTP: unverified OK; notify: verified only |
| #7 Lockout на OTP-login | `ValidateCodeAsync` lockout как у password |
| #8 Enumeration на шагах | единый `Invalid credentials.` + log |
| #10 Phone fallback | account phone после email (OTP/notify rules) |
| #11 OTP send rate limit | `Authentication:OtpSendRateLimit` в `CodeService.SendAsync` |
| #15 SecurityStamp в JWT | claim `security_stamp` + check в ValidateAccess/Refresh |
| #19 OTP code trim | `CodeService.VerifyAsync` trim как `ValidateCodeAsync` |
| Preferred email/phone | `CommunicationEndpointsGetAll` / `SetPreferred` + resolve delivery/OTP |
| BREAKING.md ведётся | `docs/BREAKING.md`; новые секции **append** (хронология), не «новые сверху» |

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

1. **M13–M14:** half-validate API docs / misuse guidance.
2. **CR M20, M22–M28:** PII logs; OTP confirm channel; supersede+userId; SMS normalize; lockout reset; AuditEntityType; preferred unique; action URL.
3. **CR M29–M35, M39:** OAuth collision; SendAsync Guid; nullability; Guard exceptions; Bag nullable; IP binding config; idle double-audit.
4. **CR M36–M38:** решить принять/отклонить (password 128, ClientContext form, Audit PII) — сейчас конфликт с принятым/закрытым.
5. **M40–M41 (бывший TO-DO):** явный выбор канала; messenger send + bot verification.
6. **CR minor:** XML docs / PhoneE164 / JsonHelpers hygiene.
