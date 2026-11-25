# Release readiness plan `2.18.0` → `master`

> **Analysis date:** 2026-11-25
> **Branch:** `release/oauth-email-always-verified` (`d75be63` tip; local finalize WT on plan/TO-DO)
> **Comparison base:** `origin/master` (`v2.17.0` / `324b279`) · merge-base `324b279`
> **Version plan:** [`docs/RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.18.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Any non-empty OAuth provider email → `EmailVerified = true`; auto-link only verified same-email; else new verified account; OAuth `UserName` left null. **BREAKING** From 2.17→2.18. Tag `v2.18.0` / NuGet — **after** merge to `master` (CI). Version-plan open C/H/M/L empty (`(closed)`).
> **Sources:** `git` / version plan · [PR #37](https://github.com/denis-peshkov/Cross.Identity/pull/37)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **51** items — ✅ **42** (82%) · 🟨 **7** (14%) · ⬜ **2** (4%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **4** |
| Files | **11** · **+439 / −148** (committed); WT also finalize plan/TO-DO + this checklist |
| Working tree | Finalize `RELEASE-PLAN-2.18.0` + `TO-DO` (+ this file) not on remote tip yet; branch **ahead 1** of `origin` |
| Remote branch | tracking · push finalize + ahead commit before merge |
| Tests | In delta; PR `.NET` **build** ✅ |
| PR | [#37](https://github.com/denis-peshkov/Cross.Identity/pull/37) `BREAKING: treat OAuth provider emails as verified when present` → `master` |
| CodeRabbit | PR bot SUCCESS |
| Version-plan open C/H/M/L | **empty** · closed H63–H65 · M152 · M154 · L45 · L47–L50 · HW `C1` `H65` `M154` `L50` (finalize WT) |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 4 | Always-verified provider email; verified-only same-email auto-link; UserName null |
| Tests | 1 | Verified account asserts; squat→new; EnsureProviderEmailVerified unit |
| FLOWS | 1 | OAuth callback / Facebook notes |
| docs | 5+ | BREAKING / CHANGELOG / PLAN 2.18.0 / PLAN 2.17 published / to-master |

---

## Table of contents

1. [New functionality (2.18.0)](#1-new-functionality-2180)
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

## 1. New functionality (2.18.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Provider email always verified | Non-empty email → `ExternalOAuthProfile.EmailVerified = true` (providers + CompleteAsync) | ✅ |
| F2 | Account email verified | New OAuth registration sets `UsersAccounts.EmailVerified = true` | ✅ |
| F3 | Same-email auto-link | Only existing **verified** same-email; else new verified account (no unverified reuse) | ✅ |
| F4 | OAuth UserName | New OAuth account leaves `UserName` / `NormalizedUserName` null | ✅ |
| F5 | Tests | Verified asserts; squat→new; EnsureProviderEmailVerified coverage | ✅ |
| F6 | Host docs | FLOWS + BREAKING + CHANGELOG | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Consumer-breaking NuGet surface | Always-verified provider email; verified-only auto-link; UserName null | ✅ documented |
| B2 | [`docs/BREAKING.md`](BREAKING.md) | From 2.17.x→2.18.0 present | ✅ |
| B3 | PR title | `BREAKING:` prefix on [#37](https://github.com/denis-peshkov/Cross.Identity/pull/37) | ✅ |
| B4 | `breaking-approved` label | Present on #37 | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) empty | ✅ |
| CG2 | Closed H63–H65 · M152 · M154 · L45 · L47–L50 | ✅ |
| CG3 | PR opened | ✅ [#37](https://github.com/denis-peshkov/Cross.Identity/pull/37) |
| CG4 | Docs on remote tip | 🟨 tip `d75be63` (+1 not pushed); finalize plan/TO-DO still local WT |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push feature tip | 🟨 feature tip mostly remote; push ahead-1 + finalize WT |
| P2 | PR → `master` | ✅ [#37](https://github.com/denis-peshkov/Cross.Identity/pull/37) |
| P3 | CI green on PR tip | ✅ build · breaking-gate · CodeQL · CodeRabbit · Sonar (on last pushed tip) |
| P4 | Ready to merge → `master` | 🟨 push finalize WT; re-check CI; then merge; tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) | ✅ `(closed)` (finalize WT) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.18.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) From 2.17→2.18 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW `C1` `H65` `M154` `L50`; Принято synced (finalize WT) |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.17.0.md`](RELEASE-PLAN-2.17.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Facebook / Microsoft / Google → account `EmailVerified` | ✅ in delta |
| T2 | Unverified squat → new verified account | ✅ in delta |
| T3 | `EnsureProviderEmailVerified` unit coverage | ✅ in delta |
| T4 | Suite evidence | ✅ PR `.NET` build SUCCESS |
| T5 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | ✅ build + breaking-gate (+ CodeQL / CodeRabbit / Sonar) |
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
| R1 | Finalize docs / ahead-1 not on remote tip | Commit + push plan/TO-DO/to-master (and ahead commit) before merge | 🟨 |
| R2 | Tag before master | Do not tag until after merge | ✅ policy |
| R3 | Trusting Facebook/Graph email as verified | Accepted (Принято); hosts update UI copy | ✅ accepted |
| R4 | Unverified same-email not reused | Accepted; OAuth creates new verified account | ✅ accepted |
| R5 | OAuth `UserName` null | Hosts must not assume email/display as username | ✅ accepted |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H63–H65 / M152 / M154 / L45 / L47–L50 closed
- 🟨 **2. Feature tip** — push ahead-1 + finalize WT onto remote / PR tip
- ✅ **3. CI** — build + breaking-gate green; `breaking-approved` present
- 🟨 **4. Docs** — push finalize `(closed)` plan + TO-DO HW/Принято
- ⬜ **5. Ready to merge** — merge #37 → `master`; tag `v2.18.0` / NuGet only after merge (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty · plan `(closed)`
- ✅ Feature set on branch (always-verified provider email + verified-only auto-link + UserName null)
- ✅ BREAKING From 2.17→2.18 documented
- 🟨 Finalize WT / ahead commit pushed to PR tip
- ✅ Breaking-gate / `breaking-approved`
- ⬜ Merge to `master`
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
