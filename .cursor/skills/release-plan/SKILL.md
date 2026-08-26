---
name: release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md from the current branch vs
  master (delta only). Moves leftover open items from the previous plan into
  docs/TO-DO.md (C/H/M/L only). Closing/dismissing a TO-DO item always adds
  `✅ #Id …` to the current plan «Закрыто» first, then removes it from TO-DO.
  Finalizing a version plan moves all remaining open items into TO-DO and
  rewrites the plan to the finalized template. Also maintains the historical
  dev→master checklist (docs/RELEASE-PLAN-dev-to-master.md) via
  scripts/release-plan-summary.mjs. Use when drafting release notes, release
  plans, closing a version plan, or updating RELEASE-PLAN-X.Y.Z.md / TO-DO.md /
  BREAKING.md / dev-to-master checklist.
---

# Release plan from branch delta

## When to use

- User asks for **release notes / RELEASE-PLAN** for the current branch vs `master`
- Update `docs/RELEASE-PLAN-X.Y.Z.md` for a planned or shipping version
- Sync open backlog into [`docs/TO-DO.md`](../../../docs/TO-DO.md)
- Current version plan file is **missing** and must be created before other work can write into it
- User asks to **close / finalize / ship** a version plan (open leftovers → TO-DO, plan → finalized shape)

## Cross-skill references

Другие skills **не** копируют workflow отсюда — одна строка, **одинаковый** формат:

```text
Skill [`release-plan`](SKILL.md) → **Section** · domain hint
```

| Section (heading in this file) | Typical domain hint |
|--------------------------------|---------------------|
| **Ensure current RELEASE-PLAN** | e.g. CR findings → plan, not TO-DO |
| **`docs/BREAKING.md`** | e.g. script names / migration body |
| **Close from TO-DO** | dismiss / won’t-fix |
| **Re-check** | close open item in version plan |
| **Finalize version plan** | ship / leftovers → TO-DO |

From sibling skills use relative link: `[`release-plan`](../release-plan/SKILL.md)`.

**Workflow numbering:** phases `### Phase N — …` (under `## Workflow`, or `## Phase N` for orchestrators). Numbered steps **only when a phase has 2 and more peer steps**; single-step phase = prose directly under the heading (no lone `1.`). **No** `1.` wrapper with a nested sub-list — one flat list or prose.

## Two version-plan files

| File | Contents |
|------|----------|
| `docs/RELEASE-PLAN-X.Y.Z.md` | **Only** this release’s delta vs base (`master`) |
| `docs/TO-DO.md` | Cross-version **open** backlog (C/H/M/L); incremental |

In the plan header: **Предыдущий план** — ссылка **только на непосредственно предыдущий** `docs/RELEASE-PLAN-A.B.C.md` (последний по версии ниже текущего). **Не** перечислять всю цепочку. Если предыдущего нет — `—`.

## Canonical shape (version plan)

**Source of truth for structure:**
- Active (open work): [`templates/RELEASE-PLAN.md`](templates/RELEASE-PLAN.md)
- Closed / finalized: [`templates/RELEASE-PLAN-FINALIZED.md`](templates/RELEASE-PLAN-FINALIZED.md)
- New `docs/BREAKING.md` **From X → Y**: workflow in **`docs/BREAKING.md`** below; snippet [`templates/BREAKING-SECTION.md`](templates/BREAKING-SECTION.md)

Shared placeholder: **`{{REPOSITORY_LINK}}`** — GitHub repo base from `git remote` (`resolve-target-version.sh` → `repository_link`; fallback in `scripts/lib/repository-link.sh`).

Fill placeholders → write `docs/RELEASE-PLAN-X.Y.Z.md`. Do **not** hardcode specific version filenames in this skill.

**Шапка — `Релиз (если есть):`** — `{{REPOSITORY_LINK}}/releases/tag/v{{VERSION}}` (не `—`; `{{REPOSITORY_LINK}}` from `git remote`, см. `resolve-target-version.sh` → `repository_link`).

**When to open an existing `docs/RELEASE-PLAN-X.Y.Z.md` / `docs/TO-DO.md`:**
- **Required (targeted):** before any **state-changing** work on them — merge/open items, close/dismiss, finalize, harvest leftovers, renumber / dedupe checks. Always read the **current** plan + `TO-DO.md` (only sections you will edit).
- **Also allowed (targeted, same turn):** when harvesting leftovers, deduplicating, or checking whether an item was already closed — read the **previous** plan’s open severity sections and only the relevant «Закрыто» rows (by id / meaning). Do **not** load the full history of every version plan.
- **Optional:** if the **template** is ambiguous/unclear (section meaning, «Закрыто» row shape, …) — one targeted glance at a version plan for shape only.
- **Forbidden:** routinely scanning / re-reading generated plans when no state change is involved; treating a generated plan as skill canon or keeping this skill file in sync with every plan output.

