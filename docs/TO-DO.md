# Cross.Identity — open backlog (`TO-DO`)

**Id high-water (не переиспользовать ≤):** `C1` `H34` `M121` `L23`

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

### H1. EF Core / DbUp: deployment state vs `1_PreDeployment`
CodeRabbit: в `.cursor/rules/102-backend-efcore.mdc` (и дубль в skill) эвристику «новый деплой» брать из **DbUp journal** / migration state, не из содержимого `1_PreDeployment`. Новые таблицы после релиза — paired `2_Initial` + idempotent `1_PreDeployment`, journal после predeployment.

---

## Средний (противоречия / баги контрактов)

### M13. `GetClaimValue` для JWS без подписи
Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую. Нужны docs / misuse guidance.

### M14. `ValidateAccessTokenJtiAsync`
Только DB lookup по JTI, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается. Нужны docs / misuse guidance.

### M39. Idle revoke double-audit
`HandleRefreshTokenIdleExpiredAsync` / idle path в `JwtTokenService` — presented token может аудититься/ревокаться дважды при family revoke.

### M42. `JsonHelpers` `Enum.IsDefined`
После `Enum.TryParse` требовать `Enum.IsDefined`.

### M44. Release-plan language (EN vs RU) — CR Major
CodeRabbit **Major** (deferred): English throughout release-plan workflow docs.
- `.cursor/skills/release-plan/SKILL.md` — translate skill body as needed; replace **Language: Russian body…** with English-body rule.
- Templates: translate intro/header in `templates/RELEASE-PLAN.md` and `templates/RELEASE-PLAN-FINALIZED.md` (structure/meaning unchanged).
- Follow-on: generated `docs/RELEASE-PLAN-*.md` / this file if policy becomes EN.
Сейчас в репо принят RU для планов/`TO-DO`; осознанный выбор (оставить RU или EN) — потом, не в текущем цикле.

### M98. Apple Sign In: JWKS re-validation of `id_token`
**Что:** после token exchange читать `id_token` не «как JSON payload», а с crypto-проверкой подписи по Apple JWKS (`https://appleid.apple.com/auth/keys`), плюс claims `iss` / `aud` (== ClientId) / `exp` (и при наличии — `nonce`).
**Зачем:** сейчас токен доверяется потому что пришёл по TLS с `appleid.apple.com/auth/token` (как Graph/userinfo у других провайдеров). JWKS закрывает класс ошибок «поддельный `id_token` в ответе exchange» (compromised HTTP handler, proxy, ошибочный mock в prod) и выравнивает Apple с OIDC best practice. Не блокер текущего Sign In; hardening вне 2.6.0 ship slice.

---

## Низкий (техдолг / несогласованности)

### L1. `TwoFactorEnabled` мёртвое поле
В entity есть, в auth pipeline не используется.

### L2. `DeveloperMode` → `LastCode` в API
Skip send (`Authentication:DeveloperMode`); утечка OTP через response, если включить в prod.

### L3. `AuditService.Record` без `SaveChanges`
Audit теряется, если caller не закоммитит.

### L5. Legacy-поля в `UserAccountEntity`
Закомментированы `PasswordSalt`, `PasswordHash`, …

### L7. `AuditEntity` XML properties
XML для public properties.

### L8. `PhoneE164` style / catch
`_pattern`/`_util`; braces; catch только `NumberParseException`.

### L9. `PhoneChannels` visibility
`ChannelEnumExtensions.PhoneChannels` — сделать `private` (mutation).

### L10. triage skill: убрать опцию `ru`
CodeRabbit: в `.cursor/skills/triage/SKILL.md` (и связанных usage) оставить English как единственный язык output; убрать documented `ru`.

---

## Принято (осознанный trade-off)

