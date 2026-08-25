---
name: triage
description: >-
  Full triage orchestrator: issue-triage + pr-triage in parallel, cross-analysis
  (double coverage, security gaps, P0 without PR, dirty CI). Saves report to
  .cursor/triage/docs/. Args: "ru"/"en" (default en), "no save" — skip file.
---

# Triage (orchestrator)

Combines `issue-triage` + `pr-triage` + cross-analysis of issues × PRs.

## When to use

- Weekly or before NuGet release
- Before sprint planning
- After CI workflow `triage.yml` — to interpret artifacts

## Phase 0 — Prerequisites

```bash
git rev-parse --is-inside-work-tree
.cursor/triage/gh-wrapper.sh auth status
date +%Y-%m-%d
```

Or run data collection:

```bash
.cursor/triage/collect-data.sh
```

## Phase 1 — Data gathering (in parallel)

**Issues** (via `gh-wrapper.sh`):

```bash
.cursor/triage/gh-wrapper.sh issue list --state open --limit 150 \
  --json number,title,author,createdAt,updatedAt,labels,assignees,body

.cursor/triage/gh-wrapper.sh issue list --state closed --limit 20 \
  --json number,title,labels,closedAt
```

**PRs**:

```bash
.cursor/triage/gh-wrapper.sh pr list --state open --limit 200 \
  --json number,title,author,createdAt,updatedAt,additions,deletions,changedFiles,isDraft,mergeable,reviewDecision,statusCheckRollup,body
```

PR files — for overlap detection (see `pr-triage`).

## Phase 2 — Individual triage

Run logic from `issue-triage` and `pr-triage` (Phase 1 of each) — issue and PR tables.

## Phase 3 — Cross-analysis

### 3.1 Double coverage — 2 PRs for 1 issue

| Issue | PR1 | PR2 | Verdict |
|-------|-----|-----|---------|

Rules: smaller scope, CI clean, internal PR, overlap >80% → conflict.

### 3.2 Security gaps

For issues with "red" risk — findings without PR (especially JWT, refresh tokens, OAuth).

### 3.3 P0/P1 without PR

Labels/keywords: crash, auth, token, jwt, security.

### 3.4 Our PRs dirty

CI dirty / CONFLICTING — reason (overlap, rebase needed).

### 3.5 PR without `fixes #N`

Internal PRs not linked to an issue.

## Phase 4 — Output

Summary:

| Category | Count |
|----------|-------|
| PRs ready to merge (ours) | N |
| Quick wins (external) | N |
| Double coverage | N |
| P0/P1 without PR | N |
| Security without PR | N |
| Dirty PRs | N |

### Saving

`.cursor/triage/docs/triage-YYYY-MM-DD.md` (unless `no save`).

File structure:

```markdown
# Triage — YYYY-MM-DD

## Issues (tables)
## PRs (tables)
## 1. Double coverage
## 2. Security gaps
## 3. P0/P1 without PR
## 4. Dirty PRs
## 5. Priority actions
## Numeric summary
```

## CI integration

After `triage.yml` read:

- `.cursor/triage/docs/.data/*.json` — raw data
- `.cursor/triage/docs/ci-report-YYYY-MM-DD.md` — CI agent report

Supplement with cross-analysis manually if needed.

## Rules

- GitHub actions (comments/close) — only with `AskQuestion`
- Table language: en (default), ru via argument
- Always use `.cursor/triage/gh-wrapper.sh` for `gh` commands
