---
name: cross-identity-release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md for Cross.Identity from the
  current branch vs master (delta only). Moves leftover open items from the
  previous plan into docs/TO-DO.md (incremental) and removes TO-DO entries when
  fixed in a version as `✅ M13 Short title` (group prefix C/H/M/L/CR). Use when
  drafting release notes, release plans, or updating RELEASE-PLAN-*.md / TO-DO.md.
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
| `docs/TO-DO.md` | Cross-version **open** backlog; incremental |

## Canonical shape (version plan)

Match **section order and headings** of:

- [`docs/RELEASE-PLAN-2.0.0.md`](../../../docs/RELEASE-PLAN-2.0.0.md)
- [`docs/RELEASE-PLAN-2.1.1.md`](../../../docs/RELEASE-PLAN-2.1.1.md)
- [`docs/RELEASE-PLAN-2.2.0.md`](../../../docs/RELEASE-PLAN-2.2.0.md) (delta-only example)

Template: [`templates/RELEASE-PLAN.md`](templates/RELEASE-PLAN.md).

**Legend:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Критично / Высокий / Средний / Низкий** — только ⬜ **этой дельты**.  
**Закрыто** — столбец `#`:  
- из `TO-DO` / severity: **`✅ M13 …`**, **`✅ CRM1 …`**, **`✅ CRL2 …`**
- legacy (как 2.0): **`✅ #34 Session IP binding config`**
Без id: **`✅ Short title`**.

Empty severity sections stay as the heading + `---` (like 2.1.1 / 2.2.0).

**Нумерация в `TO-DO.md` / severity open items:** сквозная **внутри группы** с префиксом:

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий (техдолг, не CR) |

**CodeRabbit → Out** (подгруппы в `## CodeRabbit`; id = **`CR` + наш уровень**, не имя CR-severity):

| CR | Out | Id prefix |
|----|-----|-----------|
| Critical | **C** | `CRC` |
| Major | **H** | `CRH` |
| Minor | **M** | `CRM` |
| Trivial | **L** | `CRL` |
| Info | **L** | `CRL` (общий ряд с Trivial) |

Заголовок: `### CRM1. Title` / `### CRL1. Title`. Закрытие: **`✅ CRM1 …`** / **`✅ CRL2 …`**. Исторические `M13` / `L1` не перенумеровывать.

Подгруппы CodeRabbit (**Critical → C** / **Major → H** / **Minor → M** / **Trivial → L** / **Info → L**) **всегда** присутствуют; пустые — `---`.

## `docs/TO-DO.md` (инкрементально)

Структура:

```markdown
## Критично (безопасность)
---
## Высокий (логика / auth model)
---
## Средний …
## Низкий …
## CodeRabbit
### Critical → C
---
### Major → H
---
### Minor → M
…
### Trivial → L
…
### Info → L
---
```

Пустые уровни / подгруппы **не удалять**.

1. **Перед/при** сборке `RELEASE-PLAN-X.Y.Z.md` прочитай предыдущий план и текущий `TO-DO.md`.
2. Открытое с предыдущего плана, **не** вошедшее в дельту → **добавь** с нужным префиксом (`M…` / `L…` / `CRM…` / `CRL…`); CR finding — в подгруппу по severity CR, id = `CR`+Out; merge по id.
3. Пункт **закрыт** (`✅ M13 …` / `✅ CRM1 …`) → **удали** из `TO-DO.md` (секцию/подгруппу оставь пустой).
4. В version plan содержимое `TO-DO` не копировать — только ссылка.
5. «Принято» trade-off без open work сюда не класть.

## Re-check (закрытие пунктов в version plan)

1. Закрываемый пункт **убрать** из severity-секций этого `RELEASE-PLAN-X.Y.Z.md`.
2. Добавить в `## Закрыто`:

| # | Суть |
|---|------|
| ✅ M13 GetClaimValue half-validate docs | …детали… |
| ✅ CRM1 FLOWS Register bag key | … |
| ✅ CRL1 HostSuppliedClientContext XML | … |

3. Id **сохранить** (`M13`, `CRM1`, `CRL1`, …); title короткий. CR-пункт → severity **Out** (CRM→Средний, CRL→Низкий).
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

3. **Sync `TO-DO.md`** — harvest open leftovers from previous plan; drop items closed in this delta / any version «Закрыто».
4. **Write** `docs/RELEASE-PLAN-X.Y.Z.md` from template — **delta only** (UTF-8 **with BOM**).
5. Classify **delta** changes:

| Bucket | Put here |
|--------|----------|
| Критично / Высокий / Средний / Низкий | Open issues **introduced or still open in this delta** only |
| Принято | Trade-offs decided **in this release** |
| Закрыто | Fixes/features **in this delta** |
| Что в библиотеке уже нормально | Short bullets **about this delta’s invariants** (not full library résumé) |
| Приоритет фиксов | Remaining work **for this release only** (+ link to `TO-DO.md`) |

6. If `docs/BREAKING.md` needs **From A.B → X.Y.Z** for consumer breaks in the delta — say so / offer to append.
7. **Language:** Russian body; table «Суть» may mix RU/EN names.

## Quality bar

- [ ] Version plan has **no** foreign-release backlog (that lives in `TO-DO.md`)
- [ ] `TO-DO.md` updated incrementally; closed-in-version items **removed** from it
- [ ] «Закрыто» `#` looks like `✅ M13 …` / `✅ CRM1 …` / `✅ CRL1 …` (or legacy `✅ #34 …`)
- [ ] `TO-DO.md` keeps C/H/M/L + CodeRabbit subgroups; CR ids are `CR`+Out (`CRC`/`CRH`/`CRM`/`CRL`)
- [ ] Every «Закрыто» row maps to delta evidence
- [ ] UTF-8 BOM on written plan / TO-DO if new
