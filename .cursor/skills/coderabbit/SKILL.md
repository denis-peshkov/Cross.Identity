---
name: coderabbit
description: >-
  Runs CodeRabbit CLI review of the current branch vs master (committed delta),
  saves agent findings, and always merges Critical/Major/Minor/Trivial/Info into
  the current docs/RELEASE-PLAN-X.Y.Z.md as open C/H/M/L (not docs/TO-DO.md).
  If no current version plan exists, runs the release-plan skill first to create
  it, then merges findings. Closing/dismissing a plan item moves it to that
  plan’s «Закрыто» as `✅ #Id …`. Use when the user asks to run CodeRabbit, CR
  review, or local coderabbit on branch changes.
---

# CodeRabbit vs master

## When to use

- User asks to run **CodeRabbit** / **CR** / `coderabbit` on the current branch
- Local review of committed changes vs `master` before PR / release plan

## Defaults

| Flag | Value |
|------|--------|
| Base | `origin/master` (fallback `master`) |
| Scope | committed branch delta (`--committed`) |
| Output | `--agent` (JSONL findings for agents) |
| After review | **always** triage into **current** [`docs/RELEASE-PLAN-X.Y.Z.md`](../../../docs/) (not `TO-DO.md`) |
| Missing plan | Skill [`release-plan`](../release-plan/SKILL.md) → **Ensure current RELEASE-PLAN** · run full workflow if plan missing |
| CLI | `coderabbit` from `PATH` (also `~/.local/bin`) |

## GitHub PR bot (not the local CLI)

Local `coderabbit review` ≠ GitHub bot. To re-run on a PR after auto-pause (see `.coderabbit.yaml` → `auto_pause_after_reviewed_commits`), comment on the PR:

```text
@coderabbitai full review
```

| Command | Effect |
|---------|--------|
| `@coderabbitai full review` | full review of the whole PR from scratch |
| `@coderabbitai review` | only new changes since the last review |

Ack: `Full review triggered` → `Full review finished`. Docs: [Review commands](https://docs.coderabbit.ai/reference/review-commands).

Optional: `coderabbit pullrequest <n> --agent` reads an existing GitHub review into the agent (does not trigger a new bot run).

## Workflow

### Phase 1 — Prerequisites

1. Skill [`release-plan`](../release-plan/SKILL.md) → **Ensure current RELEASE-PLAN** · CR findings → plan, not TO-DO (before or after review, before Phase 3).
2. **Auth / doctor** (if review fails):

```bash
export PATH="$HOME/.local/bin:$PATH"
coderabbit auth status
coderabbit doctor
```

If not signed in: tell the user to run `coderabbit auth login` in their terminal (browser OAuth). Do not fake findings.

### Phase 2 — Review

1. **Run review** (required — use Shell with unrestricted permissions so `~/.coderabbit` storage works):

```bash
bash .cursor/skills/coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master
```

Optional:

```bash
# Library-only (helps Free plan 150-file limit)
bash .cursor/skills/coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master --dir Cross.Identity

# Lighter / include uncommitted
bash .cursor/skills/coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master --light --uncommitted
```

Script prints the log path under `.cursor/skills/coderabbit/.cache/`.

2. **Summarize** findings from the log / `coderabbit review findings`:
   - Count by severity
   - Table: severity · file · short gist
   - Do **not** invent issues not in the output

Order note: Phase **1** step **1** may run before Phase **2** (plan first) or after Phase **2** step **2** (create plan after summary, before Phase 3). Review and plan creation can be sequential; **Phase 3 only after the plan file exists**.

### Phase 3 — RELEASE-PLAN sync

**Always — not optional.** Target = current plan from Phase **1** step **1** (created via `release-plan` if it was missing).  
Immediately merge findings into that plan’s open severity sections as **C/H/M/L only** (no separate CR section).  
**Do not** write CR findings into [`docs/TO-DO.md`](../../../docs/TO-DO.md).

| CodeRabbit | Plan section |
|------------|--------------|
| Critical | `## Критично` → `C…` |
| Major | `## Высокий` → `H…` |
| Minor | `## Средний` → `M…` |
| Trivial / Info | `## Низкий` → `L…` |

Format (match plan legend): `### M43. Title` + `⬜` description.

Rules:
- Merge by meaning; next id = **max(`Id high-water` in TO-DO, current plan open+«Закрыто» ids) + 1** for that group (`C`/`H`/`M`/`L`). **Do not** bump high-water in `TO-DO.md` until **Finalize version plan** ([`release-plan`](../release-plan/SKILL.md))
- Skip duplicates already open in the current plan or already in any plan «Закрыто»
- Skip duplicates already open in `TO-DO.md` (same meaning) — do **not** copy them into the plan open C/H/M/L, do **not** list them under **Приоритет фиксов**, and do **not** treat them as release work unless the user asks
- In the chat reply: may briefly note «skipped (already in TO-DO: H1, M44)» — that is enough; no plan edits for those
- Keep empty severity sections as heading + `---`; UTF-8 BOM
- Update **Приоритет фиксов** of the current plan only for **new open** items added to that plan
- In the chat reply: list what was **added** / **skipped** (and note if `release-plan` was run to create the file)

### Phase 4 — Close / dismiss (same turn as the user asks)

When the user closes, rejects, or dismisses a C/H/M/L item from the current plan (won’t-fix, «только пример», duplicate, fixed, …):

1. **First** append `| ✅ #H2 Short title | reason |` under that plan’s `## Закрыто` (id prefix matches severity: Minor→`#M…`, not `#L…`).
2. **Then** remove the item from the open severity section (if it was open).
3. If the same id somehow still exists in `docs/TO-DO.md`, remove it there too — Skill [`release-plan`](../release-plan/SKILL.md) → **Close from TO-DO** / **Re-check**.
4. **Never** drop an open item without the «Закрыто» row.
5. **Fix-in-same-turn:** still allocate next `C/H/M/L` id via max(TO-DO HW, current plan ids)+1 (no mid-release HW write), write `✅ #M50 …` (etc.) into «Закрыто» — do **not** skip the plan row or downgrade Minor→`L`.

## Limits

- Free plan often caps **~150 files** per review. If the branch delta is larger, prefer `--dir Cross.Identity` (then Tests / docs in a second run) or `--light`.
- Review can take several minutes — set a high Shell `block_until_ms` (e.g. 600000).

## Quality bar

- [ ] Used `--committed --base` against master (or user-specified base)
- [ ] Ran outside sandbox restrictions that break `~/.coderabbit`
- [ ] Summary matches the saved log
- [ ] Did **not** glob/`ls` all `docs/RELEASE-PLAN-*.md`; only current plan (+ TO-DO when needed)
- [ ] If current plan was missing → `release-plan` skill ran and created `docs/RELEASE-PLAN-X.Y.Z.md` before triage
- [ ] **Current** `docs/RELEASE-PLAN-X.Y.Z.md` updated in the same turn (open C/H/M/L); **not** `TO-DO.md` for CR findings
- [ ] Any dismissed/closed item → `✅ #Id …` in that plan’s «Закрыто» before removal from open sections
