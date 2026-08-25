# Release plan — Cross.Identity **2.1.1**

Release: [v2.1.1](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.1.1) ([PR #18](https://github.com/denis-peshkov/Cross.Identity/pull/18)).
Scope: `ConcurrencyStamp` rotation in `IdentityContext.SaveChanges` (drop interceptor). Consumer notes: [`BREAKING.md`](BREAKING.md) § From 2.0.x to 2.1.1.

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
There was no `2.1.0` package.

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off / контракт хоста)

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ `ConcurrencyStamp` rotation | `ConcurrencyStampInterceptor` + `OnConfiguring` **removed**; rotation in `IdentityContext.SaveChanges` / `SaveChangesAsync` |
| ✅ Pooled DbContext | `AddDbContextPool` / `AddPooledDbContextFactory` supported (no auto-`AddInterceptors` in `OnConfiguring`) |
| ✅ Bulk concurrency docs | `ExecuteUpdateAsync` / `ExecuteDeleteAsync` bypass SaveChanges — filter by original stamp, check affected rows; new stamp only via `SetProperty` |
| ✅ BREAKING.md § 2.1.1 | append-only section for NuGet consumers |

---

## Что в библиотеке уже нормально

- Stamp rotation on tracked `SaveChanges` / `SaveChangesAsync` for entities with `IHasConcurrencyStamp`.
- Host does not need `.AddInterceptors(…ConcurrencyStampInterceptor…)`.

---

## Приоритет фиксов

_(пусто — релиз 2.1.1 опубликован; открытый backlog → [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md).)_
