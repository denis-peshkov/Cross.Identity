---
name: release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md from the current branch vs
  master (delta only). Moves leftover open items from the previous plan into
  docs/TO-DO.md (C/H/M/L only). Closing/dismissing a TO-DO item always adds
  `✅ #Id …` to the current plan «Закрыто» first, then removes it from TO-DO.
  Finalizing a version plan moves all remaining open items into TO-DO and
  rewrites the plan to the finalized template. Use when drafting release notes,
  release plans, closing a version plan, or updating RELEASE-PLAN-X.Y.Z.md /
  TO-DO.md.
---

# Release plan from branch delta

## When to use

- User asks for **release notes / RELEASE-PLAN** for the current branch vs `master`
- Update `docs/RELEASE-PLAN-X.Y.Z.md` for a planned or shipping version
- Sync open backlog into [`docs/TO-DO.md`](../../../docs/TO-DO.md)
- Current version plan file is **missing** and must be created before other work can write into it
- User asks to **close / finalize / ship** a version plan (open leftovers → TO-DO, plan → finalized shape)

**Do not** use for the historical merge checklist `docs/RELEASE-PLAN-dev-to-master.md` (different format; see `docs/scripts/release-plan-summary.mjs`).

## Two files

| File | Contents |
|------|----------|
| `docs/RELEASE-PLAN-X.Y.Z.md` | **Only** this release’s delta vs base (`master`) |
| `docs/TO-DO.md` | Cross-version **open** backlog (C/H/M/L); incremental |

In the plan header: **Предыдущий план** — ссылка **только на непосредственно предыдущий** `docs/RELEASE-PLAN-A.B.C.md` (последний по версии ниже текущего). **Не** перечислять всю цепочку. Если предыдущего нет — `—`.

## Canonical shape (version plan)

**Source of truth for structure:**
- Active (open work): [`templates/RELEASE-PLAN.md`](templates/RELEASE-PLAN.md)
- Closed / finalized: [`templates/RELEASE-PLAN-FINALIZED.md`](templates/RELEASE-PLAN-FINALIZED.md)

Fill placeholders → write `docs/RELEASE-PLAN-X.Y.Z.md`. Do **not** hardcode specific version filenames in this skill.

**When to open an existing `docs/RELEASE-PLAN-X.Y.Z.md` / `docs/TO-DO.md`:**
- **Required (targeted):** before any **state-changing** work on them — merge/open items, close/dismiss, finalize, harvest leftovers, renumber checks. Read only the **current** plan + `TO-DO.md` (and only the sections you will edit).
- **Optional:** if the **template** is ambiguous/unclear (section meaning, «Закрыто» row shape, …) — one targeted glance at a version plan for shape only.
- **Forbidden:** routinely scanning / re-reading generated plans when no state change is involved; treating a generated plan as skill canon or keeping this skill file in sync with every plan output.

**Legend:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Критично / Высокий / Средний / Низкий** — только ⬜ **этой дельты**.  
**Закрыто** — столбец `#`: **`✅ #M13 Short title`** / **`✅ #H3 …`** (legacy `✅ #34 …` ok). Без id: **`✅ Short title`**.

Empty severity sections stay as the heading + `---`.

**Нумерация open items:** сквозная **внутри группы**:

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий |

Источник finding’а **не хранить отдельной секцией** — сразу в C/H/M/L open **этого** плана (если относится к дельте) или в `TO-DO.md` (если вне дельты).

## `docs/TO-DO.md` (инкрементально)

Файл = **только нерешённые** пункты **вне** дельты текущего version plan.  
Процесс/легенда/статусы **не** писать в сам файл — только здесь.

| Правило | Деталь |
|---------|--------|
| Содержимое | Open backlog вне дельты version plan; планы = только дельта релиза |
| Секции | Всегда четыре: Критично / Высокий / Средний / Низкий; пустые = заголовок + `---` |
| Формат | `### M13. Title` + описание **без** статус-маркеров (`⬜`/`✅`/…) |
| Id | Сквозная нумерация **внутри** группы `C`/`H`/`M`/`L`; следующий свободный; исторические id не перенумеровывать |
| Источник | leftover plan / audit вне дельты / … — сразу в C/H/M/L |
| Harvest | Open с предыдущего плана, не вошедшее в дельту → добавить сюда (если ещё нет) |
| Close | См. **Close from TO-DO** — сначала «Закрыто» текущего плана, потом удалить из TO-DO |
| Не класть | Work **этой** дельты (оно в version plan); «Принято» trade-off без open work; копипаст всего TO-DO в version plan (только ссылка) |

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
Писать закрытия только в **текущий** (целевой) `docs/RELEASE-PLAN-X.Y.Z.md` — не в уже shipped historical version plans других `X.Y.Z`.  
Не использовать `RELEASE-PLAN-dev-to-master.md`.

