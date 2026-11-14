# Release readiness plan `2.11.0` → `master`

> **Analysis date:** 2026-11-14
> **Branch:** `release/lock-channel-email-require-email` (`a15b1ab` tip; local ahead + WT finalize docs)
> **Comparison base:** `origin/master` (`v2.10.0` / `c30bc00`) · merge-base `c30bc00`
> **Version plan:** [`docs/RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.11.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** `LockChannelAsEmail=true` → email required at `CreateUserAsync` (`ValidationException`). PR [#29](https://github.com/denis-peshkov/Cross.Identity/pull/29). Tag `v2.11.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks` (2026-11-14)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **40** items — ✅ **33** (83%) · 🟨 **2** (5%) · ⬜ **5** (13%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **4** |
| Files | **12** · **+172 / −7** |
| Working tree | dirty — finalize `RELEASE-PLAN-2.11.0` / `TO-DO` HW (ahead 1 vs origin) |
| Remote branch | tracking · **ahead 1** |
| Tests | CI build ✅ on PR #29 |
| PR | [#29](https://github.com/denis-peshkov/Cross.Identity/pull/29) → `master` |
| CodeRabbit | pass (PR #29) |
| Version-plan open C/H/M/L | **empty** · closed H40 · L25 · HW tip `C1` `H40` `M129` `L25` |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 3 | `UserService` gate + options/IUserService docs |
| Tests | 1 | `UserServiceTests` lock + phone-only / email+phone |
| FLOWS | 1 | Register note |
| docs | 5 | BREAKING / CHANGELOG / PLAN 2.11.0 / PLAN 2.10.0 label / TO-DO |
| Labels | 2 | `.github/LABELS` security description |

---

## Table of contents

1. [New functionality (2.11.0)](#1-new-functionality-2110)
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

## 1. New functionality (2.11.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | LockChannelAsEmail create gate | `CreateUserAsync` requires email when lock enabled | ✅ |
| F2 | Host docs | FLOWS Register note + options/XML | ✅ |
| F3 | Labels | security label description refine | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.10.0 to 2.11.0 | Collect email on Register when lock on | ✅ |
| B2 | Phone-only Register + lock | Expect `ValidationException` (not late `sendCode` fail) | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md) empty | ✅ `(closed)` |
| CG2 | Closed H40 · L25 | ✅ |
| CG3 | PR opened | ✅ [#29](https://github.com/denis-peshkov/Cross.Identity/pull/29) |
| CG4 | Finalize docs committed + pushed | ⬜ WT / ahead 1 |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push finalize plan / TO-DO | ⬜ |
| P2 | PR → `master` | ✅ #29 |
| P3 | CI green on PR tip | ✅ `gh pr checks` (build, breaking-gate, CodeQL, …) |
| P4 | Ready to merge → `master` | 🟨 after P1 (tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md) | ✅ `(closed)` WT |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.11.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.10.0 to 2.11.0 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) Register note | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW + Принято sync WT |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `UserServiceTests` LockChannelAsEmail create cases | ✅ |
| T2 | Suite evidence | ✅ via CI build on PR #29 |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | ✅ |
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
| R1 | Hosts with lock + phone-only Register | BREAKING — collect email / catch `ValidationException` | ✅ documented |
| R2 | Finalize plan / TO-DO not on remote tip | Commit + push before merge | ⬜ |
| R3 | Tag before master | Do not tag until after merge | ✅ policy |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H40 closed
- ⬜ **2. Commit/push** finalize `RELEASE-PLAN-2.11.0` + `TO-DO` (ahead/WT)
- ✅ **3. PR → `master`** #29 + CI green
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.10.0→2.11.0 / FLOWS
- 🟨 **5. Ready to merge** — after P1; tag `v2.11.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (`CreateUserAsync` email gate)
- ⬜ Finalize docs committed + pushed to PR tip
- ✅ PR / CI green (#29)
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
