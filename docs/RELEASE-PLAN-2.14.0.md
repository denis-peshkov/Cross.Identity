Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.14.0` · **ветка:** `release/oauth-link-conflict-409` · **база:** `origin/master` (`v2.13.0`) · **дата:** `2026-11-19`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.14.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · WT **14** файлов · **+265 / −34**. Open: H55 M141.

**CodeRabbit:**
- не запускался.

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / licensing / auth model)

### H55. Hosts using Cross.ErrorHandlers may get 400, not 409
`ToConflictProblemDetails` → **409**, but `Cross.ErrorHandlers` **7.6.0** (Sample.Api) maps bare `ConflictException` → **400**. Hosts that only rely on the package middleware without calling `ToConflictProblemDetails` will not match BREAKING/FLOWS (409). Document Sample/host wire-up (catch → `ToConflictProblemDetails`) or align ErrorHandlers / Sample.

---

## Средний (противоречия / баги контрактов)

### M141. rest-client still says `ValidationException` for mergeToken
[`rest-client/Sample.Api.http`](../rest-client/Sample.Api.http) + [`http-client.private.env.json.example`](../rest-client/http-client.private.env.json.example) still describe link-conflict as `ValidationException`; update to `ConflictException` / HTTP **409** / `AccountMergeRequired`.

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

- Link-conflict remains throw-after-mint (not 200 outcome); library contract is `ConflictException` (`AccountMergeRequired`) + `ToConflictProblemDetails` → **409**. Ordinary validation stays **406**.
- Pending mint uses `TransactionScopeOption.Suppress` so hosts may keep ambient tx on OAuth callback.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H54 Link conflict → 409 + Suppress mint | `AccountMergeRequired` / `ConflictException` + `ToConflictProblemDetails`; Suppress around AccountMergePendings SaveChanges; FLOWS/README/BREAKING/CHANGELOG |
| ✅ #M142 Link-conflict tests for 409 | `ExternalLoginServiceTests` + `ProblemDetailsFactoryExtensionsTests` assert ConflictException / 409 fields |

---

## Что в библиотеке уже нормально

- Merge confirm path (`MergeAccountsAsync` / `ExternalLoginMerge`) unchanged aside from conflict exception type on mint.
- `ToValidationProblemDetails` still maps FluentValidation → 406.

---

## Приоритет фиксов

1. **H55** — Sample/host: map via `ToConflictProblemDetails` (не полагаться на ErrorHandlers 7.6 → 400).
2. **M141** — rest-client comments / MERGE_TOKEN wording → ConflictException / 409.
3. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
