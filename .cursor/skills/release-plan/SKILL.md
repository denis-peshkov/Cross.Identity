---
name: release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md from the current branch vs
  master (delta only). Moves leftover open items from the previous plan into
  docs/TO-DO.md (C/H/M/L only). Closing/dismissing a TO-DO item always adds
  `✅ Id …` to the current plan «Закрыто» first, then removes it from TO-DO.
  Use when drafting release notes, release plans, or updating RELEASE-PLAN-*.md
  / TO-DO.md.
---

# Release plan from branch delta

## When to use

- User asks for **release notes / RELEASE-PLAN** for the current branch vs `master`
- Update `docs/RELEASE-PLAN-X.Y.Z.md` for a planned or shipping version
- Sync open backlog into [`docs/TO-DO.md`](../../../docs/TO-DO.md)

**Do not** use for the historical merge checklist `docs/RELEASE-PLAN-dev-to-master.md` (different format; see `docs/scripts/release-plan-summary.mjs`).

## Two files

| File | Contents |
|------|----------|
| `docs/RELEASE-PLAN-X.Y.Z.md` | **Only** this release’s delta vs base (`master`) |
| `docs/TO-DO.md` | Cross-version **open** backlog (C/H/M/L); incremental |

## Canonical shape (version plan)

Match **section order and headings** of:

- [`docs/RELEASE-PLAN-2.0.0.md`](../../../docs/RELEASE-PLAN-2.0.0.md)
- [`docs/RELEASE-PLAN-2.1.1.md`](../../../docs/RELEASE-PLAN-2.1.1.md)
- [`docs/RELEASE-PLAN-2.2.0.md`](../../../docs/RELEASE-PLAN-2.2.0.md) (delta-only example)

Template: [`templates/RELEASE-PLAN.md`](templates/RELEASE-PLAN.md).

**Legend:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Критично / Высокий / Средний / Низкий** — только ⬜ **этой дельты**.  
**Закрыто** — столбец `#`: **`✅ M13 Short title`** (или legacy `✅ #34 …`). Без id: **`✅ Short title`**.

Empty severity sections stay as the heading + `---` (like 2.1.1 / 2.2.0).

**Нумерация open items:** сквозная **внутри группы**:

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий |

Источник finding’а (CodeRabbit, audit, …) **не хранить отдельной секцией** — сразу в C/H/M/L.  
Triage CR severity → Out: Critical→`C`, Major→`H`, Minor→`M`, Trivial/Info→`L`.

## `docs/TO-DO.md` (инкрементально)

Файл = **только нерешённые** пункты. Процесс/легенда/статусы **не** писать в сам файл — только здесь (и в coderabbit skill для CR triage).

| Правило | Деталь |
|---------|--------|
| Содержимое | Open backlog вне дельты version plan; планы = только дельта релиза |
| Секции | Всегда четыре: Критично / Высокий / Средний / Низкий; пустые = заголовок + `---` |
| Формат | `### M13. Title` + описание **без** статус-маркеров (`⬜`/`✅`/…) |
| Id | Сквозная нумерация **внутри** группы `C`/`H`/`M`/`L`; следующий свободный; исторические id не перенумеровывать |
| Источник | CodeRabbit / audit / … — сразу в C/H/M/L (без отдельной секции CR) |
| CR triage | Critical→`C`, Major→`H`, Minor→`M`, Trivial/Info→`L` |
| Harvest | Open с предыдущего плана, не вошедшее в дельту → добавить сюда (если ещё нет) |
| Close | См. **Close from TO-DO** — сначала «Закрыто» текущего плана, потом удалить из TO-DO |
| Не класть | «Принято» trade-off без open work; копипаст всего TO-DO в version plan (только ссылка) |

```markdown
## Критично (безопасность)
---
## Высокий (логика / auth model)
---
## Средний …
## Низкий …
```

## Current version plan

**Текущий** `docs/RELEASE-PLAN-X.Y.Z.md` = план **целевой** версии ветки (user / `**Версия:**` в файле / planned).  
Не писать закрытия в уже shipped historical планы (`2.0.0`, `2.1.1`, …), если работа идёт под следующий релиз (напр. `2.2.0`).  
Не использовать `RELEASE-PLAN-dev-to-master.md`.

## Close from TO-DO (обязательно, любой dismiss)

Когда пункт убирают из `docs/TO-DO.md` (fix, won’t-fix, CR dismiss, «это только пример», duplicate, …):

1. **Сначала** добавить строку в `## Закрыто` **текущего** `RELEASE-PLAN-X.Y.Z.md`:

| # | Суть |
|---|------|
| ✅ H2 Scripts README MERGE SystemId scope | CR dismissed: README — пример lookup, не open work |

2. Id **сохранить** (`✅ H2 …` / `✅ M13 …`); title короткий; в «Суть» — почему закрыто.
3. **Затем** удалить пункт из `docs/TO-DO.md` (пустые C/H/M/L-секции оставить).
4. Обновить **Приоритет** в TO-DO / плане при необходимости.
5. **Запрещено:** удалить из TO-DO без строки в «Закрыто» текущего плана.

Осознанный **контрактный** trade-off без id → секция **Принято** (не «Закрыто», не TO-DO).  
Id’шный backlog, который отклонили → всё равно **Закрыто** с `✅ Id …` и причиной.

## Re-check (закрытие пунктов в version plan)

1. Закрываемый пункт **убрать** из severity-секций этого `RELEASE-PLAN-X.Y.Z.md` (если был open в дельте).
2. Добавить в `## Закрыто` (тот же формат `✅ M13 …`).
3. Id **сохранить**; title короткий.
4. Обновить **Приоритет фиксов** плана (только work этой дельты).
5. **Удалить** тот же пункт из `docs/TO-DO.md`, если он там был (после шага 2).
6. То же правило, что **Close from TO-DO**: нельзя только выкинуть из TO-DO.

## Workflow

1. **Resolve version + base** — version from user/file; base default `origin/master`.
2. **Collect delta** (required):

```bash
bash .cursor/skills/release-plan/scripts/collect-release-delta.sh \
  --base origin/master \
  --version 2.2.0
```

3. **Sync `TO-DO.md`** — harvest leftovers into C/H/M/L; drop items closed in this delta / any version «Закрыто».
4. **Write** `docs/RELEASE-PLAN-X.Y.Z.md` from template — **delta only** (UTF-8 **with BOM**).
5. Classify **delta** changes:

| Bucket | Put here |
|--------|----------|
| Критично / Высокий / Средний / Низкий | Open issues **in this delta** only |
| Принято | Trade-offs decided **in this release** |
| Закрыто | Fixes/features **in this delta** |
| Что в библиотеке уже нормально | Short bullets **about this delta’s invariants** |
| Приоритет фиксов | Remaining work **for this release only** (+ link to `TO-DO.md`) |

6. If `docs/BREAKING.md` needs **From A.B → X.Y.Z** for consumer breaks — say so / offer to append.
7. **Language:** Russian body; table «Суть» may mix RU/EN names.

## Quality bar

- [ ] Version plan has **no** foreign-release backlog (that lives in `TO-DO.md`)
- [ ] `TO-DO.md` is C/H/M/L only (no separate CR section); closed items removed from it
- [ ] Every removal from `TO-DO.md` has a matching `✅ Id …` row in **current** plan «Закрыто»
- [ ] «Закрыто» `#` looks like `✅ M13 …` (or legacy `✅ #34 …`)
- [ ] Every «Закрыто» row maps to delta evidence **or** explicit dismiss reason
- [ ] UTF-8 BOM on written plan / TO-DO if new
