Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.12.0` · **ветка:** `release/apple-form-callback-url` · **база:** `origin/master` (`v2.11.0`) · **дата:** `2026-11-17`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.12.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **6** файлов · **+34 / −9**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался. → все закрыты в этом плане.

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

- `FormCallbackUrl` optional: empty → Apple uses `CallbackUrl` (backward compatible; not a NuGet breaking change).
- Unverified email/phone squat documented as intentional contract (FLOWS + TO-DO Принято + `IUserService` remarks).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H41 `FormCallbackUrl` for Apple OAuth | `ExternalLoginOptions` + `ResolveRedirectUri` (auth URL + token exchange); test asserts Apple `redirect_uri` |
| ✅ #M130 FLOWS / README `FormCallbackUrl` | Apple note + External OAuth bullet: `FormCallbackUrl` + fallback to `CallbackUrl` |
| ✅ #M131 Register unverified squat docs | FLOWS `main.Register` + TO-DO Принято + `IUserService` XML |
| ✅ #M132 Apple token `redirect_uri` tests | CR: assert `ExchangeCodeAsync` uses `FormCallbackUrl`; blank → `CallbackUrl` for authorize + token |
| ✅ #H42 Register `sendCode` bind to createUser id | Optional `userAccountIdKey`; stock Register → `createUser.UserAccountId`; FLOWS + tests |

---

## Что в библиотеке уже нормально

- Non-Apple providers still use `CallbackUrl` only.
- Apple without `FormCallbackUrl` keeps previous single-URL behavior.
- OTP `VerifyAsync` already takes `userId` (code row scoped).
- RequestCode / ForgotPassword `sendCode` omit `userAccountIdKey` → selector lookup unchanged.

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
