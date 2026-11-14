Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.10.0` · **ветка:** `release/registered-template` · **база:** `origin/master` (`v2.9.0`) · **дата:** `2026-11-14`
>
> **Релиз (если есть):** —
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md)
>
> Дельта: `origin/master...HEAD` — rename `register` → `registered`. Open C/H/M/L пустые.

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

- Welcome notify files renamed only; stock `main.Register` still sends OTP via `verify`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H38 Template `register` → `registered` | Stock + Sample.Api files; BREAKING 2.9→2.10 + CHANGELOG `v2.10.0` |

---

## Что в библиотеке уже нормально

- OTP templates (`verify` / `reset` / `*-sms`) and `password-changed` unchanged.

---

## Приоритет фиксов

_(пусто.)_
