Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.7.0` (closed) · **ветка:** `release/register-email-or-phone` · **база:** `origin/master` (`v2.6.0`) · **дата:** `2026-11-11`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.7.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.6.0.md`](RELEASE-PLAN-2.6.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **13** файлов · **+454 / −82**. Open C/H/M/L пустые.

**CodeRabbit:**
- `2026-11-10` · log `.cursor/skills/coderabbit/.cache/cr-release-register-email-or-phone-vs-origin-master-all-20261110-235129.jsonl` · 0 findings (0 Critical, 0 Major, 0 Minor) → Open C/H/M/L пустые.
- `2026-11-10` · log `.cursor/skills/coderabbit/.cache/cr-release-register-email-or-phone-vs-origin-master-all-20261110-235216.jsonl` · 1 finding (0 Critical, 0 Major, 1 Minor) → Open C/H/M/L пустые.

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / licensing / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

- Stock `main.Register` требует **хотя бы один** OTP-канал (`Email` **или** `PhoneNumber`); `UserName` — только optional alias, сам по себе регистрации не даёт.
- `null` / `""` / whitespace по identity-полям считаются отсутствием везде в цепочке `collectForm` → bag → `CreateUserAsync`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M113 atLeast/exactlyOne form-level message | `AddFailure(message)` without PropertyName; empty `fields` no longer indexes `[0]` |
| ✅ #M109 Form `atLeastOneRequired` / `exactlyOneRequired` | `UnifiedFormValidatorFactory` evaluates both kinds; empty = null/`""`/whitespace |
| ✅ #M110 Stock `main.Register` Email\|Phone | `Email`/`PhoneNumber` optional + `atLeastOneRequired`; FLOWS updated |
| ✅ #M111 `CollectFormStep` blank → null | Whitespace-only strings cleared before phone E.164 / bag write |
| ✅ #M112 `CreateUserAsync` NullIfWhiteSpace | Email / phone / username normalization treats blank as null |
| ✅ #H32 BREAKING Register email-or-phone | `docs/BREAKING.md` From 2.6.0→2.7.0 + CHANGELOG `v2.7.0` |
| ✅ Tests Register / validators / UserService | Flow + factory + create coverage for phone-only / blank identities |

---

## Что в библиотеке уже нормально

- `CollectFormStepFactory` уже парсил `atLeastOneRequired` / `exactlyOneRequired` в schema JSON — не хватало только runtime в validator factory.
- OTP `sendCode` после register по-прежнему резолвит канал через `ResolveOtpTargetAsync` (email или phone на аккаунте).

---

## Приоритет фиксов

_(пусто — релиз `2.7.0` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
