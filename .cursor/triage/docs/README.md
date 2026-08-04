# Automated Triage Reports

Triage reports for the Cross.Identity repository.

## Locally (Cursor Agent + skills)

```bash
# Full triage
# In Cursor chat: "Run cross-identity-triage"

# Or in parts:
# "Run issue-triage"
# "Run pr-triage"
```

Skills: `.cursor/skills/{issue-triage,pr-triage,cross-identity-triage}/`

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
- **pull_request** opened/synchronize: AI comment on PR (wshm-style)

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
- On a new push **updates** the same comment (marker `<!-- cross-identity-triage -->`)

Manual test: **Actions → Triage → Run workflow** → `pr_number` field.

### Artifacts

- `.cursor/triage/docs/ci-report-YYYY-MM-DD.md`
- `.cursor/triage/docs/.data/*.json` (in artifact, not in git)

### GitHub CLI

Triage scripts call `.cursor/triage/gh-wrapper.sh`, which delegates to `gh` (preinstalled on GitHub Actions runners; install locally via `gh auth login`).