Downstream **coderabbit** triage still writes only into the **current** version plan (not `TO-DO.md` for delta findings) and skips duplicates already open in the current plan, any plan «Закрыто», or `TO-DO.md`.

**Legend:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
**Критично / Высокий / Средний / Низкий** — только ⬜ **этой дельты**.
**Закрыто** — столбец `#`: **`✅ #M13 Short title`** / **`✅ #H3 …`** (legacy `✅ #34 …` ok). Без id: **`✅ Short title`**.

Empty severity sections stay as the heading + `---`.

**Нумерация open items** — **общий** namespace с [`docs/TO-DO.md`](../../../docs/TO-DO.md) (не локальный счётчик релиза):

| Префикс | Секция |
|---------|--------|
| `C` | Критично |
| `H` | Высокий |
| `M` | Средний |
| `L` | Низкий |

**Источник max (allocate, без правки `TO-DO.md`):**
`max(Id high-water в шапке TO-DO, id в open + «Закрыто» текущего плана)` по группе → **+ 1**.
**`Id high-water` в `TO-DO.md` не трогать** до **finalize** этого релиза. Исторические id не перенумеровывать / не переиспользовать.

Источник finding’а **не хранить отдельной секцией** — сразу в C/H/M/L open **этого** плана (если относится к дельте) или в `TO-DO.md` (если вне дельты).

## `docs/TO-DO.md` (инкрементально)

Файл = **только нерешённые** пункты **вне** дельты текущего version plan.
Процесс/легенда/статусы **не** писать в сам файл — только здесь.

| Правило | Деталь |
|---------|--------|
| Содержимое | Open backlog вне дельты version plan; планы = только дельта релиза |
| Секции | Всегда четыре: Критично / Высокий / Средний / Низкий; пустые = заголовок + `---` |
| Формат | `### M13. Title` + описание **без** статус-маркеров (`⬜`/`✅`/…) |
| Id | Общий namespace с version plan. Новый id = **max(TO-DO high-water, current plan open+«Закрыто» ids) + 1**. Не gap-fill / не переиспользовать |
| High-water | Строка **`Id high-water`** в шапке TO-DO = якорь **после последнего finalize**. **Писать только при finalize** (`max(старый HW, все id закрываемого релиза)`). Во время релиза high-water **не** обновлять |
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

## Ensure current RELEASE-PLAN

Перед triage (CodeRabbit, issue/PR), close/dismiss, `docs/BREAKING.md`, или любой записью в version plan — **сначала** определить текущий план. См. **Cross-skill references** — ссылка одной строкой, без дублирования ниже.

```bash
bash .cursor/skills/release-plan/scripts/resolve-target-version.sh
bash .cursor/skills/release-plan/scripts/resolve-target-version.sh --json
```

Use `plan_path` / `target_version` from output. Exit **2** → ask user for `X.Y.Z`, retry with `--version`.

| Situation | Action |
|-----------|--------|
| Current `docs/RELEASE-PLAN-X.Y.Z.md` **exists** | Use it (`test -f` / read **only that file** + `docs/TO-DO.md` when needed); `plan_path` from script output |
| **No** current plan for the target version (file missing) | **Must** run this skill **fully** (collect delta → write plan) in this same session, **then** continue |
| Version unknown | Ask user for `X.Y.Z`, or infer from `--version` / branch / `**Версия:**` in the one candidate file — **not** by listing every plan |

**Forbidden:**
- `ls` / glob / read-all of `docs/RELEASE-PLAN-*.md` (incl. `dev-to-master`) to “find current”
- invent a stub plan without this skill’s workflow; skip creating the plan when it is missing
- routinely open historical version plans (only current + TO-DO; previous plan link only when drafting a **new** plan header)

**Bump rules** (script mirrors this; do not copy into other skills):

| Ветка | Bump | Пример (после `2.2.0`) |
|-------|------|-------------------------|
| `release/*` | **minor** (+0.1.0) | `2.3.0` |
| `hotfix/*` | **patch** (+0.0.1) | `2.2.1` |
| merge **`dev` → `master`** | **спросить пользователя** — minor vs patch vs major; не угадывать | n/a (ask first) |

База bump — последний `v*` tag. Порядок: user / `--version` → script → `test -f` на `plan_path`. Script fields for BREAKING: `breaking_from`, `breaking_to`.

