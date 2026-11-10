# Release readiness plan `2.7.0` → `master`

> **Analysis date:** 2026-11-10
> **Branch:** `release/register-email-or-phone` (`de35167`)
> **Comparison base:** `origin/master` (`v2.6.0` / `677b16b`) · merge-base `677b16b`
> **Version plan:** [`docs/RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.7.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Stock `main.Register` Email|Phone `atLeastOneRequired` + form cross-field validators enforced. Feature committed locally (`de35167`); **not pushed** · **no PR**. Tag `v2.7.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / local `dotnet test` filter (re-verified 2026-11-10)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **38** items — ✅ **25** (66%) · 🟨 **0** (0%) · ⬜ **13** (34%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **1** (`de35167`) |
| Files (committed three-dot) | **13** · **+454 / −82** |
| Working tree | dirty — this checklist only (after audit) |
| Remote branch | **absent** (`origin/release/register-email-or-phone` not created) |
| Tests | local ✅ filter Register/validator/UserService — **73** passed |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** (plan finalized / closed; HW `C1` `H32` `M113` `L21`) |

| Area | Files (≈) | Role in release |
|---|---|---|
| Form validators | 1 | Enforce `atLeastOneRequired` / `exactlyOneRequired` |
| Register flow | 2 | `main.Register.json` + FLOWS |
| Collect / UserService | 2 | Blank identity → null |
| Tests | 3 | Flow / factory / UserService |
| docs | 4 | PLAN 2.7.0, CHANGELOG, BREAKING 2.6.0→2.7.0, 2.6.0 published |

---

## Table of contents

1. [New functionality (2.7.0)](#1-new-functionality-270)
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

## 1. New functionality (2.7.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Register identity | Email\|Phone `atLeastOneRequired`; UserName alias only | ✅ in `main.Register.json` + FLOWS |
| F2 | Form cross-field | `atLeastOneRequired` / `exactlyOneRequired` runtime | ✅ `UnifiedFormValidatorFactory` |
| F3 | Blank normalization | collectForm bag + `CreateUserAsync` | ✅ blank → null |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.6.0 to 2.7.0 | Register email-or-phone; validators enforced | ✅ in `de35167` |
| B2 | Hosts / FE | Stop assuming bag always has `Email` | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) empty | ✅ |
| CG2 | Closed H32 · M109–M113 (+ tests); plan **closed** | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Commit feature + docs | ✅ `de35167` (to-master audit may dirty again) |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip | ⬜ remote branch absent |
| P2 | Open PR → `master` | ⬜ |
| P3 | CI green on PR tip | ⬜ |
| P4 | Ready to merge → `master` | ⬜ after push + PR + CI (tag/NuGet = post-merge CI) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) | ✅ finalized `(closed)` 2026-11-11 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.7.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.6.0 to 2.7.0 | ✅ |
| D4 | FLOWS `main.Register` | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ no new leftovers · HW unchanged mid-release |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file (WT after audit) |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Register / validator / UserService coverage | ✅ in `de35167` |
| T2 | Related local filter | ✅ 73 passed (re-run 2026-11-10) |
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
| M1 | Schema / seed scripts | ✅ N/A (no DDL in `origin/master...HEAD`) |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Custom flows with unused `exactlyOneRequired` / `atLeastOneRequired` | Now fail closed — documented in BREAKING | ✅ |
| R2 | Not pushed / no PR | Push branch, open PR, wait CI | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; plan finalized `(closed)`; feature committed `de35167`
- ⬜ **2. Push** branch tip (`origin/release/register-email-or-phone` absent)
- ⬜ **3. PR → `master`** + CI green
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.6.0→2.7.0 / FLOWS (in commit)
- ⬜ **5. Ready to merge** — tag `v2.7.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized
- ✅ Feature set committed (`de35167`)
- ⬜ Branch pushed
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Ready-to-merge gate defined (tag/NuGet out of scope until `master`)
