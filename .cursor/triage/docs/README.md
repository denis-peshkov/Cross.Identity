# Automated Triage Reports

Triage reports for the Cross.Identity repository.

## Locally (Cursor Agent + skills)

```text
# GitHub: open issues + PRs + cross-analysis
Run triage
запусти triage

# Local: current branch vs origin/master (no PR required)
triage local
запусти triage local
triage local ru
triage local deep

# Named branch
triage branch release/fix-missed-issues base master

# Parts
Run triage-issue
Run triage-pr
triage-pr local
```

Skills: `.cursor/skills/{triage-issue,triage-pr,triage}/`

Reports:

- GitHub → `.cursor/triage/docs/triage-YYYY-MM-DD.md`
- Local/branch → `.cursor/triage/docs/branch-<safe-name>-YYYY-MM-DD.md`


## Scripts

```bash
# Data collection
bash .cursor/triage/collect-data.sh

# CI agent (requires CURSOR_API_KEY, Node 20.19.4)
cd .cursor/triage && yarn install --ignore-engines && CURSOR_API_KEY=... yarn triage
```

On Node 20 the SDK uses `JsonlLocalAgentStore` (`cursor-agent-local.mjs`), not `node:sqlite`.

## CI

Workflow `.github/workflows/triage.yml`:

- **Schedule**: Monday 06:00 UTC
- **workflow_dispatch**: manual run
- **issues opened**: data collection
- **pull_request** opened/synchronize/reopened/edited: AI comment on PR (wshm-style)

### Secrets

| Secret | Required | Purpose |
|--------|----------|---------|
| `CURSOR_API_KEY` | Yes (for AI report) | Cursor SDK in CI |
| `GITHUB_TOKEN` | Auto | `gh` CLI |

Create a key: [Cursor Dashboard → Integrations](https://cursor.com/dashboard/integrations)

### PR opened / updated

Workflow `triage.yml` → job **PR automated comment**:

- Cursor Agent analyzes the diff
- Posts a wshm-style comment (category, priority, confidence, summary, files)
- Applies GitHub labels: `{category}` and `priority:{priority}` (e.g. `enhancement`, `priority:medium`); on re-run replaces previous triage labels only
- On a new push **updates** the same comment (marker `<!-- triage -->`; legacy `<!-- cross-identity-triage -->` still matched)

Manual test: **Actions → Triage → Run workflow** → `pr_number` field.

### Artifacts

- `.cursor/triage/docs/ci-report-YYYY-MM-DD.md`
- `.cursor/triage/docs/.data/*.json` (in artifact, not in git)

### GitHub CLI

Triage scripts call `.cursor/triage/gh-wrapper.sh`, which delegates to `gh` (preinstalled on GitHub Actions runners; install locally via `gh auth login`).
