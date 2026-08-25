# Cross.Identity — open backlog (`TO-DO`)

Инкрементальный backlog **вне** конкретного `RELEASE-PLAN-X.Y.Z.md`.  
Планы версий содержат **только** дельту релиза; незакрытое с предыдущих планов живёт здесь.

**Правила:**
- При сборке нового `RELEASE-PLAN-X.Y.Z.md`: открытое из предыдущего плана, **не** вошедшее в дельту → **добавить сюда** (если ещё нет).
- Если пункт **закрыт** в какой-то версии (`## Закрыто` в `RELEASE-PLAN-*.md`) → **удалить** из этого файла.
- Не дублировать «Принято» trade-off без open work.
- Секции критичности **всегда** присутствуют; пустые — заголовок + `---`.
- Источник (CodeRabbit, audit, …) **не важен** — класть сразу в C / H / M / L по смыслу работы.

**Нумерация (сквозная внутри группы):**

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий |

Формат: `### M13. Title` · закрытие в плане: `✅ M13 Short title`.  
Новый айтем — следующий свободный номер группы. Исторические id не перенумеровывать.

При triage внешних review (напр. CodeRabbit): Critical→`C`, Major→`H`, Minor→`M`, Trivial/Info→`L` — без отдельной секции CR.

**Легенда:** ⬜ open.

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

### M13. `GetClaimValue` для JWS без подписи
⬜ Публичный API: 3-part JWT — parse payload без crypto. В `VerifyTokenStep` перед этим есть `ValidateAccessTokenAsync` — ок. Риск — **misuse** API напрямую. Нужны docs / misuse guidance.

### M14. `ValidateAccessTokenJtiAsync` / `ValidateRefreshTokenAsync`
⬜ Только DB lookup, без JWT crypto. Для middleware после `OnTokenValidated` — ок; без crypto снаружи — дыра. В stock не вызывается. Нужны docs / misuse guidance.

### M39. Idle revoke double-audit
⬜ `HandleRefreshTokenIdleExpiredAsync` / idle path в `JwtTokenService` — presented token может аудититься/ревокаться дважды при family revoke.

### M40. `FLOWS.md` `main.Register` bag key
⬜ В таблице `userAccountIdKey: UserId`, в JSON / `collectResult` — `UserAccountId`.

### M41. `EndpointId` GUID regex
⬜ `main.CommunicationEndpointSetPreferred.json` — только `min/max: 36`, без GUID regex.

### M42. `JsonHelpers` `Enum.IsDefined`
⬜ После `Enum.TryParse` требовать `Enum.IsDefined`.

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

### L6. `HostSuppliedClientContext` XML properties
⬜ XML для record properties `IpAddress` / `UserAgent` / `DeviceFingerprint`.

### L7. `AuditEntity` XML properties
⬜ XML для public properties.

### L8. `PhoneE164` style / catch
⬜ `_pattern`/`_util`; braces; catch только `NumberParseException`.

### L9. `PhoneChannels` visibility
⬜ `ChannelEnumExtensions.PhoneChannels` — сделать `private` (mutation).

---

## Приоритет (подсказка)

1. **M13–M14:** half-validate API docs / misuse guidance.
2. **M39:** idle double-audit.
3. **M40–M42:** FLOWS bag key / EndpointId regex / JsonHelpers.
4. **L1–L9:** техдолг / XML / style.
