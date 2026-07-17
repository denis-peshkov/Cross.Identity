---
name: pr-triage
description: >-
  PR triage for Cross.Identity: audit open PRs, deep review, draft review
  comments; local/branch review vs master|dev without a PR. Args: "all",
  PR numbers, "branch <name>", "local", "base master|dev",
  "ru"/"en" for table language (default en).
---

# PR Triage — Cross.Identity

## When to use

| Scenario | Action |
|----------|--------|
| "Triage PRs" / "pr triage" | Phase 1 audit (GitHub open PRs) |
| "Triage this branch" / "local triage" | Phase 1b (git diff, no PR) |
| "Review branch X vs master" | Phase 1b + Phase 2 |
| >5 open PRs without review | Suggest Phase 1 audit |
| PR stale >14 days | Flag in table |

## Modes

| Mode | Args / trigger | Diff source | GitHub comment |
|------|----------------|-------------|----------------|
| **PR audit** | `all` or default | `gh pr list` | No (draft only) |
| **PR deep** | PR number(s) | `gh pr diff {n}` | AskQuestion + template |
| **Branch** | `branch <name>` `[base master\|dev]` | `git diff base...branch` | No |
| **Local** | `local` `[base master\|dev]` | `git diff base...HEAD` (+ uncommitted if asked) | No |

Default **base**: `master` if ambiguous; use `dev` when the user says so or the branch targets `dev`.

Prefer `origin/<base>` after `git fetch` when remote exists.

## Prerequisites

```bash
git rev-parse --is-inside-work-tree
```

For **PR** modes only:

```bash
gh auth status
```

GitHub commands — via `.cursor/triage/gh-wrapper.sh`.

## Language

- Tables: English (default), `ru` — Russian
- GitHub comments: English
- Branch/local report: same as tables language

## Phase 1 — Audit (open PRs)

### Data gathering

```bash
REPO=$(.cursor/triage/gh-wrapper.sh repo view --json nameWithOwner -q .nameWithOwner)

.cursor/triage/gh-wrapper.sh pr list --state open --limit 50 \
  --json number,title,author,createdAt,updatedAt,additions,deletions,changedFiles,isDraft,mergeable,reviewDecision,statusCheckRollup,body

.cursor/triage/gh-wrapper.sh api "repos/${REPO}/collaborators" --jq '.[].login'
```

For each PR (priority — overlap candidates):

```bash
.cursor/triage/gh-wrapper.sh api "repos/${REPO}/pulls/{num}/reviews" \
  --jq '[.[] | .user.login + ":" + .state] | join(", ")'

.cursor/triage/gh-wrapper.sh pr view {num} --json files --jq '[.files[].path] | join(",")'
```

### Classification

**Size**: XS <50, S 50–200, M 200–500, L 500–1000, XL >1000 additions.

**Detections**: overlaps >50% files, clusters (3+ PRs from same author), stale >14d, CI clean/dirty.

**Our PRs**: author in collaborators.

**External — ready**: ≤1000 additions, ≤10 files, not CONFLICTING, CI clean/unstable.

**External — problematic**: XL, conflict, CI dirty, overlap.

### Output tables

Sections: Our PRs / External ready / External problematic + Summary.

0 PRs → finish (or offer Phase 1b if user is on a feature/hotfix branch).

### Cross.Identity file hotspots

On overlap/review pay attention to:

- `Cross.Identity/Services/` — JWT, OAuth, codes
- `Cross.Identity/ProcessEngine/` — flows, steps
- `Cross.Identity/Entities/` — EF configurations
- `Cross.Identity.Tests/` — coverage

## Phase 1b — Branch / local (no PR)

Use when the user asks for local branch, named branch, or review without a PR. **Do not** require `gh` or post GitHub comments.

### Resolve refs

```bash
BASE="${BASE:-master}"          # or: dev
BRANCH="${BRANCH:-HEAD}"        # local: HEAD; named: hotfix/foo or origin/hotfix/foo

git fetch origin "$BASE" 2>/dev/null || true
git rev-parse --verify "origin/$BASE" >/dev/null 2>&1 && BASE_REF="origin/$BASE" || BASE_REF="$BASE"

# Named branch not checked out:
git rev-parse --verify "$BRANCH" >/dev/null 2>&1 || git rev-parse --verify "origin/$BRANCH" >/dev/null 2>&1
```

### Collect diff

```bash
git log --oneline "$BASE_REF..$BRANCH" | head -30
git diff --stat "$BASE_REF...$BRANCH"
git diff --name-status "$BASE_REF...$BRANCH"
git diff "$BASE_REF...$BRANCH"
```

Triple-dot (`...`) = changes on the branch since fork from base (merge-base). Prefer this over two-dot for triage.

**Uncommitted** (only if user asks `local` + working tree / uncommitted):

```bash
git status -sb
git diff --stat
git diff --cached --stat
git diff
git diff --cached
```

### Size / hotspots

Same size bands and file hotspots as Phase 1. No CI / mergeable / reviews — note "N/A (branch mode)".

### Output

Short summary + file list + offer Phase 2 deep review. Save under Saving below.

## Phase 2 — Deep Review (opt-in)

`Task` with `subagent_type: bugbot` or `generalPurpose` in parallel.

**PR mode:**

```bash
.cursor/triage/gh-wrapper.sh pr diff {num}
```

**Branch / local mode:**

```bash
git diff "$BASE_REF...$BRANCH"
# or uncommitted: git diff && git diff --cached
```

Checklist: `references/dotnet-checklist.md` + `.cursor/rules/105-backend-security.mdc`.

Response structure: Critical 🔴 / Important 🟠 / Suggestions 🟡 / What's Good ✅.

## Phase 3 — Comments (AskQuestion required)

Template: `templates/review-comment.md` (manual deep review). **PR mode only.**

Branch/local: report in chat and/or save file — **never** `gh pr comment` unless the user points at an existing PR number.

### Automated PR comment (CI)

On `pull_request` opened/synchronize CI posts a **wshm-style** comment:

- Script: `.cursor/triage/post-pr-triage.mjs`
- Template: `.cursor/triage/templates/pr-automated-triage-comment.md`
- Marker: `<!-- cross-identity-triage -->` (update on push, no duplication)
- Draft PR — skip

For a **single-PR automated comment** (same as CI), prefer:

```bash
PR_NUMBER=42 CURSOR_API_KEY=... yarn pr-triage
```

(from `.cursor/triage/`), not a parallel template.

```bash
.cursor/triage/gh-wrapper.sh pr comment {num} --body-file -
```

## Saving

- PR audit: `.cursor/triage/docs/prs-YYYY-MM-DD.md`
- Branch/local: `.cursor/triage/docs/branch-<safe-name>-YYYY-MM-DD.md`  
  (sanitize branch name: `/` → `-`)
