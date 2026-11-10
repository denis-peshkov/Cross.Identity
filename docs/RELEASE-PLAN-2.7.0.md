Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.7.0` · **ветка:** `release/register-email-or-phone` · **база:** `origin/master` (`v2.6.0`) · **дата:** `2026-11-10`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.7.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.6.0.md`](RELEASE-PLAN-2.6.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · **0** файлов (tip = `v2.6.0`). **WT:** **10** файлов · **+296 / −10** (excl. this plan). Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

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

Открытых пунктов этой дельты нет. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
