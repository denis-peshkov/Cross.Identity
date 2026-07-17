# Cross.Identity — GitHub Ruleset recipes

Importable JSON for [repository rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository), aligned with `CONTRIBUTING.md` branch policy.

Based on [github/ruleset-recipes](https://github.com/github/ruleset-recipes).

## Prerequisite (important)

`Cross.Identity` is a **private** repository. Repository rulesets on private repos require **[GitHub Pro](https://docs.github.com/en/get-started/learning-about-github/githubs-plans)** (or a public repo).

Without Pro, Settings → Rules → Rulesets and the Rulesets API return:

```text
Upgrade to GitHub Pro or make this repository public to enable this feature.
```

Until Pro is enabled, keep enforcing policy via `.github/workflows/branch-policy.yml`.

## Import

1. Enable **GitHub Pro** (or make the repo public).
2. Open the repo → **Settings** → **Rules** → **Rulesets**.
3. **New ruleset** → **Import a ruleset**.
4. Import files below **one by one** (order recommended).
5. Review each ruleset:
   - Confirm **status check** context is `build` (job name in `.github/workflows/dotnet.yml`). If GitHub shows a different name (e.g. `.NET / build`), edit the required check after the first green run.
   - Confirm **Bypass** is Repository admin only (`RepositoryRole` / Admin).
6. Save with **Active** (or start with **Evaluate** if available on your plan).

## Files

| File | Target | Purpose |
|------|--------|---------|
| [`01-protect-master.json`](01-protect-master.json) | `master` | No force-push/delete; PR + CI required; admin bypass for releases |
| [`02-protect-dev.json`](02-protect-dev.json) | `dev` | No force-push/delete; PR + required `build`; admin bypass. Back-merge CI pushes with `TAGTOKEN` |
| [`03-protect-release-hotfix.json`](03-protect-release-hotfix.json) | `release/*`, `hotfix/*` | Create/update/delete only via admin bypass |
| [`04-protect-release-tags.json`](04-protect-release-tags.json) | tags `v*` | Protect NuGet/GitVersion tags; create via admin / `TAGTOKEN` CI |
| [`05-push-block-secrets.json`](05-push-block-secrets.json) | push (repo-wide) | **Not available** on personal repos (org-owned private/internal only) |

## Bypass actors

| Actor | ID | Used for |
|-------|-----|----------|
| RepositoryRole **Admin** | `5` | Owner (`denis-peshkov`) and workflows using an **admin PAT** (`TAGTOKEN`) |

**Do not** put Integration `15368` (GitHub Actions) in `bypass_actors` on this personal repo — import fails with:

```text
Error importing ruleset: The ruleset you are importing contains an invalid actor
```

Back-merge and tag push must use `secrets.TAGTOKEN` (owner PAT with `repo` scope), not `GITHUB_TOKEN`. Plain `GITHUB_TOKEN` is blocked by Protect-dev (`GH013`: PR required + check `build`).

## After import

1. Open a test PR into `dev` — required check `build` must appear.
2. Confirm non-admin cannot push to `master` / create `release/foo`.
3. Ensure Actions secret **`TAGTOKEN`** is set (owner PAT, `repo` scope) — same secret as `dotnet.yml` tag push.
4. Run **Back-merge master to dev** once — must push to `dev` via `TAGTOKEN` (admin bypass; no PR, no build wait).
5. Optionally keep `branch-policy.yml` as a secondary signal.

## Not encoded in rulesets

- “Only owner may open PR to `master`” — use admin-only merge + `branch-policy.yml`, or require reviews from CODEOWNERS.
- Contributor branch prefixes `feature|fix|chore` — optional; add `branch_name_pattern` later in Evaluate mode.

## Push ruleset (05) — not importable here

GitHub rejects push rulesets on **personal** repositories:

```text
Source public repos cannot have push rules
Source only org-owned repos can have push rules
```

Keep `05-push-block-secrets.json` as a draft for a future org transfer, or rely on `.gitignore` + `branch-policy.yml`.
