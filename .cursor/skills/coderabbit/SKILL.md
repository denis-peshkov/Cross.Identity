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
| Missing plan | **first** run [`release-plan`](../release-plan/SKILL.md), then triage into the new file |
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

0. **Ensure current RELEASE-PLAN (before or right after review, before triage)**  
   Resolve **current** version plan — see `release-plan` → **Current version plan** (user version / branch target / planned `docs/RELEASE-PLAN-X.Y.Z.md`).

   | Situation | Action |
   |-----------|--------|
   | Current `docs/RELEASE-PLAN-X.Y.Z.md` **exists** | Use it |
   | **No** current plan for the target version (file missing) | **Must** run skill [`release-plan`](../release-plan/SKILL.md) **fully** (collect delta → write plan) in this same session, **then** continue |
   | Version unknown | Ask user for `X.Y.Z`, or infer from branch/csproj/last plan +1; then create via `release-plan` if file still missing |

   **Forbidden:** dump CR findings into `docs/TO-DO.md`, invent a stub plan without `release-plan`, or skip creating the plan when it is missing.

1. **Auth / doctor** (if review fails):

```bash
export PATH="$HOME/.local/bin:$PATH"
coderabbit auth status
coderabbit doctor
```

If not signed in: tell the user to run `coderabbit auth login` in their terminal (browser OAuth). Do not fake findings.

2. **Run review** (required — use Shell with unrestricted permissions so `~/.coderabbit` storage works):

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

   Order note: step **0** may run before step 2 (plan first) or after step 3 (create plan after summary, before triage). Review and plan creation can be sequential; **triage (step 4) only after the plan file exists**.

3. **Summarize** findings from the log / `coderabbit review findings`:
   - Count by severity
   - Table: severity · file · short gist
   - Do **not** invent issues not in the output

4. **RELEASE-PLAN sync (always — not optional)**  
   Target = current plan from step **0** (created via `release-plan` if it was missing).  
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
   - Merge by meaning; next free id in that group across **current plan open + «Закрыто»** and `TO-DO.md` (`M43`, `L10`, …)
   - Skip duplicates already open in the current plan or already in any plan «Закрыто»
   - Skip duplicates already open in `TO-DO.md` (same meaning) — do **not** copy them into the plan open C/H/M/L, do **not** list them under **Приоритет фиксов**, and do **not** treat them as release work unless the user asks
   - In the chat reply: may briefly note «skipped (already in TO-DO: H1, M44)» — that is enough; no plan edits for those
   - Keep empty severity sections as heading + `---`; UTF-8 BOM
   - Update **Приоритет фиксов** of the current plan only for **new open** items added to that plan
   - In the chat reply: list what was **added** / **skipped** (and note if `release-plan` was run to create the file)

5. **Close / dismiss plan items (same turn as the user asks)**  
   If the user closes, rejects, or dismisses a C/H/M/L item from the current plan (won’t-fix, «только пример», duplicate, fixed, …):

   1. **First** append `| ✅ #H2 Short title | reason |` under that plan’s `## Закрыто`.
   2. **Then** remove the item from the open severity section.
   3. If the same id somehow still exists in `docs/TO-DO.md`, remove it there too (same **Close from TO-DO** / Re-check rules in `release-plan`).
   4. **Never** drop an open item without the «Закрыто» row.

## Limits

- Free plan often caps **~150 files** per review. If the branch delta is larger, prefer `--dir Cross.Identity` (then Tests / docs in a second run) or `--light`.
- Review can take several minutes — set a high Shell `block_until_ms` (e.g. 600000).

## Quality bar

- [ ] Used `--committed --base` against master (or user-specified base)
- [ ] Ran outside sandbox restrictions that break `~/.coderabbit`
- [ ] Summary matches the saved log
- [ ] If current plan was missing → `release-plan` skill ran and created `docs/RELEASE-PLAN-X.Y.Z.md` before triage
- [ ] **Current** `docs/RELEASE-PLAN-X.Y.Z.md` updated in the same turn (open C/H/M/L); **not** `TO-DO.md` for CR findings
- [ ] Any dismissed/closed item → `✅ #Id …` in that plan’s «Закрыто» before removal from open sections