**Текущий** `docs/RELEASE-PLAN-X.Y.Z.md` = план **целевой** версии (`target_version` / user / `**Версия:**` in file). Писать закрытия только в **текущий** plan — не в shipped historical plans. Не использовать `RELEASE-PLAN-dev-to-master.md`.

## `docs/BREAKING.md`

Consumer breaking changes. См. **Cross-skill references** — ссылка одной строкой + domain body (имена SQL, API tables, …).

### Phase 1 — Scaffold

1. **Ensure current RELEASE-PLAN** — `breaking_from` / `breaking_to` from script.
2. Scaffold (cache only; **не** пишет в repo):

```bash
bash .cursor/skills/release-plan/scripts/scaffold-breaking-section.sh \
  --out .cursor/skills/release-plan/.cache/breaking-X.Y.Z.md
```

Optional: `--from`, `--to`, `--version`. **`--pr N` required** (or an open PR on the current branch — script resolves via `gh`).

### Phase 2 — Edit `docs/BREAKING.md`

1. Paste TOC row + section **at the top** of versioned blocks (newest-first); fill `{{BODY}}`; prefix PR title `BREAKING:`.
2. **Не** дублировать layout rules in intro `docs/BREAKING.md` — только consumer text.

**Layout** ([`templates/BREAKING-SECTION.md`](templates/BREAKING-SECTION.md) — snippet only):

- **First** block after intro: intro ends with `---` — no extra `---` before heading.
- **Later** blocks: `---` immediately before `## From …` (no blank between `---` and `##`).
- **After** `## From … to …` — blank line, then `Release:` (or `###` if no `Release:`).
- **`Release:`** — `[vX.Y.Z](release-url) ([PR #N](…)).` — version **and** PR always. No `(planned)`, no `See FLOWS.md…`.

When editing `docs/BREAKING.md`, sync dev-to-master checklist if related plan items change (DOC6, §10) — run `release-plan-summary.mjs --write` (see below).

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
2. **Перенести** каждый пункт в [`docs/TO-DO.md`](../../../docs/TO-DO.md) (merge **по id** — один id = одна задача; формат TO-DO **без** `⬜`; секции C/H/M/L сохранить).
   Не класть их в «Закрыто» — это не done/dismiss, а leftover.
3. **Обновить `Id high-water`** в шапке `TO-DO.md` (**единственный** момент записи HW в этом релизе): для каждой группы `max(текущий high-water, все id релиза)` — leftovers + строки «Закрыто» вида `✅ #H9 …` / `✅ #M49 …` / ….
4. **Привести план к завершённому шаблону** [`templates/RELEASE-PLAN-FINALIZED.md`](templates/RELEASE-PLAN-FINALIZED.md):
   - header: версия **published / closed** (+ release URL если есть);
   - C/H/M/L — **пустые** (только заголовок + `---`);
   - **Принято** / **Закрыто** / **Что в библиотеке уже нормально** — сохранить содержимое этого релиза;
   - **Приоритет фиксов** — пустая отсылка к `TO-DO.md` (как в finalized template).
5. Обновить **Приоритет** в `TO-DO.md` при необходимости (новые leftovers).
6. UTF-8 BOM на изменённых docs.
7. В ответе пользователю: список **перенесённых в TO-DO** id, новый high-water, подтверждение что план = finalized shape.

**Запрещено:** оставить ⬜ open в «закрытом» плане; удалить open без переноса в TO-DO; заново сканировать все historical plans без нужды.

## Workflow

### Phase 1 — Resolve & collect

1. **Ensure current RELEASE-PLAN** — см. выше; base default `origin/master`.
2. **Collect delta** (required) — script only; do **not** put the cache path into the version plan file:

```bash
bash .cursor/skills/release-plan/scripts/collect-release-delta.sh \
  --base origin/master \
  --version X.Y.Z
```

Cache lands under `.cursor/skills/release-plan/.cache/` (script prints the path). Use it while drafting; omit from `docs/RELEASE-PLAN-X.Y.Z.md`.

### Phase 2 — Draft plan

1. **Sync `TO-DO.md`** — harvest leftovers into C/H/M/L; drop items closed in this delta / any version «Закрыто».
2. **Write** `docs/RELEASE-PLAN-X.Y.Z.md` from template — **delta only** (UTF-8 **with BOM**).
3. Classify **delta** changes:

