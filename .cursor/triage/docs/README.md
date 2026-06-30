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
# Data collection (rtk gh if installed)
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
- On a new push **updates** the same comment (marker `<!-- cross-identity-triage -->`)

Manual test: **Actions → Triage → Run workflow** → `pr_number` field.

### Artifacts

- `.cursor/triage/docs/ci-report-YYYY-MM-DD.md`
- `.cursor/triage/docs/.data/*.json` (in artifact, not in git)

## RTK

Installation (optional, for compressing `gh` output):

```bash
curl -fsSL https://raw.githubusercontent.com/rtk-ai/rtk/master/install.sh | sh
rtk gain  # verify: Rust Token Killer, not Type Kit
```

Script `.cursor/triage/rtk-gh.sh` automatically uses `rtk gh` or falls back to `gh`.
