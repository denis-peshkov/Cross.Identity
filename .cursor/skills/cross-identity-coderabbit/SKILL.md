---
name: cross-identity-coderabbit
description: >-
  Runs CodeRabbit CLI review of the current branch vs master (committed delta),
  saves agent findings, and always merges Critical/Major/Minor/Trivial/Info into
  docs/TO-DO.md as C/H/M/L. Use when the user asks to run CodeRabbit, CR review,
  or local coderabbit on branch changes.
---

# Cross.Identity — CodeRabbit vs master

## When to use

- User asks to run **CodeRabbit** / **CR** / `coderabbit` on the current branch
- Local review of committed changes vs `master` before PR / release plan

## Defaults

| Flag | Value |
|------|--------|
| Base | `origin/master` (fallback `master`) |
| Scope | committed branch delta (`--committed`) |
| Output | `--agent` (JSONL findings for agents) |
| After review | **always** triage into [`docs/TO-DO.md`](../../../docs/TO-DO.md) |
| CLI | `coderabbit` from `PATH` (also `~/.local/bin`) |

## Workflow

1. **Auth / doctor** (if review fails):

```bash
export PATH="$HOME/.local/bin:$PATH"
coderabbit auth status
coderabbit doctor
```

If not signed in: tell the user to run `coderabbit auth login` in their terminal (browser OAuth). Do not fake findings.

2. **Run review** (required — use Shell with unrestricted permissions so `~/.coderabbit` storage works):

```bash
bash .cursor/skills/cross-identity-coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master
```

Optional:

```bash
# Library-only (helps Free plan 150-file limit)
bash .cursor/skills/cross-identity-coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master --dir Cross.Identity

# Lighter / include uncommitted
bash .cursor/skills/cross-identity-coderabbit/scripts/run-coderabbit-review.sh \
  --base origin/master --light --uncommitted
```

Script prints the log path under `.cursor/skills/cross-identity-coderabbit/.cache/`.

3. **Summarize** findings from the log / `coderabbit review findings`:
   - Count by severity
   - Table: severity · file · short gist
   - Do **not** invent issues not in the output

4. **TO-DO sync (always — not optional)**  
   Immediately merge findings into [`docs/TO-DO.md`](../../../docs/TO-DO.md) as **C/H/M/L only** (no separate CR section):

| CodeRabbit | TO-DO |
|------------|--------|
| Critical | `C…` |
| Major | `H…` |
| Minor | `M…` |
| Trivial / Info | `L…` |

   Rules:
   - Merge by meaning; next free id in that group (`M43`, `L10`, …)
   - Skip duplicates already open in TO-DO
   - Do not re-add items already in any `RELEASE-PLAN-*.md` «Закрыто»
   - Keep empty C/H sections if still empty; UTF-8 BOM
   - In the chat reply: list what was **added** / **skipped**

## Limits

- Free plan often caps **~150 files** per review. If the branch delta is larger, prefer `--dir Cross.Identity` (then Tests / docs in a second run) or `--light`.
- Review can take several minutes — set a high Shell `block_until_ms` (e.g. 600000).

## Quality bar

- [ ] Used `--committed --base` against master (or user-specified base)
- [ ] Ran outside sandbox restrictions that break `~/.coderabbit`
- [ ] Summary matches the saved log
- [ ] `docs/TO-DO.md` updated in the same turn (C/H/M/L); additions reported to user