| Bucket | Put here |
|--------|----------|
| Критично / Высокий / Средний / Низкий | Open issues **in this delta** only |
| Принято | Trade-offs decided **in this release** |
| Закрыто | Fixes/features **in this delta** |
| Что в библиотеке уже нормально | Short bullets **about this delta’s invariants** |
| Приоритет фиксов | Remaining work **for this release only** (+ link to `TO-DO.md`) |

### Phase 3 — Consumer docs

1. **`docs/BREAKING.md`** — if consumer breaks in this delta: см. **`docs/BREAKING.md`** выше.
2. **Language:** Russian body; table «Суть» may mix RU/EN names.

## Other release docs (not version plans)

| File | Role |
|------|------|
| `RELEASE-PLAN.md` (repo root) | Library audit / hardening backlog |
| `docs/RELEASE-PLAN-dev-to-master.md` | Historical `dev` → `master` readiness checklists |
| `docs/BREAKING.md` | Breaking changes for NuGet consumers |

Version plans (`docs/RELEASE-PLAN-X.Y.Z.md`) and `docs/TO-DO.md` — см. выше. **Не** подменять dev-to-master checklist version plan’ом.

### Root `RELEASE-PLAN.md` status marks

**Легенда:** ⬜ open · ✅ done · 🟨 partial / accepted · ❌ blocker

When closing an item (fixed or accepted):

1. Prefix the heading with green checkbox `✅` (e.g. `### ✅ 22. …`) — do **not** use plain text «закрыто» alone.
2. Add/update the row in **Закрыто** with `✅ #N …`.
3. Drop the item from **Приоритет фиксов** open lists.

### `docs/RELEASE-PLAN-dev-to-master.md` — Checklist Summary

**On any change** to `docs/RELEASE-PLAN-dev-to-master.md` (statuses ⬜/✅/🟨/❌, new items, §8 DB migration, DOC6, breaking changes, release gate, go/no-go) **always** recalculate and update the **"Checklist Summary"** line in the document header (immediately after the legend).

The same applies when editing `docs/BREAKING.md` if it changes the status of related plan items (e.g. DOC6, §10.8, P1 for `collectResult`).

```bash
node .cursor/skills/release-plan/scripts/release-plan-summary.mjs --write
```

Without `--write` — output the line only for verification. Do not edit percentages and counts manually if the script can be run.

**Status legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker

### Breaking changes ↔ dev-to-master plan

| Area | Where in dev-to-master plan |
|------|-----------------------------|
| `docs/BREAKING.md` | DOC6, §2 breaking changes, §10 items 3 and 8, go/no-go |
| SQL / EF migrations | §8 (M1–M4), release gate item 4 |
| `config.nuspec` releaseNotes | DOC4 |

Workflow for new sections: **`docs/BREAKING.md`** (this skill).

## Scripts (`scripts/`)

| Script | Purpose |
|--------|---------|
| [`resolve-target-version.sh`](scripts/resolve-target-version.sh) | `target_version`, `plan_path`, `repository_link`, `breaking_from`/`breaking_to` from branch + latest `v*` tag |
| [`scaffold-breaking-section.sh`](scripts/scaffold-breaking-section.sh) | TOC row + `From X to Y` block (cache only; agent edits `docs/BREAKING.md`) |
| [`collect-release-delta.sh`](scripts/collect-release-delta.sh) | Branch delta cache for plan drafting |
| [`release-plan-summary.mjs`](scripts/release-plan-summary.mjs) | `RELEASE-PLAN-dev-to-master.md` Checklist Summary line |

Other skills: **Cross-skill references** (one-line link; no duplicate scripts or layout prose).

## Quality bar

- [ ] Version plan has **no** foreign-release backlog (that lives in `TO-DO.md`)
- [ ] `TO-DO.md` is C/H/M/L only (no separate review-tool section); closed items removed from it
- [ ] Every removal from `TO-DO.md` has a matching `✅ #Id …` row in **current** plan «Закрыто»
- [ ] «Закрыто» `#` looks like `✅ #M13 …` / `✅ #H3 …` (or legacy `✅ #34 …`)
- [ ] Every «Закрыто» row maps to delta evidence **or** explicit dismiss reason
- [ ] Finalize: leftovers in `TO-DO.md`; **`Id high-water`** обновлён один раз (`≥` все id релиза); plan = `RELEASE-PLAN-FINALIZED`
- [ ] New C/H/M/L ids = max(TO-DO HW, current plan ids) + 1; **no** mid-release HW edits in `TO-DO.md`
- [ ] UTF-8 BOM on written plan / TO-DO if new
- [ ] New `BREAKING.md` sections follow [`templates/BREAKING-SECTION.md`](templates/BREAKING-SECTION.md) (layout not duplicated in consumer intro)
