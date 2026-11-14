Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.11.0` · **ветка:** `release/lock-channel-email-require-email` · **база:** `origin/master` (`v2.10.0`) · **дата:** `2026-11-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.11.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **10** файлов · **+169 / −5**. Open C/H/M/L пустые.

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

- Reversed prior trade-off (≤2.10): phone-only Register under `LockChannelAsEmail` no longer deferred to `sendCode`; reject at `CreateUserAsync` with explicit `ValidationException`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H40 LockChannelAsEmail require email on Register | `CreateUserAsync` gate; options/IUserService XML; FLOWS; BREAKING 2.10→2.11; `UserServiceTests`; drop obsolete TO-DO Принято sendCode-late-fail |

---

## Что в библиотеке уже нормально

- With lock + email present, create path unchanged; OTP still email-locked after create.
- Phone-only Register remains valid when `LockChannelAsEmail` is `false`.

---

## Приоритет фиксов

Открытых пунктов этой дельты нет. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md) (`M126` send `registered` after verify).