**Resolve without scanning history:**
- Prefer an explicit version from the user / branch / package.
- Confirm with `test -f docs/RELEASE-PLAN-X.Y.Z.md` (or read that one path).
- For triage/close: open **only** the current plan and `docs/TO-DO.md` (targeted, before state change — see above).
- **Forbidden:** `ls docs/RELEASE-PLAN-*.md`, reading every version plan, or using `dev-to-master` to discover “current”.

## Close from TO-DO (обязательно, любой dismiss)

Когда пункт убирают из `docs/TO-DO.md` (fix, won’t-fix, dismiss, «это только пример», duplicate, …):

1. **Сначала** добавить строку в `## Закрыто` **текущего** `RELEASE-PLAN-X.Y.Z.md`:

| # | Суть |
|---|------|
| ✅ #H2 Scripts README MERGE SystemId scope | dismissed: README — пример lookup, не open work |

2. Id **сохранить** (`✅ #H2 …` / `✅ #M13 …`); title короткий; в «Суть» — почему закрыто.
3. **Затем** удалить пункт из `docs/TO-DO.md` (пустые C/H/M/L-секции оставить).
4. Обновить **Приоритет** в TO-DO / плане при необходимости.
5. **Запрещено:** удалить из TO-DO без строки в «Закрыто» текущего плана.

Осознанный **контрактный** trade-off без id → секция **Принято** (не «Закрыто», не TO-DO).  
Id’шный backlog, который отклонили → всё равно **Закрыто** с `✅ #Id …` и причиной.

## Re-check (закрытие пунктов в version plan)

1. Закрываемый пункт **убрать** из severity-секций этого `RELEASE-PLAN-X.Y.Z.md` (если был open в дельте).
2. Добавить в `## Закрыто` (тот же формат `✅ #M13 …` / `✅ #H3 …`).
3. Id **сохранить**; title короткий.
4. Обновить **Приоритет фиксов** плана (только work этой дельты).
5. **Удалить** тот же пункт из `docs/TO-DO.md`, если он там был (после шага 2).
6. То же правило, что **Close from TO-DO**: нельзя только выкинуть из TO-DO.

## Finalize version plan (закрытие релиза)

Когда пользователь просит **закрыть / финализировать / ship** `docs/RELEASE-PLAN-X.Y.Z.md` (релиз вышел или план этой версии больше не ведётся):

1. **Собрать весь ⬜ open** из секций Критично / Высокий / Средний / Низкий этого плана.
2. **Перенести** каждый пункт в [`docs/TO-DO.md`](../../../docs/TO-DO.md) (merge по id; формат TO-DO **без** `⬜`; секции C/H/M/L сохранить).  
   Не класть их в «Закрыто» — это не done/dismiss, а leftover.
3. **Привести план к завершённому шаблону** [`templates/RELEASE-PLAN-FINALIZED.md`](templates/RELEASE-PLAN-FINALIZED.md):
   - header: версия **published / closed** (+ release URL если есть);
   - C/H/M/L — **пустые** (только заголовок + `---`);
   - **Принято** / **Закрыто** / **Что в библиотеке уже нормально** — сохранить содержимое этого релиза;
   - **Приоритет фиксов** — пустая отсылка к `TO-DO.md` (как в finalized template).
4. Обновить **Приоритет** в `TO-DO.md` при необходимости (новые leftovers).
5. UTF-8 BOM на изменённых docs.
6. В ответе пользователю: список **перенесённых в TO-DO** id и подтверждение, что план = finalized shape.

**Запрещено:** оставить ⬜ open в «закрытом» плане; удалить open без переноса в TO-DO; заново сканировать все historical plans без нужды.

## Workflow

1. **Resolve version + base** — version from user/file; base default `origin/master`.
2. **Collect delta** (required):

```bash
bash .cursor/skills/release-plan/scripts/collect-release-delta.sh \
  --base origin/master \
  --version X.Y.Z
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

6. If `docs/BREAKING.md` needs **From A.B → X.Y.Z** for consumer breaks — say so / offer to insert **at the top** (newest-first).
7. **Language:** Russian body; table «Суть» may mix RU/EN names.

## Quality bar

- [ ] Version plan has **no** foreign-release backlog (that lives in `TO-DO.md`)
- [ ] `TO-DO.md` is C/H/M/L only (no separate review-tool section); closed items removed from it
- [ ] Every removal from `TO-DO.md` has a matching `✅ #Id …` row in **current** plan «Закрыто»
- [ ] «Закрыто» `#` looks like `✅ #M13 …` / `✅ #H3 …` (or legacy `✅ #34 …`)
- [ ] Every «Закрыто» row maps to delta evidence **or** explicit dismiss reason
- [ ] Finalize: all former open items are in `TO-DO.md`; plan matches `RELEASE-PLAN-FINALIZED` (empty C/H/M/L + priority → TO-DO)
- [ ] UTF-8 BOM on written plan / TO-DO if new
