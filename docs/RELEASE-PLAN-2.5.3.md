Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.3` (published / closed) · **ветка:** `master` · **база:** `v2.5.2` · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.3
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.2.md`](RELEASE-PLAN-2.5.2.md)
>
> Дельта: `v2.5.2...v2.5.3` — **1** коммит · **1** файл · **+14 / −2**. Open C/H/M/L пустые.

**CodeRabbit:** —

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

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #L20 `401-markdown` YAML globs | Rule applies to `.yaml` / `.yml` (incl. commented tables, e.g. `.coderabbit.yaml`); empty-cell guidance |

---

## Что в библиотеке уже нормально

- Дельта **не** трогает library / NuGet API — только расширение scope `.cursor/rules/401-markdown.mdc`.

---

## Приоритет фиксов

_(пусто — релиз `2.5.3` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
