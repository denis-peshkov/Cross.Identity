---
name: pr-triage
description: >-
  PR triage for Cross.Identity: audit open PRs, deep review, draft review
  comments. Args: "all", PR numbers,
  "ru"/"en" for table language (default en).
---

# PR Triage — Cross.Identity

## When to use

| Scenario | Action |
|----------|--------|
| "Triage PRs" / "pr triage" | Run skill |
| >5 open PRs without review | Suggest audit |
| PR stale >14 days | Flag in table |

## Prerequisites

```bash
git rev-parse --is-inside-work-tree
gh auth status
```

GitHub commands — via `.cursor/triage/gh-wrapper.sh`.

## Language

- Tables: English (default), `ru` — Russian
- GitHub comments: English

## Phase 1 — Audit

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

0 PRs → finish.

### Cross.Identity file hotspots

On overlap/review pay attention to:

- `Cross.Identity/Services/` — JWT, OAuth, codes
- `Cross.Identity/ProcessEngine/` — flows, steps
- `Cross.Identity/Entities/` — EF configurations
- `Cross.Identity.Tests/` — coverage

## Phase 2 — Deep Review (opt-in)

`Task` with `subagent_type: bugbot` or `generalPurpose` in parallel.

```bash
.cursor/triage/gh-wrapper.sh pr diff {num}
```

Checklist: `references/dotnet-checklist.md` + `.cursor/rules/105-backend-security.mdc`.

Response structure: Critical 🔴 / Important 🟡 / Suggestions 🟢 / What's Good ✅.

## Phase 3 — Comments (AskQuestion required)

Template: `templates/review-comment.md` (manual deep review).

### Automated PR comment (CI)

On `pull_request` opened/synchronize CI posts a **wshm-style** comment:

- Script: `.cursor/triage/post-pr-triage.mjs`
- Template: `.cursor/triage/templates/pr-automated-triage-comment.md`
- Marker: `<!-- cross-identity-triage -->` (update on push, no duplication)
- Draft PR — skip

```bash
PR_NUMBER=42 CURSOR_API_KEY=... yarn pr-triage
```

```bash
.cursor/triage/gh-wrapper.sh pr comment {num} --body-file -
```

## Saving

`.cursor/triage/docs/prs-YYYY-MM-DD.md`
