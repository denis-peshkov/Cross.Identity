# Release readiness plan `2.15.0` → `master`

> **Analysis date:** 2026-11-19
> **Branch:** `release/communication-endpoint-add` (`1ac07bf`)
> **Comparison base:** `origin/master` (`v2.14.0` / `293b859`) · merge-base `293b859`
> **Version plan:** [`docs/RELEASE-PLAN-2.15.0.md`](RELEASE-PLAN-2.15.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.15.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Secondary email/phone via `main.CommunicationEndpointAdd` + `main.CommunicationEndpointSetVerified` (`FlowOperationEnum` 19/20). Additive (not BREAKING). Tag `v2.15.0` / NuGet — **after** merge to `master` (CI). Leftover **M141** (Sample.Api.http still says ValidationException for mergeToken) → TO-DO, not a merge blocker.
> **Sources:** `git` / version plan · **no open PR yet**
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **42** items — ✅ **25** (60%) · 🟨 **6** (14%) · ⬜ **11** (26%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **1** |
| Files | **23** · **+1219 / −22** (+ this checklist WT) |
| Working tree | to-master update pending commit · tip `1ac07bf` = `origin/release/communication-endpoint-add` |
| Remote branch | tracking · up to date (except local to-master) |
| Tests | In delta; no PR CI yet |
| PR | ⬜ none yet |
| CodeRabbit | not run |
| Version-plan open C/H/M/L | **empty** · closed H57 · M143 · L35 · HW still `C1` `H56` `M142` `L34` until finalize · leftover M141 in TO-DO |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 8 | `AddAsync` / `SetVerifiedAsync`; steps/factories/flows; enum 19/20; DI |
| Tests | 5 | Flow / step / factory / service |
| FLOWS / README / rest-client | 3 | Host docs + http samples (+ partial M141 env fix) |
| docs | 3 | CHANGELOG / PLAN 2.15.0 / PLAN 2.14.0 published (+ this to-master) |

---

## Table of contents

1. [New functionality (2.15.0)](#1-new-functionality-2150)
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

## 1. New functionality (2.15.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | `CommunicationEndpointAdd` | Upsert unverified Manual + OTP to submitted address | ✅ |
| F2 | `CommunicationEndpointSetVerified` | Verify OTP then `SetVerifiedAsync` | ✅ |
| F3 | Service API | Public `AddAsync` / `SetVerifiedAsync` | ✅ |
| F4 | Host docs / sample | FLOWS + README + rest-client | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Consumer-breaking NuGet surface | None — additive flows/API/enum 19–20 | ✅ N/A |
| B2 | [`docs/BREAKING.md`](BREAKING.md) | No new From 2.14→2.15 section required | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.15.0.md`](RELEASE-PLAN-2.15.0.md) empty | ✅ |
| CG2 | Closed H57 · M143 · L35 | ✅ |
| CG3 | PR opened | ⬜ none |
| CG4 | Docs on remote tip | ✅ (`1ac07bf`; plan still active / not finalized) |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push feature tip | ✅ |
| P2 | PR → `master` (or `dev` per policy) | ⬜ |
| P3 | CI green on PR tip | ⬜ blocked on PR |
| P4 | Ready to merge → `master` | ⬜ (wait PR/CI; tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.15.0.md`](RELEASE-PLAN-2.15.0.md) | 🟨 draft (not finalized) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.15.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) | ✅ no new section |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) + README | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | 🟨 HW bump on finalize; M141 leftover |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.14.0.md`](RELEASE-PLAN-2.14.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Flow / step / factory / service add + set-verified | ✅ in delta |
| T2 | Suite evidence | ⬜ no PR CI yet |
| T3 | PR CI build | ⬜ no PR |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | ⬜ no PR |
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
| R1 | No PR yet | Open PR before CI / merge gate | ⬜ |
| R2 | Tag before master | Do not tag until after merge | ✅ policy |
| R3 | Sample.Api.http mergeToken still says ValidationException (M141) | Fix or leave in TO-DO | 🟨 deferred |
| R4 | Version plan not finalized | Run «закрой» when ship slice done | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H57 / M143 / L35 closed
- ✅ **2. Feature tip** on remote (`1ac07bf`)
- ⬜ **3. PR** — open → `master` (or `dev`) + CI green
- 🟨 **4. Docs** — CHANGELOG/FLOWS done; finalize version plan when ready
- ⬜ **5. Ready to merge** — wait PR/CI; tag `v2.15.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (Add + SetVerified OTP flows)
- 🟨 Finalize version plan still pending
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
