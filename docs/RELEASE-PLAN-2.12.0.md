Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.12.0` · **ветка:** `release/apple-form-callback-url` · **база:** `origin/master` (`v2.11.0`) · **дата:** `2026-11-17`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.12.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **6** файлов · **+34 / −9**. Open: M130.

**CodeRabbit:**
- не запускался.

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / licensing / auth model)

---

## Средний (противоречия / баги контрактов)

### M130. FLOWS / README still say Apple uses `CallbackUrl` only
После `FormCallbackUrl` код для Apple берёт `FormCallbackUrl` (fallback `CallbackUrl`), но [`FLOWS.md`](../Cross.Identity/FLOWS.md) Apple note и README External OAuth всё ещё описывают единый `CallbackUrl` как POST bridge. Обновить на `FormCallbackUrl` (+ fallback).

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

- `FormCallbackUrl` optional: empty → Apple uses `CallbackUrl` (backward compatible; not a NuGet breaking change).
- Unverified email/phone squat documented as intentional contract (FLOWS + TO-DO Принято + `IUserService` remarks).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H41 `FormCallbackUrl` for Apple OAuth | `ExternalLoginOptions` + `ResolveRedirectUri` (auth URL + token exchange); test asserts Apple `redirect_uri` |
| ✅ #M131 Register unverified squat docs | FLOWS `main.Register` + TO-DO Принято + `IUserService` XML |

---

## Что в библиотеке уже нормально

- Non-Apple providers still use `CallbackUrl` only.
- Apple without `FormCallbackUrl` keeps previous single-URL behavior.

---

## Приоритет фиксов

1. **M130** — sync FLOWS Apple note + README with `FormCallbackUrl`.
2. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
