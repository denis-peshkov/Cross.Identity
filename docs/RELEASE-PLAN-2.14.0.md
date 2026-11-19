Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.14.0` (closed) · **ветка:** `release/oauth-link-conflict-409` · **база:** `origin/master` (`v2.13.0`) · **дата:** `2026-11-19`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.14.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · committed **14** файлов · **+274 / −34**; + WT → **27** файлов · **+660 / −71** vs `master`. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

**PR:** [#33](https://github.com/denis-peshkov/Cross.Identity/pull/33) (`BREAKING:` map OAuth link conflicts to ConflictException).

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

- Link-conflict remains throw-after-mint (not 200 outcome); library contract is `ConflictException` (`AccountMergeRequired`). HTTP **409** via `Cross.ErrorHandlers` **≥7.7.0** middleware and/or `ToConflictProblemDetails` (structured `mergeToken` body). Ordinary validation stays **406**.
- Pending mint uses `TransactionScopeOption.Suppress` + `IDbContextFactory<IdentityContext>` (fresh connection) so hosts may keep ambient tx on OAuth callback.
- Prefer `AddCrossIdentity(configuration, configureDbContext)`; bare overload still for pooled/custom hosts that register factory themselves.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H54 Link conflict → ConflictException + Suppress mint | `AccountMergeRequired` + throw; FLOWS/README/BREAKING/CHANGELOG |
| ✅ #M142 Link-conflict tests for 409 | Service + `ToConflictProblemDetails` assert ConflictException / 409 fields |
| ✅ #H55 ErrorHandlers **7.7.0** → 409 | Library + Sample bump; ≤7.6 mapped Conflict → 400 |
| ✅ #H56 Suppress mint via `IDbContextFactory` | Factory context inside Suppress; DI overload; PG Testcontainers ambient-tx regression |
| ✅ #L33 EditorConfig charset split | Global `utf-8`; `*.{cs,csproj,sln,slnx}` → `utf-8-bom` |
| ✅ #L34 CONTRIBUTING / Scripts README DI | Point hosts at `AddCrossIdentity(..., configureDbContext)` / factory |

---

## Что в библиотеке уже нормально

- Merge confirm path (`MergeAccountsAsync` / `ExternalLoginMerge`) unchanged aside from conflict exception type on mint.
- `ToValidationProblemDetails` still maps FluentValidation → 406.
- Sample.Api uses `AddCrossIdentity(configuration, UseInMemoryDatabase…)` + ErrorHandlers 7.7.0.

---

## Приоритет фиксов

_(пусто — релиз `2.14.0` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
