<!-- PR body: narrative sections + checklist (Homebrew-style). English — for GitHub history and triage bot. -->

## PR Merge Title

<!-- Same as squash merge title and lead commit — imperative English per CONTRIBUTING.md. Breaking: prefix commit/PR body with BREAKING: -->

```
Add integration tests for RefreshToken flow
```

---

## Solution

<!-- One-line summary of what was built and why it fixes the problem. -->

Closes: #0000

---

## Changes

<!-- Extend `path/to/file.cs` — numbered list; optional scope line at the end. -->

1. …
2. …

**Scope:** N files, ~N lines.

---

## Acceptance criteria

<!-- How you verified the change — check off before merge. -->

- [ ] …
- [ ] `dotnet build Cross.Identity.slnx` — green locally
- [ ] `dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj` — green locally

---

## Why this PR is small and safe

<!-- Why review is straightforward: narrow diff, no unrelated refactors, risk boundaries. -->

-

---

<!-- Do not tick a checkbox below if you have not performed its action. Honesty keeps review smooth. -->

### Process

- [ ] I have read and followed [CONTRIBUTING.md](../CONTRIBUTING.md).
- [ ] **PR Merge Title** above matches the squash title and lead commit message.
- [ ] There is no other open [pull request](https://github.com/denis-peshkov/Cross.Identity/pulls) for the same fix or feature.
- [ ] This PR targets **`dev`** (not `master`, unless agreed with a maintainer).
- [ ] **One PR = one feature or one fix** — no unrelated refactors or drive-by formatting.

### CI and quality

- [ ] CI [.NET workflow](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml) is expected to pass (build, test, SonarCloud quality gate on PR).
- [ ] New or updated tests cover the changed behavior (unit / integration / functional as appropriate).

### Scope-specific (tick what applies)

- [ ] **Flow / step / JSON definition** — integration test in `Cross.Identity.Tests/Identity/FlowTests/`, update [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md).
- [ ] **Breaking change for NuGet consumers** — [`docs/MIGRATION.md`](../docs/MIGRATION.md) and `config.nuspec` releaseNotes updated; `BREAKING:` noted in **Solution**.
- [ ] **New option or public API** — README and XML docs on the options/type updated.
- [ ] **Sample.Api** — smoke-checked locally (`dotnet run --project Sample.Api`) or via [`rest-client/Sample.Api.http`](../rest-client/Sample.Api.http) where relevant.
- [ ] **Release plan checklist** — if edited, ran `node docs/scripts/release-plan-summary.mjs --write`.
- [ ] **Auth / JWT / OAuth / licensing / passwords** — risks described in **Why this PR is small and safe** or below; no secrets in the diff.

### Security notes

<!-- Required for auth, JWT, refresh, OAuth, licensing, or PII/logging changes. Otherwise write "N/A". -->

-

### Repository conventions

- [ ] `.editorconfig` respected (UTF-8 BOM, CRLF, 4 spaces for `.cs`; `GlobalUsings.cs` for usings).
- [ ] No secrets, `.env`, pepper values, OAuth client secrets, or real license keys committed.

---

### AI assistance

- [ ] AI was used to generate or assist with this PR. *Describe below how AI helped and what you manually verified.*

<!-- If you did not use AI, leave unchecked and write "N/A" below. -->

-

---

**License:** By opening this PR, you agree that contributions are under [RPL 1.5](../LICENSE) (or [Peshkov commercial license](https://peshkov.biz/license) where applicable).
