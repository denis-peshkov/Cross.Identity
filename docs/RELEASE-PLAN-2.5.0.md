Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.0` (closed) · **ветка:** `hotfix/register-password-optional` · **база:** `origin/master` (`v2.4.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **20** файлов · **+403 / −240**. Open C/H/M/L пустые.

**CodeRabbit:** `2026-09-14` · pasted agent finding (FLOWS Register Purpose) · 1 finding (Trivial/Info) → все закрыты в этом плане.

**PR:** [#22](https://github.com/denis-peshkov/Cross.Identity/pull/22) (`Make Register Password optional; rename RELEASE-PLAN-to-master`).

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

### Register `Password` optional
Stock `main.Register`: `Password` `required: false`; `CreateUserAsync` не хеширует blank/whitespace → `PasswordPhc = null`. Регистрация без пароля = осознанный product path (дальше OTP / set password на хосте). Клиенты, которые всегда слали пароль, не ломаются.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M91 Register `Password` optional | `main.Register.json` `required: false`; `CreateUserAsync` skip blank → `PasswordPhc` null; flow/user tests; FLOWS/README |
| ✅ #M40 `FLOWS.md` Register `userAccountIdKey` | `UserId` → `UserAccountId` (как в JSON) |
| ✅ #M90 `FLOWS.md` sync vs Definitions | 18 flows; `ChangeAccountEmail` в host-authorize; Notifications = host config no defaults; Jti wording |
| ✅ #M43 duplicate `{{OPTIONAL_CR_OR_NOTES}}` | dismiss: placeholder уже нет в `RELEASE-PLAN.md` |
| ✅ #M92 rename to-master checklist/script | `RELEASE-PLAN-to-master.md` + `release-plan-to-master.mjs`; PR template / CONTRIBUTING — не для контрибьюторов |
| ✅ #L4 commented `IJwtIssuer` | dismiss: в `IJwtTokenService.cs` уже нет |
| ✅ #L6 `HostSuppliedClientContext` XML | dismiss: `<param>` на Ip/UA/Fingerprint уже есть |
| ✅ #L16 markdown table rule | `.cursor/rules/401-markdown.mdc` — short separators / one-space cells |
| ✅ #L17 Register Purpose optional password | `FLOWS.md` Purpose: email + optional password (aligned with JSON) |

---

## Что в библиотеке уже нормально

- Stock `main.Register`: optional `Password`; bag `UserAccountId` в FLOWS согласован с JSON.
- `FLOWS.md` отражает 18 stock flows и host-authorize для `ChangeAccountEmail`.

---

## Приоритет фиксов

_(пусто — релиз `2.5.0` закрыт; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
