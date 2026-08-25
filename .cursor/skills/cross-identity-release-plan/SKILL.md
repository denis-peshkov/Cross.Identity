---
name: cross-identity-release-plan
description: >-
  Builds or refreshes docs/RELEASE-PLAN-X.Y.Z.md for Cross.Identity from the
  current branch vs master (or another base), using the same severity sections
  as RELEASE-PLAN-2.0.0 / 2.1.1. On re-check, moves closed items out of severity
  lists into «Закрыто» as `✅ #34 Session IP binding config` (numbers kept). Use when
  drafting release notes, release plans, or updating RELEASE-PLAN-*.md for an
  upcoming NuGet version.
---

# Cross.Identity — release plan from branch delta

## When to use

- User asks for **release notes / RELEASE-PLAN** for the current branch vs `master`
- Update `docs/RELEASE-PLAN-X.Y.Z.md` for a planned or shipping version
- Split or refresh versioned plans after hardening / CR / feature work

**Do not** use for the historical merge checklist `docs/RELEASE-PLAN-dev-to-master.md` (different format; see `docs/scripts/release-plan-summary.mjs`).

## Canonical shape

Match **section order and headings** of:

- [`docs/RELEASE-PLAN-2.0.0.md`](../../../docs/RELEASE-PLAN-2.0.0.md) (filled «Принято» / «Закрыто»)
- [`docs/RELEASE-PLAN-2.1.1.md`](../../../docs/RELEASE-PLAN-2.1.1.md) (slim closed release)

Template: [`templates/RELEASE-PLAN.md`](templates/RELEASE-PLAN.md).

**Legend:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Критично / Высокий / Средний / Низкий** — только открытые (⬜).  
**Закрыто** — только ✅; в столбце `#` формат как в 2.0.0: **`✅ #34 Session IP binding config`** (`✅` + `#N` + краткий title). Без номера: **`✅ краткий title`**. Номера сохраняются.

Empty severity sections stay as the heading + `---` (like 2.1.1).

## Re-check (закрытие пунктов)

При **перепроверке** плана (повторный прогон скилла, audit, CR, ручной review):

1. Если пункт **закрывается** (исправлен в коде / verified) — **убрать** его из секции критичности (`## Критично` / `## Высокий` / `## Средний` / `## Низкий`, включая вложенные списки вроде CodeRabbit minor).
2. **Добавить** строку в `## Закрыто (проверено в коде)`:

| # | Суть |
|---|------|
| ✅ #34 Session IP binding config | `Authentication:Jwt:SessionBindingCheckIp`; default `false` … |

3. Столбец `#`: **`✅ #N Short title`** — номер **тот же**, что был у open-пункта; title — короткое имя (как `#34 Session IP binding config`). Без стабильного номера: `✅ Short title` (не выдумывать `#`).
4. Столбец `Суть` — детали фикса / где проверено (не дублировать весь title).
5. Пункт **не** дублировать: после переноса его нет в severity-секциях и нет повторной строки в «Закрыто».
6. Обновить **Приоритет фиксов** — убрать закрытые номера из списка.
7. **Принято** (trade-off без кода-фикса) — не в «Закрыто»; при отклонении CR как «не делаем» → «Принято», не severity.

## Workflow

1. **Resolve version + base**
   - Version: from user (`2.2.0`) or infer from branch / target file `docs/RELEASE-PLAN-X.Y.Z.md`
   - Base: default `origin/master` (fallback `master`)
   - Scope: **only** changes on this line vs base — do **not** copy closed items from older `RELEASE-PLAN-*.md` unless they are still open backlog for this version

2. **Collect delta** (required)

```bash
bash .cursor/skills/cross-identity-release-plan/scripts/collect-release-delta.sh \
  --base origin/master \
  --version 2.2.0
```

Read the printed path (under `.cursor/skills/cross-identity-release-plan/.cache/`). Use it as the evidence pack; do not invent commits.

3. **Classify each change** into exactly one bucket:

| Bucket | Put here |
|--------|----------|
| Критично | Auth/token/OAuth security holes still open |
| Высокий | Auth-model / correctness bugs still open |
| Средний | Contract contradictions / medium bugs still open |
| Низкий | Tech debt, XML, style, docs hygiene still open |
| Принято | Conscious trade-offs / host contracts decided for **this** release |
| Закрыто | Shipped or ready-to-ship fixes/features **in this delta** |
| Что в библиотеке уже нормально | Short bullets: invariants that remain true after the delta |
| Приоритет фиксов | Ordered open work left for this version (or «пусто» if release is done) |

4. **Write** `docs/RELEASE-PLAN-X.Y.Z.md` from the template (UTF-8 **with BOM**).

5. **Scope hygiene**
   - One version file = one release line. Cross-link other versions (`→ RELEASE-PLAN-2.2.0.md`) instead of duplicating their «Закрыто».
   - Carry-over open items from a prior plan **only if** still open and in scope for this version.
   - If `docs/BREAKING.md` must gain a **From A.B.C to X.Y.Z** section for consumer-facing breaks in the delta, say so in chat and offer to append (do not silently skip a needed BREAKING update when the user asked for release notes).

6. **Language:** Russian body (same as 2.0.0 / 2.1.1); table «Суть» may mix RU/EN technical names.

## Quality bar

- [ ] Sections match template order
- [ ] No foreign-release closed tables copied wholesale
- [ ] Every «Закрыто» row maps to evidence in the delta cache (commit, file, or API)
- [ ] Closed items removed from severity sections; `#` column looks like `✅ #34 Session IP binding config`
- [ ] «Приоритет фиксов» does not list closed numbers
- [ ] «Принято» states host vs library responsibility clearly when relevant
- [ ] File has UTF-8 BOM
