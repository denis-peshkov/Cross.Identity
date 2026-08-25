Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

**Версия:** `2.3.0`
**Ветка:** `release/fix-missed-issues` · **база:** `origin/master` · **дата:** `2026-08-26`
**Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.3.0

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker

**Предыдущий план:** [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)

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
| ✅ #M51 `main.ChangePassword` collectForm `UserAccountId` | stock JSON / selector / tests; `FLOWS.md`; `BREAKING.md` § [From 2.2.0 to 2.3.0](#from-220-to-230) |

---

## Что в библиотеке уже нормально

- User-scoped flows и token lifecycle — без изменений относительно 2.2.0 (см. [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)).

---

## Приоритет фиксов

_(пусто — открытых пунктов дельты нет.)_
