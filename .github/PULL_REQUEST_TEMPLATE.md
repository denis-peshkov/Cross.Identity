<!-- PR body: short narrative + minimal checklist. English only — used in GitHub history and triage. -->

## Summary

<!-- What changed and why. The PR title should already be the intended squash merge title. Prefix the PR title with BREAKING: when needed. Add "Closes: #0000" if applicable. -->

Closes: #0000

---

## Changes

<!-- Extend `path/to/file.cs` — numbered list; optional scope line at the end. -->

1. …
2. …

**Scope:** N files, ~N lines.

---

## Test plan

<!-- How you verified the change — check off before merge. -->

- [ ] New or updated tests cover the changed behavior
- [ ] `dotnet build Cross.Identity.slnx` — green locally
- [ ] `dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj` — green locally

---

## Risks / notes

<!-- Required for auth, JWT, refresh, OAuth, licensing, PII/logging, or breaking changes. Otherwise write "N/A". -->

N/A


---

## Checklist

- [ ] I have read and followed [CONTRIBUTING.md](../CONTRIBUTING.md).
- [ ] PR title matches the intended squash merge title and lead commit message.
- [ ] There is no other open [pull request](https://github.com/denis-peshkov/Cross.Identity/pulls) for the same fix or feature.
- [ ] **One PR = one feature or one fix** — no unrelated refactors or drive-by formatting.
- [ ] `.editorconfig` respected; no secrets committed.
- [ ] If this PR changes a public flow or step JSON, update [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) and add/update an integration test in `Cross.Identity.Tests/Identity/FlowTests/`.
- [ ] If this PR changes public API, options, or consumer contract, update README / XML docs as needed.
- [ ] If this PR is breaking for NuGet consumers, update [`docs/MIGRATION.md`](../docs/MIGRATION.md) (sole breaking-change list) and prefix the **PR title** with `BREAKING:`.
- [ ] If this PR edits `docs/RELEASE-PLAN-dev-to-master.md`, run `node docs/scripts/release-plan-summary.mjs --write`.
- [ ] If this PR touches auth / JWT / OAuth / licensing / passwords, the risks are described above and the diff contains no secrets.


---

## AI assistance

- [ ] AI was used to generate or assist with this PR. *Describe briefly what AI helped with and what you manually verified.*


---

**License:** By opening this PR, you agree that contributions are under [RPL 1.5](../LICENSE) (or [Peshkov commercial license](https://peshkov.biz/license) where applicable).
