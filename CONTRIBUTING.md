# Contributing to Cross.Identity

Thank you for your interest in the project.

## Quick links

- [Report an issue](https://github.com/denis-peshkov/Cross.Identity/issues/new)
- [Open PRs](https://github.com/denis-peshkov/Cross.Identity/pulls)
- [CI (.NET)](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml)
- [CI (back-merge master → dev)](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/backmerge-master-to-dev.yml)
- [SonarCloud](https://sonarcloud.io/summary/new_code?id=denis-peshkov.Cross.Identity)
- [NuGet](https://www.nuget.org/packages/Cross.Identity/)
- Flow documentation: [`Cross.Identity/FLOWS.md`](Cross.Identity/FLOWS.md)
- Release plan: [`docs/RELEASE-PLAN-dev-to-master.md`](docs/RELEASE-PLAN-dev-to-master.md)

---

## What is Cross.Identity?

**Cross.Identity** is a NuGet identity and authentication library for .NET:

- process engine with JSON flows (`main/*`, …);
- JWT access/refresh, Argon2, OTP (email/SMS);
- external OAuth (Google, Microsoft, GitHub, Apple);
- licensing via Peshkov JWT key (`CrossIdentity:LicenseKey`).

Consumers register the package with `services.AddCrossIdentity(configuration)` and call `IFlowExecutor.ExecuteAsync`.

---

## How you can help

| Type | Examples |
|------|----------|
| **Report** | Bug with repro steps, expected/actual behavior, package version and TFM |
| **Fix** | Regression fix, OAuth state, refresh rotation, flow validation |
| **Build** | New step/factory, flow JSON, integration tests, Sample.Api improvements |
| **Review** | PR review, especially auth/JWT/OAuth changes |
| **Document** | `FLOWS.md`, `docs/MIGRATION.md`, README, release notes in `config.nuspec` |

---

## Development principles

### Security first

This is an identity library. Any change to JWT, refresh tokens, OAuth state, passwords, or licensing is **high-priority review**. Do not log passwords, codes, tokens, or license keys.

### Flow contracts are public API

JSON in `ProcessEngine/Definitions/Flows/` and `collectResult` behavior are contracts for NuGet consumers. Breaking changes require entries in `docs/MIGRATION.md` and `config.nuspec` releaseNotes.

### One `kind` per flow

In a single JSON flow, each step `kind` must be unique. Two `collectForm` steps in one file will not load — see `FLOWS.md`.

### Minimal diff

Do not mix refactoring, formatting untouched files, and a feature in one PR. Drive-by changes belong in a separate PR.

### Repository conventions

- `.editorconfig` — style source (UTF-8 BOM, CRLF, 4 spaces for `.cs`).
- `GlobalUsings.cs` — all `using` directives in one file; `ImplicitUsings` = `disable`.
- New `.cs` / `.csproj` / `.sln` files — **UTF-8 with BOM**.
- Tests — **NUnit**; method names `Given[X]_When[Y]_Then[Z]` (async → `…Async`). Canonical: [`.cursor/rules/300-testing-dotnet.mdc`](.cursor/rules/300-testing-dotnet.mdc).

More details: [`.cursor/rules/`](.cursor/rules/) (for Cursor/IDE).

---

## In scope / out of scope

### In scope

- `Cross.Identity/` — library, steps, services, entities, licensing;
- `Cross.Identity.Tests/` — unit + integration (flow, OAuth, JWT);
- `Sample.Api/` — smoke/E2E host example;
- `Cross.Identity/FLOWS.md`, `docs/MIGRATION.md`, `config.nuspec`;
- CI: `.github/workflows/dotnet.yml`, `triage.yml`, `backmerge-master-to-dev.yml`.

### Out of scope (without maintainer discussion)

- Large process engine architecture refactors “for aesthetics”;
- New external dependencies without a strong reason;
- Consumer-breaking changes without a migration guide;
- Secrets, keys, `.env` in commits.

### New flow or step

1. JSON: `Cross.Identity/ProcessEngine/Definitions/Flows/{flow}.{Operation}.json`
2. For a new `kind` — `IStep` + `IStepFactory` + registration in `ServiceCollectionExtensions`
3. Tests: unit step/factory + integration flow test
4. Entry in `FLOWS.md`
5. For public contract changes — `docs/MIGRATION.md`

---

## Branches and releases

```
                        ┌──── CI merge ─────────┐     (master → dev, no PR)
feature/* ──┐           ▼                       │
fix/*     ──┼── PR ──► dev ── merge ──► master ─┴─► NuGet + git tag
chore/*   ──┘                              ▲
                                           │
                                 release/* / hotfix/* (owner only)
```

| Branch | Purpose | Who |
|--------|---------|-----|
| `dev` | Feature integration | **Default PR target** for all contributors |
| `master` | Stable release; GitVersion, tag, NuGet push | **Owner only** — direct push and PRs |
| `feature/*` | New functionality | Contributors |
| `fix/*` | Bug fixes | Contributors |
| `chore/*` | CI, deps, docs-only, maintenance | Contributors |
| `release/*` | Release preparation | **Owner only** — branch creation and push |
| `hotfix/*` | Urgent production patches | **Owner only** — branch creation and push |

**Access rules (enforced in CI via `.github/workflows/branch-policy.yml`):**

- Contributors open PRs **only into `dev`** from `feature/*`, `fix/*`, or `chore/*`.
- PRs targeting **`master`** — repository owner only (`denis-peshkov`).
- Pushing to **`master`**, **`release/*`**, or **`hotfix/*`** — owner only.
- Release merge `dev` → `master`, tags, and NuGet publish — maintainer step after release checklist.
- After changes land on **`master`**, CI (`.github/workflows/backmerge-master-to-dev.yml`) **merges `master` into `dev` and pushes** (no PR, no build wait) using the owner PAT secret **`TAGTOKEN`** — required to bypass Protect-dev (PR + `build`). Plain `GITHUB_TOKEN` is rejected (`GH013`). If there are conflicts, the job fails — resolve locally and push to `dev`.

Versioning: **GitVersion** (`GitVersion.yml`). `dev` is pre-release (`-dev.N`), not a release branch.

---

## Branch naming

Prefix + kebab-case description:

| Prefix | When |
|--------|------|
| `feature/` | New functionality |
| `fix/` | Bug fix |
| `chore/` | CI, deps, docs-only, maintenance |

Examples:

```
feature/external-login-apple-profile
fix/refresh-token-reuse-after-rotation
chore/sonar-quality-gate-docs
```

---

## Commit messages

Use **clear English** messages in imperative/descriptive style (as in repository history):

```
Add integration tests for RefreshToken flow
Fix OAuth state storage for multi-instance deployments
Update FLOWS.md for ExternalLogin flows
```

For breaking changes, explicitly include `BREAKING:` in the commit body or PR description.

`CHANGELOG.md` is maintained manually before release (see `docs/RELEASE-PLAN-dev-to-master.md`).

---

## Pull request process

### 1. Preparation

```bash
git checkout dev
git pull origin dev
git checkout -b feature/short-description
```

### 2. Changes

- Follow directory structure (`Services/`, `ProcessEngine/Steps/`, `Entities/`).
- Do not touch unrelated files.
- Breaking change → `docs/MIGRATION.md` + `config.nuspec` releaseNotes.

### 3. Tests (required)

See [Testing](#testing).

### 4. Open PR

- **Base branch:** `dev` (required for contributors)
- **Do not** open PRs into `master`, `release/*`, or `hotfix/*` unless you are the repository owner
- Description: what, why, how to verify (in **English** — for GitHub history and the triage bot)
- For auth/security — explicitly note risks

### 5. CI

Must pass:

- `dotnet build` + `dotnet test` (`.NET` workflow)
- Branch policy (`.github/workflows/branch-policy.yml`) — contributors cannot PR to `master` or push `release/*` / `hotfix/*`
- SonarCloud quality gate (on PR — `sonar.qualitygate.wait=true`)
- If triage changed — `PR automated comment` job (must not fail on large diffs)

### 6. Review and merge

After approval — merge into `dev`. Release to `master` and NuGet publish is a separate maintainer step.

### One PR rule

**One PR = one feature or one fix.** Split large changes (model + tests → flow → docs).

---

## Testing

**Canonical:** [`.cursor/rules/300-testing-dotnet.mdc`](.cursor/rules/300-testing-dotnet.mdc) (naming `Given[X]_When[Y]_Then[Z]`, AAA, categories, layout, coverage commands). Do not use a different naming style in new or renamed tests.

### Local run

```bash
dotnet build Cross.Identity.slnx
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj
```

With coverage (as in CI) — see the OpenCover example in `300-testing-dotnet.mdc`.

### Pre-PR checklist

- [ ] Tests added/updated for changed behavior (`Given[X]_When[Y]_Then[Z]`; async → `Async`)
- [ ] `dotnet test` — green locally
- [ ] For flows — integration test in `Cross.Identity.Tests/Identity/FlowTests/`
- [ ] For OAuth/JWT/licensing — not only happy path
- [ ] No secrets in code or test data
- [ ] If editing `RELEASE-PLAN` — `node docs/scripts/release-plan-summary.mjs --write`

---

## Documentation

| What changed | Update |
|--------------|--------|
| JSON flow / step | `Cross.Identity/FLOWS.md` |
| Breaking change for consumers | `docs/MIGRATION.md`, `config.nuspec` |
| New configuration option | `README.md`, XML on options class |
| OAuth / multi-instance | `FLOWS.md` (briefly), release plan §B |
| Release checklists | `docs/RELEASE-PLAN-dev-to-master.md` + summary script |
| Package public API | `README.md` |

Flow documentation covers **JSON and steps only**, not full appsettings (config is in README / release plan).

---

## Security

For issues/PRs on the following topics, include package version and minimal repro:

- JWT (signature, encryption, claims, refresh rotation);
- OAuth state / external login;
- token or PII leakage in logs;
- license bypass;
- SQL/EF injection, mass assignment in flow input.

**Do not publish** real license keys, pepper secrets, or OAuth client secrets in issues/PRs.

---

## License

Code is under [RPL 1.5](LICENSE) (Reciprocal Public License). By contributing, you agree that derivative works are distributed under the same terms, or under a [Peshkov commercial license](https://peshkov.biz/license).

There is no separate CLA — merging a PR means agreement with the repository license.

---

## For maintainers (internal)

- Triage: `.cursor/skills/`, `bash .cursor/triage/collect-data.sh`
- GitHub CLI wrapper for triage: `.cursor/triage/gh-wrapper.sh`
- Release gate: `docs/RELEASE-PLAN-dev-to-master.md`
- Deploy key (`cross-identity-deploy-key*`) — Azure DevOps submodule only, **not** for license key

---

## Questions?

- Bugs and features: [GitHub Issues](https://github.com/denis-peshkov/Cross.Identity/issues)
- Flow API discussion: open an issue with a tag and link to `FLOWS.md`

**Thank you for contributing to Cross.Identity.**
