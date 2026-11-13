# Release readiness plan `2.8.0` → `master`

> **Analysis date:** 2026-11-13
> **Branch:** `release/sms-otp-templates` (`18c329b`)
> **Comparison base:** `origin/master` (`v2.7.0` / `4fd1703`) · merge-base `4fd1703`
> **Version plan:** [`docs/RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.8.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** SMS `{Template}-sms` OTP + DE/PL/UA email locales. Tip in sync with origin; **no PR**. Open **L22** (FLOWS.md). Tag `v2.8.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan (drafted 2026-11-13)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **40** items — ✅ **22** (55%) · 🟨 **3** (8%) · ⬜ **15** (38%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **2** (`612c5aa` · `18c329b`) |
| Files (committed three-dot) | **67** · **+2 171 / −626** |
| Working tree | dirty — plan / BREAKING / CHANGELOG / to-master (+ 2.7.0 published label) |
| Local vs `origin/release/sms-otp-templates` | tip **in sync**; docs WT ahead |
| Tests | local ⬜ (`dotnet` AdvSimd/LSE abort in this env) |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** (plan finalized / closed; L22 → TO-DO) · HW `C1` `H34` `M118` `L22` |

| Area | Files (≈) | Role in release |
|---|---|---|
| SendCodeStep | 1 | SMS `{Template}-sms` resolve |
| Templates | ~60 | SMS short + DE/PL/UA email |
| Tests | 1 | SendCode SMS path |
| docs / sample | ~6 | SMS-OTP-TEMPLATES, README/CONTRIBUTING, Sample RO drop |

---

## Table of contents

1. [New functionality (2.8.0)](#1-new-functionality-280)
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

## 1. New functionality (2.8.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | SMS OTP templates | `{Template}-sms` resolve + stock short bodies | ✅ |
| F2 | Email locales | DE/PL/UA register/verify/reset/confirm/password-changed | ✅ |
| F3 | Sample cleanup | Drop RO register/verify; `verify-sms` stubs | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.7.0 to 2.8.0 | Custom SMS template hosts must ship `-sms` | ✅ drafted |
| B2 | Hosts / custom providers | Add short `-sms` variants | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) empty | ✅ finalized; L22 → TO-DO |
| CG2 | Closed H33–H34 · M115–M118 (+ tests) | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Commit plan / BREAKING / CHANGELOG / to-master WT | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip (after docs commit) | 🟨 tip synced; docs WT not pushed |
| P2 | Open PR → `master` | ⬜ |
| P3 | CI green on PR tip | ⬜ |
| P4 | Ready to merge → `master` | ⬜ after L22 + PR + CI (tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) | ✅ finalized `(closed)` 2026-11-13 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.8.0 | ✅ local WT |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.7.0 to 2.8.0 | ✅ local WT |
| D4 | `docs/SMS-OTP-TEMPLATES.md` / README | ✅ on tip |
| D5 | [`FLOWS.md`](../Cross.Identity/FLOWS.md) `sendCode` `-sms` | ⬜ leftover **L22** in TO-DO |
| D6 | [`TO-DO.md`](TO-DO.md) | ✅ L22 harvested · HW `C1` `H34` `M118` `L22` · Принято synced |
| D7 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `SendCode` SMS unit coverage | ✅ in tip |
| T2 | Local `dotnet test` | ⬜ env AdvSimd/LSE abort |
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
| R1 | Custom template hosts missing `-sms` | BREAKING + SMS-OTP-TEMPLATES | ✅ |
| R2 | FLOWS.md out of sync | Leftover **L22** in TO-DO | 🟨 deferred |
| R3 | Docs WT uncommitted | Commit plan/BREAKING/CHANGELOG/to-master | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — plan finalized `(closed)`; L22 → TO-DO
- ⬜ **2. Commit/push** plan · BREAKING · CHANGELOG · to-master (+ FLOWS if fixed)
- ⬜ **3. PR → `master`** + CI green
- 🟨 **4. Docs content** — BREAKING/CHANGELOG/SMS-OTP drafted; FLOWS pending
- ⬜ **5. Ready to merge** — tag `v2.8.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized `(closed)`
- ✅ Feature set on tip (SMS templates + locales)
- ⬜ Docs WT committed + pushed
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
