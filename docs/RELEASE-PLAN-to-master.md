# Release readiness plan `2.7.0` → `master`

> **Analysis date:** 2026-11-11
> **Branch:** `release/register-email-or-phone` (`d92e393`)
> **Comparison base:** `origin/master` (`v2.6.0` / `677b16b`) · merge-base `677b16b`
> **Version plan:** [`docs/RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.7.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Stock `main.Register` Email|Phone `atLeastOneRequired` + form cross-field validators. Tip in sync with origin · PR [#25](https://github.com/denis-peshkov/Cross.Identity/pull/25) OPEN · CI on tip green (`breaking-approved`). Local WT: M114 dismissed + plan/TO-DO/to-master refresh (not in tip). Tag `v2.7.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks 25` / local filter (re-verified 2026-11-11)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **40** items — ✅ **30** (75%) · 🟨 **8** (20%) · ⬜ **2** (5%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **2** (`de35167` · `d92e393`) |
| Files (committed three-dot) | **14** · **+518 / −83** |
| Working tree | dirty — plan / TO-DO / this checklist (M114 dismiss + header sync) |
| Local vs `origin/release/register-email-or-phone` | tip **in sync**; docs WT ahead |
| Tests | local ✅ filter — **75** passed · PR CI build ✅ |
| PR | [#25](https://github.com/denis-peshkov/Cross.Identity/pull/25) OPEN · labels `breaking-changes` + `breaking-approved` |
| CodeRabbit | `2026-11-10` · PR check SUCCESS · M114 dismissed locally |
| Version-plan open C/H/M/L | **empty** (plan `(closed)`; HW `C1` `H32` `M114` `L21`) |

| Area | Files (≈) | Role in release |
|---|---|---|
| Form validators | 1 | Enforce `atLeastOneRequired` / `exactlyOneRequired` + form-level failures |
| Register flow | 2 | `main.Register.json` + FLOWS |
| Collect / UserService | 2 | Blank identity → null |
| Tests | 3 | Flow / factory / UserService |
| docs | 4 | PLAN 2.7.0, CHANGELOG, BREAKING 2.6.0→2.7.0, TO-DO |

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
| F2 | Form cross-field | `atLeastOneRequired` / `exactlyOneRequired` runtime + form-level `AddFailure` | ✅ |
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
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) empty | ✅ tip + local |
| CG2 | Closed H32 · M109–M114 (+ tests); plan **closed** | ✅ M114 dismissed (local WT) |
| CG3 | PR opened | ✅ [#25](https://github.com/denis-peshkov/Cross.Identity/pull/25) |
| CG4 | Commit feature + docs | 🟨 tip `de35167`+`d92e393`; plan/TO-DO/to-master WT dirty |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip | ✅ in sync with origin |
| P2 | Open PR → `master` | ✅ [#25](https://github.com/denis-peshkov/Cross.Identity/pull/25) |
| P3 | CI green on PR tip | ✅ build · breaking-gate · Sonar · CodeQL · Owner-only · CodeRabbit |
| P4 | Ready to merge → `master` | 🟨 CI go; GitHub `mergeStateStatus=BLOCKED` — tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.7.0.md`](RELEASE-PLAN-2.7.0.md) | 🟨 `(closed)` · open empty · M114/Принято/delta/PR refreshed locally (uncommitted) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.7.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.6.0 to 2.7.0 | ✅ |
| D4 | FLOWS `main.Register` | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | 🟨 HW → `M114` + LockChannel Принято locally (uncommitted) |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Register / validator / UserService coverage | ✅ |
| T2 | Related local filter | ✅ 75 passed (re-run 2026-11-11) |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR #25 (current tip) | ✅ |
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
| R2 | Merge UI BLOCKED despite tip CI green | Owner merge / ruleset bypass if needed | 🟨 |
| R3 | Uncommitted docs (M114 dismiss + checklist) | Commit before next push / merge | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; plan `(closed)`; M114 dismissed
- ✅ **2. Push** branch tip — in sync with origin
- ✅ **3. PR #25** + CI green on tip (`breaking-approved`)
- 🟨 **4. Docs content** — BREAKING/CHANGELOG/FLOWS on tip; M114/HW/to-master local WT
- 🟨 **5. Ready to merge** — CI go; GitHub merge still BLOCKED; commit docs WT first; tag/NuGet only after `master`

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized `(closed)`
- ✅ Feature set committed + pushed
- ⬜ Docs WT (M114 dismiss / HW / to-master) committed
- ✅ PR #25 / CI green on tip
- ✅ PostDeployment N/A
- 🟨 Merge UI not clean (`BLOCKED`) — owner action
- ✅ Tag/NuGet out of scope until `master`
