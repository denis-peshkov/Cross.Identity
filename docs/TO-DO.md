# Cross.Identity — open backlog (`TO-DO`)

Инкрементальный backlog **вне** конкретного `RELEASE-PLAN-X.Y.Z.md`.  
Планы версий содержат **только** дельту релиза; незакрытое с предыдущих планов живёт здесь.

**Правила:**
- При сборке нового `RELEASE-PLAN-X.Y.Z.md`: открытое из предыдущего плана, **не** вошедшее в дельту → **добавить сюда** (если ещё нет).
- Если пункт **закрыт** в какой-то версии (`## Закрыто` в `RELEASE-PLAN-*.md`) → **удалить** из этого файла.
- Не дублировать «Принято» trade-off без open work.
- Секции критичности **и** подгруппы CodeRabbit **всегда** присутствуют; пустые — заголовок + `---`.

**Нумерация (сквозная внутри группы):**

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий (техдолг, не CR) |

**CodeRabbit → наши уровни** ([docs](https://docs.coderabbit.ai/guides/code-review-overview)):

| CR | Out | Id prefix (`CR` + Out) | Смысл |
|----|-----|------------------------|--------|
| **Critical** | **C** | `CRC` | сбои, security, потеря данных |
| **Major** | **H** | `CRH` | серьёзный удар по функциональности / perf |
| **Minor** | **M** | `CRM` | надо править, но не критично для системы |
| **Trivial** | **L** | `CRL` | мелкие улучшения качества кода |
| **Info** | **L** | `CRL` | контекст, без обязательного action |

Префикс = **`CR` + наш уровень** (`C`/`H`/`M`/`L`), не имя severity CodeRabbit.  
Trivial и Info оба → Out **L** → общий сквозной ряд `CRL1`, `CRL2`, …  
Формат: `### CRM1. Title` · закрытие: `✅ CRM1 Short title` (в plan — секция Out).  
Исторические `M13` / `L1` не перенумеровывать.

**Легенда:** ⬜ open · источник = откуда перенесено.

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

### M13. `GetClaimValue` для JWS без подписи
⬜ Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую.  
Источник: hardening 2.0 (принято как half-validate; нужны docs / misuse guidance).

### M14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
⬜ Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается.  
Источник: hardening 2.0 (docs / misuse guidance).

### M39. Idle revoke double-audit? (CR)
⬜ `HandleRefreshTokenIdleExpiredAsync` — presented token может аудититься/ревокаться дважды при family revoke.  
Источник: CR / RELEASE-PLAN-2.0 carry-over. См. также CRM4.

---

## Низкий (техдолг / несогласованности)

### L1. `TwoFactorEnabled` мёртвое поле
⬜ В entity есть, в auth pipeline не используется.

### L2. `DeveloperMode` → `LastCode` в API
⬜ Skip send (`Authentication:DeveloperMode`); утечка OTP через response, если включить в prod.

### L3. `AuditService.Record` без `SaveChanges`
⬜ Audit теряется, если caller не закоммитит.

### L4. Закомментированный `IJwtIssuer`
⬜ В `IJwtTokenService.cs`.

### L5. Legacy-поля в `UserAccountEntity`
⬜ Закомментированы `PasswordSalt`, `PasswordHash`, …

---

## CodeRabbit

Findings с маппингом severity → наши C / H / M / L. Id = `CR` + Out. Подгруппы **всегда** на месте.

### Critical → C (`CRC`)

---

### Major → H (`CRH`)

---

### Minor → M (`CRM`)

### CRM1. `FLOWS.md` `main.Register` bag key
⬜ В таблице `userAccountIdKey: UserId`, в JSON / `collectResult` — `UserAccountId`.

### CRM2. `EndpointId` GUID regex
⬜ `main.CommunicationEndpointSetPreferred.json` — только `min/max: 36`, без GUID regex.

### CRM3. `JsonHelpers` `Enum.IsDefined`
⬜ После `Enum.TryParse` требовать `Enum.IsDefined`.

### CRM4. Idle path double audit/revoke
⬜ `JwtTokenService` idle path: не дублировать audit/revoke presented token (см. M39).

---

### Trivial → L (`CRL`)

### CRL1. `HostSuppliedClientContext` XML properties
⬜ XML для record properties `IpAddress` / `UserAgent` / `DeviceFingerprint`.

### CRL2. `AuditEntity` XML properties
⬜ XML для public properties.

### CRL3. `PhoneE164` style / catch
⬜ `_pattern`/`_util`; braces; catch только `NumberParseException`.

### CRL4. `PhoneChannels` visibility
⬜ `ChannelEnumExtensions.PhoneChannels` — сделать `private` (mutation).

---

### Info → L (`CRL`)

---

## Приоритет (подсказка)

1. **M13–M14:** half-validate API docs / misuse guidance.
2. **M39 / CRM4:** idle double-audit.
3. **CRM1–CRM3:** FLOWS / EndpointId / JsonHelpers.
4. **CRL1–CRL4 / L1–L5:** trivial CR + техдолг.
