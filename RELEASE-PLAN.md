Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Средний** — только ⬜ open. **Закрыто** — все ✅ (номера сохраняются).

**CodeRabbit (local CLI, 2026-08-23):** `coderabbit review --committed --base origin/dev --dir Cross.Identity` — Free plan limit 150 files; полный diff 293 → scope только `Cross.Identity/` (40 findings: 22 major / 18 minor). Сырой лог: `/tmp/cr-identity.jsonl`.

---

## Средний (противоречия / баги контрактов)

### 13. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.

### 14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается.

### 20. PII в логах SendCode / GetUserAccountId / ValidateCode (CR)
`SendCodeStep`, `GetUserAccountIdStep`, `UserService` — в Information/Warning пишется raw `selector.Value` (email/phone). Маскировать или логировать `userId`.

### 23. OTP supersede без `userId` (CR)
`CodeService` — supersede active codes фильтрует только по email/phone; добавить `UserAccountId` в predicate.

### 26. `AuditEntityType` discriminator collision (CR)
`LinkedMessenger` / `UserCommunicationEndpoint` / `ExternalLoginState` — одинаковые numeric values.

### 28. SendCode action URL всегда `/reset-password` (CR)
`SendCodeStep.BuildActionUrl` — path не зависит от `Template` (verify/register vs reset).

### 32. `UserAccountGuard` → `InvalidOperationException` (CR)
Conflict на email/phone бросает `InvalidOperationException` вместо Conflict/Validation.

### 33. `Bag` nullable `Convert.ChangeType` (CR)
`Bag.Get` / `TryGet` — для `T?` value-types передавать underlying type в `Convert.ChangeType`.

### 35. `WatsApp` obsolete alias (CR)
`ChannelEnum` — вернуть obsolete `WatsApp` для source/serialization compat (сейчас breaking rename в 2.0).

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

