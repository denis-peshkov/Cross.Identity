Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.19.0` · **ветка:** `release/oauth-link-mode-userid-only` · **база:** `origin/master` (`v2.18.0`) · **дата:** `2026-11-25`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.19.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · WT **4** файлов · **+22 / −16** (incl. plan). Open C/H/M/L пустые.

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

- OAuth Complete link mode = `UserAccountId` present in state only (authenticated link).
- `ReturnUrl` containing `ExternalLogins` (e.g. post-login redirect to settings) is sign-in, not link — no `NotAuthorizedException` for anonymous complete.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H66 Link mode only via `UserAccountId` | Drop `IsExternalLoginLinkReturnUrl`; `isLinking = payload.UserAccountId.HasValue` |
| ✅ #M155 ExternalLogins ReturnUrl sign-in test | Complete without UserAccountId succeeds as sign-in |
| ✅ #L51 CHANGELOG 2.19.0 | Notes for link-mode fix (`release/*` → Minor) |

---

## Что в библиотеке уже нормально

- FLOWS already: `UserAccountId` enables linking when present; omit for normal sign-in / sign-up.
- Authenticated link with `UserAccountId` + merge-conflict path unchanged.
- Missing provider email / always-verified email rules from 2.18.0 unchanged.

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