- Refresh rotation без атомарности в библиотеке — хост оборачивает refresh во внешнюю транзакцию (`FLOWS.md`); race → `REPLAY_DETECTED` + family revoke.
- Sync-over-async в `GetClaimValue` (JWE): `GetAwaiter().GetResult()`; основной путь — JWS без async I/O.
- `HostSuppliedClientContext` — trusted pipeline хоста (Ip/UA/Fingerprint); библиотека не читает `HttpContext`.
- Delivery channel: `LockChannelAsEmail` → preferred verified → account email/phone; selector Email/Phone — только identity lookup.
- Публичные half-validate API (`GetClaimValue` / JTI lookup) — для второго шага после crypto, не standalone auth (см. open M13/M14).
- OTP plaintext в логах `CodeService.SendAsync` — хост не утекает verbose logs в SIEM.
- Messenger preferred → SMS (`ToEmailOrSms`); нет messenger sender / remapping.
- `main.GetUserAccountId` existence oracle — продуктовое решение; reject-пути дают единый `Invalid credentials.`
- `PasswordAlgoEnum.SHA256` obsolete; pepper ignored; default Argon2id/PBKDF2.
- `ChangePassword` без library session proof — `UserAccountId` + current password; session proof опционально на хосте.
- OAuth `ReturnUrl` — библиотека только хранит/отдаёт; allowlist open redirect — хост.
- Refresh + `Empty` при `SessionBindingCheckIp=true` → `ValidationException`; при `false` — прежняя UA/FP логика.
- Password max 32 в stock `collectForm` — контракт UX/JSON, не hasher/БД.
- OAuth unverified squat + verified profile → новый verified account (не auto-link).
- Audit PII в `auth.Audits` (Ip/UA/Fingerprint) — forensics by design; retention — хост.
- PII в логах auth steps (email/phone) — forensics; redaction/sink — хост.
- Выбор типа канала / messenger bot (#40/#41) — вне stock scope.
- `ChannelEnum.WhatsApp` — typo `WatsApp` удалён; без obsolete alias.
- User-scoped flows: session proof на стороне хоста; библиотека принимает `UserAccountId` без RefreshToken.
- Lifecycle bags: хост резолвит identity до `ExecuteAsync` (`Jti` / `UserAccountId`); без parse compact refresh на logout/refresh/change-password paths.
- Scaffold `Release:` — PR optional (`--pr`); без auto-`gh`.
- `ChangeAccountEmail` — account op на `IUserService`; upsert email-endpoint = delivery/OTP sync side-effect (не `ICommunicationEndpointService` как owner).
- Optional `LanguageCode` + `Authentication:Notifications` — host config; library без built-in brand defaults; stock `{{supportEmail}}` (не `{{support}}`).
- `{{support}}` → `{{supportEmail}}` унификация — не consumer-breaking (поле всегда `SupportEmail`; `{{support}}` был typo/alias).
- OTP в HTML email preheader — осознанный UX; кастомный host template может убрать.
- `NotificationComposer` без HtmlEncode brand/placeholders — sanitize зона хоста / доверенный config.
- CHANGELOG dated pre-tag для ship prep — ок; Unreleased не обязателен.
- `repository-link` при missing/non-GitHub origin — soft empty + exit 0 (не hard-fail).
- Register `Password` optional: blank/whitespace → `PasswordPhc` null; OTP / set-password на хосте; клиенты с паролем не ломаются.
- GitVersion: squash `hotfix/*` → Patch, `release/*` → Minor (`main.source-branches` = release+hotfix only; root Patch; commit-message incrementing off; branch digits ignored).
- GitVersion `next-version: 1.0.0` — низкий ConfiguredNextVersion fallback; при тегах побеждает `TaggedCommit`; не поднимать под каждый ship.
- Mixed release slice: OAuth providers + CI/workflow / package bumps в одном ship — осознанно, не дробить без нужды.
- Facebook seed `ProviderId=5`: если id занят не-Facebook строкой — silent skip; хост с кастомными provider ids чинит seed вручную.
- Apple DisplayName: без truncate/cap в app code; oversized form_post names → provider/SQL constraints.
- Release-plan / to-master: только ATX headings + blank line before `---`; setext out of scope для Change summary script.
- Stock `main.Register`: хотя бы один OTP-канал (`Email` или `PhoneNumber`); `UserName` — optional alias, сам по себе регистрации не даёт.
- Identity blank (`null` / `""` / whitespace) = отсутствие в `collectForm` → bag → `CreateUserAsync`.
- `LockChannelAsEmail=true` + phone-only Register: fail at `sendCode` (`Invalid credentials.`) accepted; host with lock must supply email for OTP (no pre-create form reject).
- SMS OTP bodies — short single-segment (`verify-sms` / `reset-sms`); email keeps long HTML/txt.
- `SendCodeStep` appends `-sms` automatically when channel is SMS (idempotent if already `-sms`).
- Sample.Api drops Romanian `register`/`verify` overrides; stock library still ships `register.ro` / `verify.ro`.
- Ukrainian template files stay `*.ua.*`; ISO `uk` maps to `ua` (HTML `lang="uk"` unchanged).
