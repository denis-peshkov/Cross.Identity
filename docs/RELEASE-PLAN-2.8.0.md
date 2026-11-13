Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.8.0` (closed) · **ветка:** `release/sms-otp-templates` · **база:** `origin/master` (`v2.7.0`) · **дата:** `2026-11-13`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.8.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md)
>
> Дельта: `origin/master...HEAD` — **7** коммитов · **76** файлов · **+2 382 / −693**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

**PR:** [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) (`Add short SMS OTP templates and DE/PL/UA email locales`).

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

- SMS OTP bodies — short single-segment (`verify-sms` / `reset-sms`); email keeps long HTML/txt.
- `SendCodeStep` appends `-sms` automatically when channel is SMS (idempotent if already `-sms`).
- Sample.Api drops Romanian `register`/`verify` overrides; stock library still ships `register.ro` / `verify.ro`.
- Ukrainian template files stay `*.ua.*`; ISO `uk` maps to `ua` (HTML `lang="uk"` unchanged).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M115 `SendCodeStep` `{Template}-sms` | SMS channel resolves short template; action URL uses base name |
| ✅ #M116 Stock `verify-sms` / `reset-sms` | Embedded short OTP templates (en/ru/de/pl/ua/ro) |
| ✅ #M117 Email locales DE/PL/UA | register / verify / reset / confirm-email / password-changed |
| ✅ #M118 Sample RO register/verify removed | Drop Sample.Api `register.ro` / `verify.ro`; add `verify-sms` stubs |
| ✅ #H33 README/CONTRIBUTING LicenseKey+Pepper | Required host LicenseKey + PepperVault wiring documented |
| ✅ #H34 BREAKING SMS template contract | `docs/BREAKING.md` From 2.7.0→2.8.0 + CHANGELOG `v2.8.0` |
| ✅ Tests `SendCode` SMS path | Asserts `verify-sms` load / body; never email `verify` |
| ✅ #M119 README LicenseKey log levels | Table: null → Critical; empty/invalid JWT → Error + Critical |
| ✅ #M120 README/CONTRIBUTING LicenseKey prod vs dev | Production requires key; dev/test soft-fail; Pepper always required |
| ✅ #M121 Map ISO `uk` → template key `ua` | `ResolveLanguageCode` alias; FLOWS + unit tests; `.ua.*` unchanged |
| ✅ #L23 SMS-OTP-TEMPLATES segment encoding | Columns GSM-7 / Unicode; note extension chars ×2 |
| ✅ #L22 FLOWS.md `sendCode` `-sms` | kind reference documents SMS `{template}-sms` resolve + base-name action URL |

---

## Что в библиотеке уже нормально

- Template language selection via `HostSuppliedLanguageContext` / fallback `en` unchanged.
- Flow JSON still passes `template: verify|reset`; SMS suffix is a channel-side resolve, not a flow JSON change.

---

## Приоритет фиксов

_(пусто — релиз `2.8.0` закрыт; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
