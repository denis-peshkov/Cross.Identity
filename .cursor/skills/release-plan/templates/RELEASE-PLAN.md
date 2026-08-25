Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

**Версия:** `{{VERSION}}`
**Ветка:** `{{BRANCH}}` · **база:** `{{BASE}}` · **дата:** `{{DATE}}`
**Релиз (если есть):** {{REPOSITORY_LINK}}/releases/tag/v{{VERSION}}

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker

**Предыдущий план:** {{PREVIOUS_PLAN_LINK_OR_DASH}}

{{OPTIONAL_NOTES}}

---

## Критично (безопасность)

{{CRITICAL_OR_EMPTY}}

---

## Высокий (логика / auth model)

{{HIGH_OR_EMPTY}}

---

## Средний (противоречия / баги контрактов)

{{MEDIUM_OR_EMPTY}}

---

## Низкий (техдолг / несогласованности)

{{LOW_OR_EMPTY}}

---

## Принято (осознанный trade-off / контракт хоста)

{{ACCEPTED_OR_EMPTY}}

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ #{{CLOSED_ID}} {{CLOSED_TITLE}} | {{CLOSED_SUMMARY}} |

---

## Что в библиотеке уже нормально

{{ALREADY_OK_BULLETS}}

---

## Приоритет фиксов

{{PRIORITY_OR_EMPTY}}
