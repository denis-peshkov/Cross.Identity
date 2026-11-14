Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.11.0` · **ветка:** `release/lock-channel-email-require-email` · **база:** `origin/master` (`v2.10.0`) · **дата:** `2026-11-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.11.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md)
>
> Дельта: `origin/master...HEAD` — LockChannelAsEmail require email on create. Open C/H/M/L пустые.

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

- Reversed prior trade-off (≤2.10): phone-only Register under `LockChannelAsEmail` no longer deferred to `sendCode`; reject at `CreateUserAsync`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H40 LockChannelAsEmail require email on Register | `CreateUserAsync` → `ValidationException`; FLOWS / BREAKING / tests; reverses TO-DO / 2.7 accepted sendCode-late-fail |

---

## Что в библиотеке уже нормально

- OTP resolve with lock still email-only after create; gate moves earlier to create.

---

## Приоритет фиксов

_(пусто.)_
