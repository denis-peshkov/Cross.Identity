Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
**Средний** — только ⬜ open. **Закрыто** — все ✅ (номера сохраняются).

**CodeRabbit (local CLI, в период hardening):**
- **2026-08-23 23:24** — `release/add-new-flows` vs `origin/dev`, `--dir Cross.Identity` — **4 findings** (4 minor). Лог: `/tmp/cr-identity-20260823-2324.log`.
- **2026-08-23 22:49** — **13 findings** (11 major / 2 minor). Major (#48–#53 и др.) → «Закрыто» / «Принято».

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

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
Контракт для второго шага после crypto (JwtBearer / `ValidateAccessTokenAsync`), не для standalone auth. Misuse guidance / docs — post-2.0 ([`TO-DO.md`](TO-DO.md) M13 / M14).

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

### Refresh + `Empty` при `SessionBindingCheckIp` (#48) — принято
При **`Authentication:Jwt:SessionBindingCheckIp = true`** и anchor с `Created*` refresh **обязан** передавать trusted `IpAddress` / `UserAgent` / `DeviceFingerprint` (как на Token). `HostSuppliedClientContext.Empty` → **`ValidationException`** до сравнения binding (без family revoke). `Empty` по-прежнему OK на logout / password change / revoke. При **`SessionBindingCheckIp = false`** (default) — прежняя логика сравнения UA/FP; хосту всё равно рекомендуется прокидывать metadata на refresh.

### Password max 32 (#36) — принято
Stock `collectForm` (`main.Register`, `main.Token`, `main.ResetPassword`, `main.ChangePassword`, …): `min: 8`, `max: 32`. Лимит — контракт stock JSON / UX, не ограничение hasher или колонки БД. CodeRabbit max 128 отклонён: 32 уже закрыто в коде; хост может поднять `max` в кастомном flow override или своей валидации до submit.

### OAuth unverified squat + verified profile (#29) — принято
`ExternalLoginService.ResolveOrCreateUserAsync`: если local row с тем же email **unverified**, а OAuth-провайдер вернул **verified** email — создаётся **новый** аккаунт с `EmailVerified = true` (squat остаётся unverified). Auto-link только при **обоих verified** (см. «OAuth takeover по email»). CodeRabbit: блокировать при unverified squat — отклонено: verified OAuth = доказательство владения email; жертва squat получает свой verified-аккаунт; squat не блокирует legitimate OAuth signup. Unverified OAuth + squat по-прежнему `ValidationException`.

### Audit PII в `auth.Audits` (#38) — принято
`AuditEntity.IpAddress` / `UserAgent` / `DeviceFingerprint` — намеренно для issue/revoke forensics (`AuditService.RecordTokenIssued` / `RecordTokenRevoked`); на token rows только `RevokedAt`. CR M38 (минимизировать PII) **отклонён**: retention и доступ к `auth.Audits` — ответственность хоста; см. `docs/BREAKING.md` (revoke audit metadata).

### PII в логах auth steps (#20) — принято
`SendCodeStep`, `VerifyCodeStep`, `GetUserAccountIdStep`, `UserService.ValidateCodeAsync`, `CodeService` — raw email/phone/destination в Information/Warning для operational forensics, пока клиенту отдаётся единый `Invalid credentials.` (anti-enumeration). CR M20 (маскировать / только `userId`) **отклонён**: без identity в логе сложнее саппорт и расследование (особенно «user not found» до резолва Guid); retention, redaction и доступ к log sink — ответственность хоста (аналогично #38).

### Выбор типа канала коммуникации (#40) — принято (2.0 scope)
Отдельного stock flow «выбери тип канала» в 2.0 **нет**. Контракт: канал доставки/OTP = **preferred verified endpoint** (или `LockChannelAsEmail` / account email / phone по правилам `ResolveOtpTargetAsync` / `ResolveTargetAsync`). Selector (`Email` vs `Phone`) — только identity lookup, не выбор канала. Явный UI выбора канала — опционально на стороне хоста (`CommunicationEndpointSetPreferred`, кастомный flow).

### Мессенджер + верификация / бот (#41) — принято (2.0 scope)
`Telegram` / `Viber` / `WhatsApp`, `LinkedMessenger`, endpoints в модели — **задел под будущее**. В 2.0: нет messenger sender, нет bot link/verify flow; OTP только Email/Sms (`SupportsOtp`); messenger preferred → `ToEmailOrSms()` / `NotSupportedException` на SendCode (см. #9). Реализация sender + bot verification — post-2.0 / кастом хоста.

### `ChannelEnum.WhatsApp` (#35) — принято (2.0)
Legacy typo **`WatsApp` удалён**; единственное имя — **`WhatsApp`**. Obsolete alias не добавляем: stock flow JSON **без** `channel`; endpoint channel в БД — `smallint` (numeric value не менялся). Хост обновляет C# / свой string enum на `WhatsApp`.

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
| ✅ Старые OTP при resend | supersede active codes per `UserAccountId` + destination (`ExpiresAt = now`) |
| ✅ #23 OTP supersede scoped by user (CR) | `SupersedeActive*VerificationsAsync` фильтрует по `UserAccountId` + email/phone |
| ✅ #26 `AuditEntityType` unique values (CR) | `UserCommunicationEndpoint=7`, `ExternalLoginState=8`, `LinkedMessenger=9` |
| ✅ #33 `Bag` nullable `Convert.ChangeType` (CR) | `Get` / `TryGet` — underlying type для `T?` value-types; null в `TryGet` |
| ✅ #32 Verified contact conflict → `ConflictException` (CR) | `UserAccountGuard` + `CreateUserAsync`; email/phone/username duplicate |
| ✅ #28 SendCode action URL by template (CR) | `verify` → `/verify?code=`; `reset` → `/reset-password?code=`; + `email`/`phone` when selector is Email/PhoneNumber |
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
| ✅ #20 PII в логах auth steps (CR отклонён) | selector identity / OTP paths в Information/Warning для forensics; см. «Принято» |
| ✅ #10 Phone fallback | account phone после email (OTP/notify rules) |
| ✅ #11 OTP send rate limit | `Authentication:OtpSendRateLimit` в `CodeService.SendAsync` |
| ✅ #15 SecurityStamp в JWT | claim `security_stamp` + check в ValidateAccess/Refresh |
| ✅ #19 OTP code trim | `CodeService.VerifyAsync` trim как `ValidateCodeAsync` |
| ✅ #22 Confirm contact по OTP-каналу (CR) | `ValidateCodeAsync` — verified flags по `otpTarget.Channel`, не selector field |
| ✅ #24 SMS normalize (CR) | `CodeService` send/verify — `ChannelEnum.NormalizeAddress` (SMS trim-only) |
| ✅ #25 Lockout after expiry (CR) | `RecordFailedAccess` сбрасывает счётчик при истёкшем `LockoutEnd` |
| ✅ #27 Preferred unique index (CR) | `UX_*_User_Preferred` — один `IsPreferred` на user; `1_10_*` |
| ✅ #30 SendAsync userId Guid (CR) | `ICodeService.SendAsync` — `Guid userAccountId`, как `VerifyAsync` |
| ✅ #31 GetUserAccountIdByAsync nullability (CR) | `Task<Guid?>`; missing user → `null` |
| ✅ Preferred email/phone | `CommunicationEndpointsGetAll` / `SetPreferred` + resolve delivery/OTP |
| ✅ #40 Выбор канала (2.0 scope, принято) | канал = preferred endpoint; selector — identity only; см. «Принято» |
| ✅ #41 Messenger bot verify (2.0 scope, принято) | модель/endpoints задел; sender+bot post-2.0; см. «Принято» / #9 |
| ✅ #35 `ChannelEnum.WhatsApp` (CR отклонён) | `WatsApp` typo removed; no obsolete alias; flow JSON без `channel`; см. «Принято» |
| ✅ BREAKING.md ведётся | `docs/BREAKING.md`; новые секции **append** (хронология), не «новые сверху» |
| ✅ #37 HostSuppliedClientContext в ExternalLogin form (CR отклонён) | collectForm Ip/UA/Fingerprint — host trusted pipeline; см. «Принято» |
| ✅ #38 Audit PII в `auth.Audits` (CR отклонён) | Ip/UA/Fingerprint на issue/revoke — forensics by design; retention/access — хост; см. «Принято» |
| ✅ #42 GetUserId → GetUserAccountId | operation `GetUserAccountId`, step `getUserAccountId`, `GetUserAccountIdByAsync`, `main.GetUserAccountId.json` |
| ✅ #43 Bag keys `UserId` → `UserAccountId` | `userAccountIdKey`, step output, collectForm; `collectResult` → `user_account_id` |
| ✅ #44 `ClientContext` → `HostSuppliedClientContext` | type/file/API param `hostSuppliedClientContext`; `Empty` / `Read(bag)`; `docs/BREAKING.md` |
| ✅ #45 License JWT claim `user_id` | `License.UserId` / claim `"user_id"` — отдельно от identity `user_account_id` |
| ✅ #46 CR minor XML docs | `NotificationMessage`, entities, `Configure`, `HostSuppliedClientContext` param/type docs; record **properties** — см. [`TO-DO.md`](TO-DO.md) |
| ✅ #47 `Sample.Api.http` | `UserAccountId` / `USER_ACCOUNT_ID` на identity flows (не license) |
| ✅ EmailVerified rename (CR отклонён как open) | PreDeployment `1_07`–`1_09`; `docs/BREAKING.md` § `EmailConfirmed` → `EmailVerified` |
| ✅ RefreshToken / VerifyToken `user_account_id` (CR stale) | intentional 2.0 output; см. #43 / `BREAKING.md` (не `user_id`) |
| ✅ `IdentityConstants` claim rename (CR stale) | `UserAccountId` → `"user_account_id"`; license JWT `"user_id"` отдельно (#45) |
| ✅ CR: GetUserAccountId enumeration (2026-08-23) | см. «Принято» — existence oracle |
| ✅ CR: PII в auth step logs (2026-08-23) | см. «Принято» #20 — forensics by design |
| ✅ #50 ExternalLogin PK bigint→Guid (CR) | `1_11_auth_UsersExternalLogins_UserExternalLoginIdToGuid.sql`; `docs/BREAKING.md` |
| ✅ #48 Refresh Empty при SessionBindingCheckIp (CR) | `ValidationException` если `Empty` + anchor; docs/API; `Empty` OK на logout/revoke |
| ✅ #49 Re-hash swallow cancellation (CR) | `ValidatePasswordAsync`: `catch (DbUpdateException) when (needRehash)`; cancellation пробрасывается |
| ✅ #51 `Bag.TryGet` Guid string parse (CR) | `TryGet` — `Guid.TryParse` как в `Get` для `Guid` / `Guid?` |
| ✅ #52 CreateUserAsync UserName split (CR) | `userName` / `PhoneNumber` через `ToString().Trim()` для entity и normalized paths |
| ✅ #53 UpsertAsync trust boundary (CR) | `UpsertAsync` + `SyncAccountContactsAsync` на internal `ICommunicationEndpointUpsertService`; DI singleton per scope |

---

## Что в библиотеке уже нормально

- Crypto `ValidateAccessTokenAsync` (signature + DB `jti` + `security_stamp`).
- Refresh replay → family revoke (`REPLAY_DETECTED`).
- OAuth state one-time use + expiry.
- Refresh token hash в DB, не plain text.
- External login link/unlink / user-scoped endpoints требуют `RefreshToken` + `EnsureRefreshTokenBelongsToUserAsync`.
- Unlink last method without password blocked.
- Password change revokes all tokens + `SecurityStamp` rotation (stamp also in JWT; mismatch fails validate).
- E.164 gate на `collectForm` PhoneNumber.
- `TokenPairIssuer` централизует выдачу пары.
- SendCode ↔ VerifyCode channel resolution (`ResolveOtpTargetAsync`) согласованы между собой.
- Argon2id/PBKDF2 password hashing + pepper (не SHA256).
- OTP hash storage, max 3 attempts, supersede on resend.

---

## Приоритет фиксов

_(пусто — релиз 2.0.0 опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md); ConcurrencyStamp → [`RELEASE-PLAN-2.1.1.md`](RELEASE-PLAN-2.1.1.md).)_
