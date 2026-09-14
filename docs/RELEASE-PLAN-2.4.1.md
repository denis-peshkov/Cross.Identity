Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.4.1` · **ветка:** `hotfix/register-password-optional` · **база:** `origin/master` (`v2.4.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.1
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · **0** файлов (ветка = tip `master` / `v2.4.0`). **WT:** **20** файлов · **+406 / −240**. Open C/H/M/L пустые.

**CodeRabbit:** не запускался.

**PR:** —

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

---

## Что в библиотеке уже нормально

- Stock `main.Register`: optional `Password`; bag `UserAccountId` в FLOWS согласован с JSON.
- `FLOWS.md` отражает 18 stock flows и host-authorize для `ChangeAccountEmail`.

---

## Приоритет фиксов

_(пусто — открытых пунктов дельты нет; внерелизный backlog → [`TO-DO.md`](TO-DO.md).)_

Ship: commit WT · PR → `master` · CI · tag `v2.4.1`.
