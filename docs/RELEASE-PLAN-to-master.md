# Release readiness plan `2.13.0` → `master`

> **Analysis date:** 2026-11-18
> **Branch:** `feature/account-merge-external-login` (`315677f`)
> **Comparison base:** `origin/master` (`v2.12.0` / `bb33195`) · merge-base `bb33195`
> **Version plan:** [`docs/RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.13.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** OAuth link conflict → `mergeToken` + `main.ExternalLoginMerge`; DbUp `auth.AccountMergePendings`. PR [#31](https://github.com/denis-peshkov/Cross.Identity/pull/31) (`BREAKING:`). Tag `v2.13.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks` (2026-11-18: build **in progress**; latest breaking-gate ✅; earlier breaking-gate failures on prior SHAs)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **43** items — ✅ **35** (81%) · 🟨 **6** (14%) · ⬜ **2** (5%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **6** |
| Files | **51** · **+1496 / −118** (+ this checklist WT) |
| Working tree | to-master refresh pending commit · tip `315677f` = `origin/feature/account-merge-external-login` |
| Remote branch | tracking · up to date (except local to-master) |
| Tests | CI build 🟨 in progress on PR #31 tip |
| PR | [#31](https://github.com/denis-peshkov/Cross.Identity/pull/31) → `master` · MERGEABLE |
| CodeRabbit | local agent `2026-11-18` closed in plan; GitHub CodeRabbit pending on PR |
| Version-plan open C/H/M/L | **empty** · closed H43–H50 · M133–M138 · L28 · HW `C1` `H50` `M138` `L28` |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 15 | Merge mint/confirm, entity/EF, flow/step/DI, enums |
| Tests | 20 | Service + flow + factory; `IsActive` fixtures |
| Infrastructure scripts | 7 | `1_12` / `2_03` AccountMergePendings ×3 + Scripts README |
| FLOWS / README / Sample.http | 3+ | Merge flow docs + http sample + env example |
| docs | 6 | BREAKING / CHANGELOG / PLAN 2.13.0 / PLAN 2.12.0 / TO-DO / to-master |

---

## Table of contents

1. [New functionality (2.13.0)](#1-new-functionality-2130)
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

## 1. New functionality (2.13.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Link-conflict merge token | `ThrowIfLinkConflictNeedsMergeAsync` → `AccountMergePendings` + `alreadyLinked` failures | ✅ |
| F2 | `ExternalLoginMerge` flow | Step/factory/DI + `MergeAccountsAsync` (move externals/endpoints, deactivate, revoke) | ✅ |
| F3 | Audit / revoke reason | `UserAccountsMerged` + `ACCOUNT_MERGED` | ✅ |
| F4 | Host docs / sample | FLOWS + README + Scripts README + Sample.Api.http | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.12.0 to 2.13.0 — DbUp | Run `1_12` / ship with `2_03` AccountMergePendings | ✅ |
| B2 | Link-conflict `ValidationException` shape | Surface `mergeToken`; call `ExternalLoginMerge` | ✅ documented |
| B3 | `UserAccountEntity.IsActive` no C# default `true` | Set explicitly on create / fixtures | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md) empty | ✅ `(closed)` |
| CG2 | Closed H43–H50 · M133–M138 · L28 | ✅ |
| CG3 | PR opened | ✅ [#31](https://github.com/denis-peshkov/Cross.Identity/pull/31) |
| CG4 | Finalize docs on remote tip | ✅ (`315677f`) |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push finalize plan / TO-DO / to-master | ✅ |
| P2 | PR → `master` | ✅ #31 |
| P3 | CI green on PR tip | 🟨 build in progress · latest breaking-gate ✅ |
| P4 | Ready to merge → `master` | ⬜ (wait build green; tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md) | ✅ `(closed)` |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.13.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.12.0 to 2.13.0 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) ExternalLoginMerge | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW + Принято synced |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `ExternalLoginServiceTests` merge mint / confirm / empty token | ✅ in delta |
| T2 | `Main_ExternalOAuth_FlowTests` + step factory | ✅ in delta |
| T3 | PR CI build | 🟨 in progress (#31) |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | 🟨 build in progress |
| C2 | Tag / NuGet pipeline | ✅ post-merge on `master` (not a pre-merge gate) |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | Schema scripts `AccountMergePendings` | ✅ `1_12` PreDeployment + `2_03` Initial ×3 providers |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Existing DBs without `AccountMergePendings` | BREAKING — run `1_12` before using merge | ✅ documented |
| R2 | Hosts ignore `mergeToken` on link conflict | Surface token + `ExternalLoginMerge` UX | ✅ documented |
| R3 | Tag before master | Do not tag until after merge | ✅ policy |
| R4 | CI not fully green yet | Wait `.NET` build on #31 tip | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H43–H50 / M133–M138 / L28 closed
- ✅ **2. Commit/push** finalize docs on tip (`315677f`)
- 🟨 **3. PR → `master`** #31 · MERGEABLE · build in progress
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.12.0→2.13.0 / FLOWS / README
- ⬜ **5. Ready to merge** — wait CI green; tag `v2.13.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (merge token + ExternalLoginMerge + DbUp)
- ✅ Finalize docs on remote tip
- 🟨 PR open (#31) · CI not fully green yet
- ✅ PostDeployment N/A (PreDeployment `1_12` + Initial `2_03` shipped)
- ✅ Tag/NuGet out of scope until `master`