### `HostSuppliedClientContext` — trusted pipeline хоста
`IpAddress` / `UserAgent` / `DeviceFingerprint` из bag/form. Библиотека не читает `HttpContext`; хост перезаписывает из server-side metadata. Поля в `main.ExternalLogin.json` collectForm — **принято** (CR M37 отклонён; см. ✅ #37 в «Закрыто»).

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

### `main.GetUserAccountId` existence oracle — принято
Успех возвращает `{ user_account_id }` и раскрывает существование пользователя. Продуктовое решение; шаги SendCode/VerifyCode/GetUserAccountId на reject дают единый `Invalid credentials.`.

### `PasswordAlgoEnum.SHA256` (#16) — принято
Obsolete; pepper в `HashSha256`/`VerifySha256` игнорируется; нет auto-upgrade на Argon2id. Default path — Argon2id/PBKDF2 + pepper. SHA256 только для явного legacy; хост не должен включать его для новых хешей.

### `ChangePassword` без session proof (#17) — принято
Достаточно `Id` + current password (как при логине). Refresh не требуется: энтропия Guid + знание пароля — достаточный proof; session proof — опционально на стороне хоста (`[Authorize]`).

### OAuth `ReturnUrl` (#18) — принято (контракт хоста)
Библиотека только хранит `ReturnUrl` в OAuth state и отдаёт обратно; HTTP-redirect не делает. Allowlist / relative-only / запрет open redirect — ответственность хоста.

### Password max 32 (#36) — принято
Stock `collectForm` (`main.Register`, `main.Token`, `main.ResetPassword`, `main.ChangePassword`, …): `min: 8`, `max: 32`. Лимит — контракт stock JSON / UX, не ограничение hasher или колонки БД. CodeRabbit max 128 отклонён: 32 уже закрыто в коде; хост может поднять `max` в кастомном flow override или своей валидации до submit.

### OAuth unverified squat + verified profile (#29) — принято
`ExternalLoginService.ResolveOrCreateUserAsync`: если local row с тем же email **unverified**, а OAuth-провайдер вернул **verified** email — создаётся **новый** аккаунт с `EmailVerified = true` (squat остаётся unverified). Auto-link только при **обоих verified** (см. «OAuth takeover по email»). CodeRabbit: блокировать при unverified squat — отклонено: verified OAuth = доказательство владения email; жертва squat получает свой verified-аккаунт; squat не блокирует legitimate OAuth signup. Unverified OAuth + squat по-прежнему `ValidationException`.

### Audit PII в `auth.Audits` (#38) — принято
`AuditEntity.IpAddress` / `UserAgent` / `DeviceFingerprint` — намеренно для issue/revoke forensics (`AuditService.RecordTokenIssued` / `RecordTokenRevoked`); на token rows только `RevokedAt`. CR M38 (минимизировать PII) **отклонён**: retention и доступ к `auth.Audits` — ответственность хоста; см. `docs/BREAKING.md` (revoke audit metadata).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ #29 OAuth unverified squat (CR отклонён) | verified OAuth + local unverified → новый verified account; см. «Принято» |
| ✅ OAuth takeover по email | auto-link только при `profile.EmailVerified` + local verified |
| ✅ Account linking без auth | linking требует `RefreshToken` того же user |
| ✅ IDOR на flows с `UserId` | `EnsureRefreshTokenBelongsToUserAsync` на endpoints/OAuth unlink/getAll |
| ✅ OTP attempts в `CodeService` | поиск по identity; `Attempts++` при неверном коде |
| ✅ Logout access revoke | `RevokeRefreshTokenForLogoutAsync` отзывает access той же `FamilyId` |
| ✅ CSPRNG для OTP | `RandomNumberGenerator.GetInt32` в `CodeGeneratorHelper` |
| ✅ GitHub email verified | только `verified: true` emails |
| ✅ `IsActive` | `UserAccountGuard` + checks в auth pipeline |
| ✅ Lockout пароля | `ValidatePasswordAsync` + `Authentication:Lockout` |
| ✅ Старые OTP при resend | supersede active codes (`ExpiresAt = now`) |
| ✅ Session binding на refresh | `Created*` на refresh + compare `HostSuppliedClientContext` |
| ✅ #34 Session IP binding config | `Authentication:Jwt:SessionBindingCheckIp`; default `false` (opt-in); device/UA always when captured |
| ✅ Refresh idle timeout | `LastActivityAt` + `RefreshTokenIdleTimeout` |
| ✅ UserName + Code | `ResolveOtpTargetAsync` + unified verify path |
| ✅ #36 Password max 32 (CR 128 отклонён) | stock Password fields `max: 32`; см. «Принято» |
| ✅ `TokenStep` fail vs ok | `NotAuthorizedException` на bad credentials |
| ✅ SendCode enumeration (unknown) | `Invalid credentials.` без отправки |
| ✅ Messenger SendCode no-op | `NotSupportedException` |
| ✅ `VerifyTokenStep` swallow | только token-format errors → `Valid=false` |
| ✅ Мёртвые ключи `main.Token.json` | удалены |
| ✅ `CreatedBy` | колонка удалена |
| ✅ #2 TokenStep ↔ SendCode channel | `ValidateCodeAsync` → `ResolveOtpTargetAsync` |
| ✅ #3 VerifyAsync без userId | `VerifyAsync(userId, …)` + `UserAccountId` match |
| ✅ #4 Lookup без PreferVerified | `OrderByDescending` verified перед FirstOrDefault |
| ✅ #5 Microsoft EmailVerified | только OIDC `email` + `email_verified` (не Graph fallback) |
| ✅ #21 Microsoft verified без OIDC email | Graph mail не verified при `email_verified` без userinfo `email` |
| ✅ #6 OTP vs notify email | OTP: unverified OK; notify: verified only |
| ✅ #7 Lockout на OTP-login | `ValidateCodeAsync` lockout как у password |
| ✅ #8 Enumeration на шагах | единый `Invalid credentials.` + log |
| ✅ #10 Phone fallback | account phone после email (OTP/notify rules) |
| ✅ #11 OTP send rate limit | `Authentication:OtpSendRateLimit` в `CodeService.SendAsync` |
| ✅ #15 SecurityStamp в JWT | claim `security_stamp` + check в ValidateAccess/Refresh |
| ✅ #19 OTP code trim | `CodeService.VerifyAsync` trim как `ValidateCodeAsync` |
| ✅ #22 Confirm contact по OTP-каналу (CR) | `ValidateCodeAsync` — verified flags по `otpTarget.Channel`, не selector field |
| ✅ #24 SMS normalize (CR) | `CodeService` send/verify — `ChannelEnum.NormalizeAddress` (SMS trim-only) |
| ✅ #25 Lockout after expiry (CR) | `RecordFailedAccess` сбрасывает счётчик при истёкшем `LockoutEnd` |
| ✅ #27 Preferred unique index (CR) | `UX_*_User_Preferred` — один `IsPreferred` на user; `1_10_*` |
| ✅ #30 SendAsync userId Guid (CR) | `ICodeService.SendAsync` — `Guid userId`, как `VerifyAsync` |
| ✅ #31 GetUserAccountIdByAsync nullability (CR) | `Task<Guid?>`; missing user → `null` |
| ✅ Preferred email/phone | `CommunicationEndpointsGetAll` / `SetPreferred` + resolve delivery/OTP |
| ✅ BREAKING.md ведётся | `docs/BREAKING.md`; новые секции **append** (хронология), не «новые сверху» |
| ✅ #37 HostSuppliedClientContext в ExternalLogin form (CR отклонён) | collectForm Ip/UA/Fingerprint — host trusted pipeline; см. «Принято» |
| ✅ #38 Audit PII в `auth.Audits` (CR отклонён) | Ip/UA/Fingerprint на issue/revoke — forensics by design; retention/access — хост; см. «Принято» |
| ✅ #42 GetUserId → GetUserAccountId | operation `GetUserAccountId`, step `getUserAccountId`, `GetUserAccountIdByAsync`, `main.GetUserAccountId.json` |
| ✅ #43 Bag keys `UserId` → `UserAccountId` | `userAccountIdKey`, step output, collectForm; `collectResult` → `user_account_id` |
| ✅ #44 `ClientContext` → `HostSuppliedClientContext` | type/file/API param `hostSuppliedClientContext`; `Empty` / `Read(bag)`; `docs/BREAKING.md` |
| ✅ #45 License JWT claim `user_id` | `License.UserId` / claim `"user_id"` — отдельно от identity `user_account_id` |
| ✅ #46 CR minor XML docs | `NotificationMessage`, entities, `Configure`, `HostSuppliedClientContext` param docs |
| ✅ #47 `Sample.Api.http` | `UserAccountId` / `USER_ACCOUNT_ID` на identity flows (не license) |

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
2. **CR M20, M28:** PII logs; action URL.
3. **CR M26, M32–M33, M35, M39:** AuditEntityType; Guard exceptions; Bag nullable; idle double-audit.
4. **M40–M41:** явный выбор канала; messenger send + bot verification.
5. **CR minor:** PhoneE164 / JsonHelpers / PhoneChannels visibility / idle double-audit (#39).
