Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.2` (published / closed) · **ветка:** `master` · **база:** `v2.5.1` · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.2
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.1.md`](RELEASE-PLAN-2.5.1.md)
>
> Дельта: `v2.5.1...v2.5.2` — **1** коммит · **24** файла · **+155 / −153**. Open C/H/M/L пустые.

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
| ✅ #M97 Markdown table syntax standardize | Short separators / one-space cells across docs, skills, FLOWS, BREAKING, triage, CONTRIBUTING, README |

---

## Что в библиотеке уже нормально

- Дельта **не** трогает library / NuGet API — только Markdown table formatting в docs/skills.
- Правило `401-markdown` уже задавало short separators; релиз выровнял исторические таблицы под него.

---

## Приоритет фиксов

_(пусто — релиз `2.5.2` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
