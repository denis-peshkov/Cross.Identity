---
name: cross-identity-release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md for Cross.Identity from the
  current branch vs master (delta only). Moves leftover open items from the
  previous plan into docs/TO-DO.md (C/H/M/L only) and removes TO-DO entries when
  fixed in a version as `✅ M13 Short title`. Use when drafting release notes,
  release plans, or updating RELEASE-PLAN-*.md / TO-DO.md.
---

# Cross.Identity — release plan from branch delta

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

Только четыре уровня (пустые оставлять):

```markdown
## Критично (безопасность)
---
## Высокий (логика / auth model)
---
## Средний …
## Низкий …
```

1. **Перед/при** сборке `RELEASE-PLAN-X.Y.Z.md` прочитай предыдущий план и `TO-DO.md`.
2. Открытое с предыдущего плана, **не** вошедшее в дельту → **добавь** как `C…`/`H…`/`M…`/`L…` (merge по id).
3. Пункт **закрыт** (`✅ M13 …`) → **удали** из `TO-DO.md` (секцию оставь пустой при отсутствии айтемов).
4. В version plan `TO-DO` не копировать — только ссылка.
5. «Принято» trade-off без open work сюда не класть.

## Re-check (закрытие пунктов в version plan)

1. Закрываемый пункт **убрать** из severity-секций этого `RELEASE-PLAN-X.Y.Z.md`.
2. Добавить в `## Закрыто`:

| # | Суть |
|---|------|
| ✅ M13 GetClaimValue half-validate docs | …детали… |

3. Id **сохранить**; title короткий.
4. Обновить **Приоритет фиксов** плана (только work этой дельты).
5. **Удалить** тот же пункт из `docs/TO-DO.md`, если он там был.
6. **Принято** (trade-off) — не в «Закрыто».

## Workflow

1. **Resolve version + base** — version from user/file; base default `origin/master`.
2. **Collect delta** (required):

```bash
bash .cursor/skills/cross-identity-release-plan/scripts/collect-release-delta.sh \
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
- [ ] «Закрыто» `#` looks like `✅ M13 …` (or legacy `✅ #34 …`)
- [ ] Every «Закрыто» row maps to delta evidence
- [ ] UTF-8 BOM on written plan / TO-DO if new
