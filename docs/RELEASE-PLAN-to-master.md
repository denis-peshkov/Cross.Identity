# Release readiness plan `2.16.0` → `master`

> **Analysis date:** 2026-11-20
> **Branch:** `release/oauth-email-endpoint-always-verified` (`dfd3052`)
> **Comparison base:** `origin/master` (`v2.15.0` / `75c4b78`) · merge-base `75c4b78`
> **Version plan:** [`docs/RELEASE-PLAN-2.16.0.md`](RELEASE-PLAN-2.16.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.16.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Linked OAuth email communication endpoint always `IsVerified = true` (`ExternalProvider`). `ExternalOAuthProfile.EmailVerified` still gates account `EmailVerified` / collision / merge. **BREAKING** From 2.15→2.16. Tag `v2.16.0` / NuGet — **after** merge to `master` (CI). Leftover **M141** (Sample.Api.http mergeToken wording) → TO-DO, not a merge blocker.
> **Sources:** `git` / version plan · [PR #35](https://github.com/denis-peshkov/Cross.Identity/pull/35)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **46** items — ✅ **33** (72%) · 🟨 **11** (24%) · ⬜ **2** (4%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **1** |
| Files | **8** · **+119 / −7** (+ this checklist WT; finalize plan/TO-DO uncommitted) |
| Working tree | `RELEASE-PLAN-2.16.0` + `TO-DO` finalize pending commit · tip `dfd3052` = remote |
| Remote branch | tracking · up to date (except local finalize WT) |
| Tests | In delta; PR build ✅ |
| PR | [#35](https://github.com/denis-peshkov/Cross.Identity/pull/35) `BREAKING: always verify linked OAuth email endpoints` → `master` |
| CodeRabbit | PR bot ran (SUCCESS) |
| Version-plan open C/H/M/L | **empty** · closed H60 · M146 · M147 · L40 · HW `C1` `H60` `M147` `L40` (finalize WT) · leftover M141 in TO-DO |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 2 | `SyncProviderEmailEndpointAsync` always verified; profile XML |
| Tests | 1 | Microsoft Graph / unverified-attestation → endpoint verified |
| FLOWS | 1 | OAuth registration note |
| docs | 4 | BREAKING / CHANGELOG / PLAN 2.16.0 / PLAN 2.15 published (+ this to-master) |

---

## Table of contents

1. [New functionality (2.16.0)](#1-new-functionality-2160)
2. [Breaking / consumer notes](#2-breaking--consumer-notes)
3. [Code gate](#3-code-gate)
4. [Practical ship steps](#4-practical-ship-steps)
5. [Documentation](#5-documentation)
6. [Automated tests](#6-automated-tests)
7. [CI / package](#7-ci--package)
8. [Database](#8-database)
9. [Blockers and risks](#9-blockers-and-risks)
10. [Go / No-Go and execution order](#10-recommended-work-order-release-gate)

---

## 1. New functionality (2.16.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | OAuth endpoint sync | Linked provider email → endpoint `IsVerified = true` always | ✅ |
| F2 | Account flag unchanged | `UsersAccounts.EmailVerified` still from `ExternalOAuthProfile.EmailVerified` | ✅ |
| F3 | Tests | Graph / unverified attestation assert endpoint verified | ✅ |
| F4 | Host docs | FLOWS + BREAKING + CHANGELOG | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Consumer-breaking NuGet surface | Endpoint for linked OAuth email always verified (was profile flag) | ✅ documented |
| B2 | [`docs/BREAKING.md`](BREAKING.md) | From 2.15.0→2.16.0 present | ✅ |
| B3 | PR title | `BREAKING:` prefix on [#35](https://github.com/denis-peshkov/Cross.Identity/pull/35) | ✅ |
| B4 | `breaking-approved` label | Required for breaking-gate | 🟨 confirm on tip before merge |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.16.0.md`](RELEASE-PLAN-2.16.0.md) empty | ✅ |
| CG2 | Closed H60 · M146 · M147 · L40 | ✅ |
| CG3 | PR opened | ✅ [#35](https://github.com/denis-peshkov/Cross.Identity/pull/35) |
| CG4 | Docs on remote tip | 🟨 tip `dfd3052`; finalize plan/TO-DO still local WT |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push feature tip | ✅ `dfd3052` |
| P2 | PR → `master` | ✅ [#35](https://github.com/denis-peshkov/Cross.Identity/pull/35) |
| P3 | CI green on PR tip | 🟨 build ✅; breaking-gate had early fail then success — verify label / latest |
| P4 | Ready to merge → `master` | 🟨 push finalize WT; then merge; tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.16.0.md`](RELEASE-PLAN-2.16.0.md) | ✅ `(closed)` (finalize WT) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.16.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) From 2.15→2.16 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW `C1` `H60` `M147` `L40`; Принято synced (finalize WT) |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.15.0.md`](RELEASE-PLAN-2.15.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | ExternalLogin Microsoft unverified → endpoint verified | ✅ in delta |
| T2 | Suite evidence | ✅ PR `.NET` build SUCCESS |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | 🟨 build ✅; confirm breaking-gate + `breaking-approved` |
| C2 | Tag / NuGet pipeline | ✅ post-merge on `master` (not a pre-merge gate) |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | Schema / seed scripts | ✅ N/A (no DDL) |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Finalize docs not on remote tip | Commit + push plan/TO-DO/to-master before merge | 🟨 |
| R2 | Tag before master | Do not tag until after merge | ✅ policy |
| R3 | Sample.Api.http mergeToken still says ValidationException (M141) | Leave in TO-DO | 🟨 deferred |
| R4 | Always-verified endpoint may block address squat for other accounts | Accepted (#H60 / Принято); hosts must understand | ✅ accepted |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H60 / M146 / M147 / L40 closed
- ✅ **2. Feature tip** on remote (`dfd3052`) + PR #35
- 🟨 **3. CI** — build green; confirm breaking-gate / `breaking-approved`
- 🟨 **4. Docs** — push finalize WT (plan closed + TO-DO HW/Принято)
- ⬜ **5. Ready to merge** — merge #35 → `master`; tag `v2.16.0` / NuGet only after merge (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty · plan `(closed)`
- ✅ Feature set on branch (OAuth endpoint always verified)
- ✅ BREAKING From 2.15→2.16 documented
- 🟨 Finalize WT pushed to PR tip
- 🟨 Breaking-gate / label confirmed
- ⬜ Merge to `master`
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
