# Release readiness plan `2.10.0` → `master`

> **Analysis date:** 2026-11-14
> **Branch:** `release/registered-template` (`8d4c2e5`)
> **Comparison base:** `origin/master` (`v2.9.0` / `786d24e`) · merge-base `786d24e`
> **Version plan:** [`docs/RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.10.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Welcome templates `register` → `registered`; drop unused stock `confirm-email.*` and Sample host overrides. README TFMs via badge only (**L24** dismissed). No PR yet. Tag `v2.10.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan (drafted 2026-11-14)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **44** items — ✅ **28** (64%) · 🟨 **0** (0%) · ⬜ **16** (36%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **4** |
| Files | **35** · **+101 / −1 493** |
| Working tree | dirty — docs (BREAKING / CHANGELOG / PLAN 2.10.0 / PLAN 2.9.0 published label) |
| Remote branch | **absent** / not tracking |
| Tests | local ⬜ (use CI after push) |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** · closed H38–H39 · M127–M128 · L24 · HW tip `C1` `H37` `M125` `L23` (mid-release; HW bumps at finalize) |

| Area | Files (≈) | Role in release |
|---|---|---|
| Stock templates | 24 | rename `register`→`registered`; drop `confirm-email.*` |
| Provider comment | 1 | EmbeddedResourceProcessDefinitionProvider example key |
| Tests | 1 | placeholders for `registered`; drop confirm-email |
| Sample | 4 | remove host `registered.*` overrides |
| docs | 4+ | PLAN 2.10.0, BREAKING, CHANGELOG, TO-DO M126 |

---

## Table of contents

1. [New functionality (2.10.0)](#1-new-functionality-2100)
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

## 1. New functionality (2.10.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Welcome template rename | Stock `register.*` → `registered.*` (lookup key `registered`) | ✅ |
| F2 | Drop unused confirm-email | Stock `confirm-email.*` locales removed | ✅ |
| F3 | Sample host overrides | Drop Sample.Api `registered.*` copies | ✅ |
| F4 | Deferred send-after-verify | TO-DO `M126` (not this ship) | ✅ deferred |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.9.0 to 2.10.0 | Switch `register` → `registered`; drop package `confirm-email.*` | ✅ WT docs |
| B2 | Hosts loading `register` / `confirm-email` from package | Update keys / ship own templates | ✅ documented |
| B3 | Sample no longer overrides welcome | Use stock embedded `registered.*` | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md) | ✅ empty (L24 dismissed) |
| CG2 | Closed H38–H39 · M127–M128 | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Docs WT committed | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | **L24** README TFMs | ✅ dismissed (badge only) |
| P2 | Commit + push branch tip | ⬜ |
| P3 | Open PR → `master` | ⬜ |
| P4 | CI green on PR tip | ⬜ |
| P5 | Ready to merge → `master` | ⬜ after CI (tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.10.0.md`](RELEASE-PLAN-2.10.0.md) | ✅ draft (WT) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.10.0 | ✅ WT |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.9.0 to 2.10.0 | ✅ WT |
| D4 | README TFMs | ✅ badge only (L24 dismissed) |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ `M126` backlog |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md) | ✅ `(published / closed)` WT |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `EmbeddedTemplatePlaceholderTests` → `registered` | ✅ |
| T2 | Suite evidence | ⬜ via CI after push |
| T3 | PR CI build | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | ⬜ |
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
| R1 | Hosts still using `register` / package `confirm-email` | BREAKING action | ✅ documented |
| R2 | README TFMs prose/section removed (**L24**) | Badge only — accepted | ✅ |
| R3 | Docs uncommitted WT | Commit before PR | ⬜ |
| R4 | No remote / PR | Push + open PR → `master` | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty (**L24** dismissed: TFM badge)
- ⬜ **2. Commit/push** template + docs WT
- ⬜ **3. PR → `master`** + CI green
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.9.0→2.10.0
- ⬜ **5. Ready to merge** — tag `v2.10.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (rename + drop confirm-email + Sample cleanup)
- ⬜ Feature + docs committed + pushed
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
