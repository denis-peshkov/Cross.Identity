Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.1.1` (published / closed) · **ветка:** `release/replace-ConcurrencyStampInterceptor-with-owerride-SaveChanges` · **база:** `origin/master` · **дата:** `2026-08-24`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.1.1
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.0.0.md`](RELEASE-PLAN-2.0.0.md)

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

_(пусто — релиз `2.1.1` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
