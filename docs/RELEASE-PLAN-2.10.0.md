Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.10.0` (closed) · **ветка:** `release/registered-template` · **база:** `origin/master` (`v2.9.0`) · **дата:** `2026-11-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.10.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md)
>
> Дельта: `origin/master...HEAD` — **8** коммитов · **38** файлов · **+205 / −1 541**. Open C/H/M/L пустые.

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

- Welcome notify files renamed `register` → `registered` only; stock `main.Register` still sends OTP via `verify`.
- Stock `confirm-email.*` removed — unused by stock flows; hosts that relied on package copies must ship their own.
- Sample.Api no longer ships host overrides for `registered.*` (use stock embedded templates).
- Send welcome `registered` after OTP verify (non-SMS) deferred → TO-DO `M126` (out of this ship slice).
- README TFMs: badge only; prose/section Supported TFMs dropped as duplicate of the badge.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H38 Template `register` → `registered` | Stock embedded files + lookup key; BREAKING 2.9→2.10 + CHANGELOG |
| ✅ #H39 Drop stock `confirm-email.*` | All locales removed; unused by stock flows |
| ✅ #M127 Sample.Api drop `registered.*` overrides | Host copies removed after rename |
| ✅ #M128 Placeholder tests for templates | `registered` asserted; `confirm-email` coverage dropped |
| ✅ #L24 README Supported TFMs removed in delta | dismissed: TFM badge replaces prose/section (duplicate) |
| ✅ #M129 FLOWS notification placeholders | Document brand vs per-step placeholders; `registered` not sent by stock yet |

---

## Что в библиотеке уже нормально

- OTP templates (`verify` / `reset` / `*-sms`) and `password-changed` unchanged.
- Form / Register flow still gates on `verify` OTP; rename does not change send path.

---

## Приоритет фиксов

_(пусто — релиз `2.10.0` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
