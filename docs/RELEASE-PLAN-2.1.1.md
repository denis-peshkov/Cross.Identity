Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по коду, без опоры на предыдущие версии плана.

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
**Средний** — только ⬜ open. **Закрыто** — все ✅ (номера сохраняются).

**CodeRabbit (local CLI, в период hardening):**

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
