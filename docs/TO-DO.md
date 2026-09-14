# Cross.Identity — open backlog (`TO-DO`)

**Id high-water (не переиспользовать ≤):** `C1` `H27` `M92` `L17`

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
- Apple в registry без реализации (`NotSupportedException`) — не включать в Providers до реализации.
- `main.GetUserAccountId` existence oracle — продуктовое решение; reject-пути дают единый `Invalid credentials.`
- `PasswordAlgoEnum.SHA256` obsolete; pepper ignored; default Argon2id/PBKDF2.
- `ChangePassword` без library session proof — `UserAccountId` + current password; session proof опционально на хосте.
- OAuth `ReturnUrl` — библиотека только хранит/отдаёт; allowlist open redirect — хост.
- Refresh + `Empty` при `SessionBindingCheckIp=true` → `ValidationException`; при `false` — прежняя UA/FP логика.
- Password max 32 в stock `collectForm` — контракт UX/JSON, не hasher/БД.
- OAuth unverified squat + verified profile → новый verified account (не auto-link).
- Audit PII в `auth.Audits` (Ip/UA/Fingerprint) — forensics by design; retention — хост.
- PII в логах auth steps (email/phone) — forensics; redaction/sink — хост.
- Выбор типа канала / messenger bot (#40/#41) — вне 2.0 stock scope.
- `ChannelEnum.WhatsApp` — typo `WatsApp` удалён; без obsolete alias.
- User-scoped flows (2.2): session proof на стороне хоста; библиотека принимает `UserAccountId` без RefreshToken.
- Lifecycle bags (2.3): хост резолвит identity до `ExecuteAsync` (`Jti` / `UserAccountId`); без parse compact refresh на logout/refresh/change-password paths.
- Scaffold `Release:` — PR optional (`--pr`); без auto-`gh`.
- `ChangeAccountEmail` — account op на `IUserService`; upsert email-endpoint = delivery/OTP sync side-effect (не `ICommunicationEndpointService` как owner).
- Optional `LanguageCode` + `Authentication:Notifications` — host config; library без built-in brand defaults; stock `{{supportEmail}}` (не `{{support}}`).
- `{{support}}` → `{{supportEmail}}` унификация — не consumer-breaking (поле всегда `SupportEmail`; `{{support}}` был typo/alias).
- OTP в HTML email preheader — осознанный UX; кастомный host template может убрать.
- `NotificationComposer` без HtmlEncode brand/placeholders — sanitize зона хоста / доверенный config.
- CHANGELOG dated pre-tag для ship prep — ок; Unreleased не обязателен.
- `repository-link` при missing/non-GitHub origin — soft empty + exit 0 (не hard-fail).
- Register `Password` optional (2.4.1): blank/whitespace → `PasswordPhc` null; OTP / set-password на хосте; клиенты с паролем не ломаются.
