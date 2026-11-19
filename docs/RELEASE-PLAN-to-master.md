# Release readiness plan `2.14.0` → `master`

> **Analysis date:** 2026-11-19
> **Branch:** `release/oauth-link-conflict-409` (`2b3ae9f`)
> **Comparison base:** `origin/master` (`v2.13.0` / `7baa985`) · merge-base `7baa985`
> **Version plan:** [`docs/RELEASE-PLAN-2.14.0.md`](RELEASE-PLAN-2.14.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.14.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** OAuth link conflict → `ConflictException` / HTTP **409**; mint via Suppress + `IDbContextFactory`; `Cross.ErrorHandlers` **7.7.0**. PR [#33](https://github.com/denis-peshkov/Cross.Identity/pull/33) (`BREAKING:`). Tag `v2.14.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan. Leftover **M141** (rest-client wording) → TO-DO, not a merge blocker.
> **Sources:** `git` / version plan / `gh pr checks` (2026-11-19: build **in progress**; breaking-gate ✅)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **45** items — ✅ **36** (80%) · 🟨 **7** (16%) · ⬜ **2** (4%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **2** |
| Files | **28** · **+663 / −72** (+ this checklist WT) |
| Working tree | to-master refresh pending commit · tip `2b3ae9f` = `origin/release/oauth-link-conflict-409` |
| Remote branch | tracking · up to date (except local to-master) |
| Tests | CI build 🟨 in progress on PR #33 tip |
| PR | [#33](https://github.com/denis-peshkov/Cross.Identity/pull/33) → `master` · MERGEABLE |
| CodeRabbit | not run on this tip |
| Version-plan open C/H/M/L | **empty** (finalized) · closed H54–H56 · M142 · L33–L34 · leftover M141 → TO-DO · HW `C1` `H56` `M142` `L34` |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | ~8 | ConflictException mint; factory Suppress; DI overload; ErrorHandlers 7.7 |
| Tests | ~8 | 409 ProblemDetails; PG ambient-tx merge-pending regression; sharing factory |
| Sample / README / FLOWS / CONTRIBUTING | ~5 | Host wiring `AddCrossIdentity(..., configureDbContext)` |
| Tooling | ~2 | EditorConfig charset; Scripts README |
| docs | ~4 | BREAKING / CHANGELOG / PLAN 2.14.0 / PLAN 2.13.0 (+ this to-master) |

---

## Table of contents

1. [New functionality (2.14.0)](#1-new-functionality-2140)
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

## 1. New functionality (2.14.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Link conflict → `ConflictException` | `AccountMergeRequired` + throw (was ValidationException) | ✅ |
| F2 | HTTP 409 mapping | `ToConflictProblemDetails` + ErrorHandlers **7.7.0** | ✅ |
| F3 | Durable pending mint | Suppress + `IDbContextFactory` fresh context | ✅ |
| F4 | DI host wiring | `AddCrossIdentity(configuration, configureDbContext)`; Sample.Api | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.13.0 to 2.14.0 | Catch ConflictException / 409; not ValidationException / 406 | ✅ |
| B2 | `IDbContextFactory<IdentityContext>` required | Prefer new overload or register factory yourself | ✅ documented |
| B3 | `Cross.ErrorHandlers` ≥7.7.0 | ≤7.6 mapped Conflict → 400 | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.14.0.md`](RELEASE-PLAN-2.14.0.md) empty | ✅ `(closed)` |
| CG2 | Closed H54–H56 · M142 · L33–L34 | ✅ |
| CG3 | PR opened | ✅ [#33](https://github.com/denis-peshkov/Cross.Identity/pull/33) |
| CG4 | Finalize docs on remote tip | ✅ (`2b3ae9f`) |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push finalize plan / TO-DO | ✅ |
| P2 | PR → `master` | ✅ #33 |
| P3 | CI green on PR tip | 🟨 build in progress · breaking-gate ✅ |
| P4 | Ready to merge → `master` | ⬜ (wait build green; tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.14.0.md`](RELEASE-PLAN-2.14.0.md) | ✅ `(closed)` |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.14.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.13.0 to 2.14.0 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) + README / Sample | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW + Принято + leftover M141 |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.13.0.md`](RELEASE-PLAN-2.13.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | ConflictException / `ToConflictProblemDetails` 409 | ✅ in delta |
| T2 | PG Testcontainers ambient-tx merge-pending regression | ✅ in delta |
| T3 | PR CI build | 🟨 in progress (#33) |

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
| M1 | Schema / seed scripts | ✅ N/A (no new DDL; uses `AccountMergePendings` from prior release) |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Hosts still catch ValidationException for link conflict | BREAKING — switch to ConflictException / 409 | ✅ documented |
| R2 | Hosts without `IDbContextFactory` | Register factory or use new `AddCrossIdentity` overload | ✅ documented |
| R3 | ErrorHandlers ≤7.6 → Conflict **400** | Bump to ≥7.7.0 | ✅ documented |
| R4 | Tag before master | Do not tag until after merge | ✅ policy |
| R5 | CI not fully green yet | Wait `.NET` build on #33 tip | 🟨 |
| R6 | rest-client ValidationException wording (M141) | Leftover in TO-DO — not a merge blocker | 🟨 deferred |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H54–H56 / M142 / L33–L34 closed
- ✅ **2. Commit/push** finalize docs on tip (`2b3ae9f`)
- 🟨 **3. PR → `master`** #33 · MERGEABLE · build in progress
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.13.0→2.14.0 / FLOWS / README
- ⬜ **5. Ready to merge** — wait CI green; tag `v2.14.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (ConflictException 409 + factory mint + ErrorHandlers 7.7)
- ✅ Finalize docs on remote tip
- 🟨 PR open (#33) · CI not fully green yet
- ✅ PostDeployment N/A (no new DDL)
- ✅ Tag/NuGet out of scope until `master`
