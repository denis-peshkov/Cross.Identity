# Release readiness plan `2.7.0` → `master`

> **Analysis date:** 2026-11-10
> **Branch:** `release/register-email-or-phone` (WT on `677b16b` / `v2.6.0`)
> **Comparison base:** `origin/master` (`v2.6.0` / `677b16b`) · merge-base `677b16b`
> **Version plan:** [`docs/RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.7.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Stock `main.Register` Email|Phone `atLeastOneRequired` + form cross-field validators enforced. Tip = master; **all work uncommitted** (no remote branch / no PR). Tag `v2.7.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / local `dotnet test` filter (re-verified 2026-11-10)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **38** items — ✅ **24** (63%) · 🟨 **0** (0%) · ⬜ **14** (37%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **0** (tip = `v2.6.0`) |
| Files (WT vs tip) | **10** · **+296 / −10** (feature + docs; excl. this checklist) |
| Working tree | dirty (feature + `RELEASE-PLAN-2.7.0` / BREAKING / CHANGELOG / to-master) |
| Local vs `origin/release/register-email-or-phone` | n/a / unpushed branch tip = master |
| Tests | local ✅ related filters (73 passed) |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** |

| Area | Files (≈) | Role in release |
|---|---|---|
| Form validators | 1 | Enforce `atLeastOneRequired` / `exactlyOneRequired` |
| Register flow | 2 | `main.Register.json` + FLOWS |
| Collect / UserService | 2 | Blank identity → null |
| Tests | 3 | Flow / factory / UserService |
| docs | 3+ | PLAN 2.7.0, CHANGELOG, BREAKING 2.6.0→2.7.0 |

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
| F1 | Register identity | Email\|Phone `atLeastOneRequired`; UserName alias only | ✅ |
| F2 | Form cross-field | `atLeastOneRequired` / `exactlyOneRequired` runtime | ✅ |
| F3 | Blank normalization | collectForm bag + `CreateUserAsync` | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.6.0 to 2.7.0 | Register email-or-phone; validators enforced | ✅ |
| B2 | Hosts / FE | Stop assuming bag always has `Email` | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) empty | ✅ |
| CG2 | Closed M109–M112 · H32; feature rows | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Commit feature + docs WT | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push branch tip | ⬜ |
| P2 | Open PR → `master` | ⬜ |
| P3 | CI green on PR tip | ⬜ |
| P4 | Ready to merge → `master` | ⬜ go after CI (tag/NuGet = post-merge CI) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) | ✅ draft |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.7.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.6.0 to 2.7.0 | ✅ |
| D4 | FLOWS `main.Register` | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ no new leftovers |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Register / validator / UserService coverage | ✅ |
| T2 | Related local filter | ✅ 73 passed |
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
| M1 | Schema / seed scripts | ✅ N/A (no DDL this release) |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Custom flows with unused `exactlyOneRequired` / `atLeastOneRequired` | Now fail closed — documented in BREAKING | ✅ |
| R2 | Uncommitted WT | Commit before PR | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; plan drafted
- ⬜ **2. Commit/push** feature + docs WT
- ⬜ **3. PR → `master`** + CI green
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.6.0→2.7.0 / FLOWS
- ⬜ **5. Ready to merge** — tag `v2.7.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set in WT (Register email-or-phone + validators)
- ⬜ Feature + docs committed + pushed
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Ready-to-merge gate defined (tag/NuGet out of scope until `master`)
