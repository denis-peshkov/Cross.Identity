# Release readiness plan `2.8.0` → `master`

> **Analysis date:** 2026-11-13
> **Branch:** `release/sms-otp-templates` (`676d8b5`+)
> **Comparison base:** `origin/master` (`v2.7.0` / `4fd1703`) · merge-base `4fd1703`
> **Version plan:** [`docs/RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.8.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** SMS `{Template}-sms` + DE/PL/UA + `uk`→`ua` + LicenseKey docs. PR [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) OPEN · CI green on remote tip `668bc62` (`breaking-approved`). Local **ahead** of origin + WT (`FLOWS` L22 fix · plan · TO-DO · this file). Tag/NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks 26` (re-verified 2026-11-13)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **44** items — ✅ **33** (75%) · 🟨 **8** (18%) · ⬜ **3** (7%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **≥7** (local ahead of `origin/release/sms-otp-templates`) |
| Files (committed three-dot vs master) | **76** · **+2 382 / −693** (plus local WT: FLOWS L22) |
| Working tree | dirty — `FLOWS.md` · plan · TO-DO · this checklist |
| Local vs `origin/release/sms-otp-templates` | **ahead** (remote PR head `668bc62`) |
| Tests | PR CI build ✅ on remote tip · local `dotnet` unavailable (AdvSimd/LSE) |
| PR | [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) OPEN · `breaking-approved` |
| CodeRabbit | PR tip checks include skip/pass history |
| Version-plan open C/H/M/L | **empty** · HW `C1` `H34` `M121` `L23` · **L22 closed** in plan |

| Area | Files (≈) | Role in release |
|---|---|---|
| SendCodeStep | 1 | SMS `{Template}-sms` resolve |
| Templates | ~60 | SMS short + DE/PL/UA email |
| Language | 1+ | ISO `uk` → template key `ua` |
| Tests | 1+ | SendCode SMS path |
| docs / sample | ~8 | SMS-OTP-TEMPLATES, BREAKING/CHANGELOG, README, FLOWS `-sms` |

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
| F4 | Language `uk`→`ua` | ISO alias; `.ua.*` files unchanged | ✅ |
| F5 | LicenseKey docs | prod/dev + log levels in README/CONTRIBUTING | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.7.0 to 2.8.0 | Custom SMS template hosts must ship `-sms` | ✅ |
| B2 | Hosts / custom providers | Add short `-sms` variants | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) empty | ✅ |
| CG2 | Closed H33–H34 · M115–M121 · L22–L23 (+ tests) | ✅ |
| CG3 | PR opened | ✅ [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) |
| CG4 | Commit / push tip + docs WT | 🟨 local ahead + WT (`FLOWS`/plan/TO-DO/to-master) not on PR |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip | 🟨 remote PR head behind local tip / WT |
| P2 | Open PR → `master` | ✅ [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) |
| P3 | CI green on PR tip | ✅ on remote tip `668bc62` (re-check after push) |
| P4 | Ready to merge → `master` | 🟨 after push+CI; GitHub `mergeStateStatus=BLOCKED` (owner) — tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) | ✅ `(closed)` · L22 in Закрыто (local WT) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.8.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.7.0 to 2.8.0 | ✅ |
| D4 | `docs/SMS-OTP-TEMPLATES.md` / README | ✅ |
| D5 | [`FLOWS.md`](../Cross.Identity/FLOWS.md) `sendCode` `-sms` | ✅ fixed (local WT) |
| D6 | [`TO-DO.md`](TO-DO.md) | ✅ L22 removed · HW `C1` `H34` `M121` `L23` |
| D7 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `SendCode` SMS unit coverage | ✅ |
| T2 | Full suite evidence | ✅ PR CI `build` green on remote tip (local host AdvSimd/LSE abort) |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR #26 (remote tip) | ✅ |
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
| R2 | FLOWS.md `sendCode` `-sms` | Fixed in `FLOWS.md` · ✅ #L22 | ✅ |
| R3 | Local tip / WT not on PR #26 | Commit WT · push · re-check CI | ⬜ |
| R4 | Merge UI BLOCKED | Owner merge / ruleset bypass after tip sync | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — plan `(closed)`; L22 closed in FLOWS/plan/TO-DO
- ⬜ **2. Commit/push** WT + ahead tip onto PR #26
- 🟨 **3. PR #26** — CI green on remote tip; re-run after push
- ✅ **4. Docs content** — BREAKING / CHANGELOG / SMS-OTP / FLOWS `-sms`
- 🟨 **5. Ready to merge** — after tip sync + CI; merge UI may stay BLOCKED; tag/NuGet only after `master`

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized `(closed)`
- ✅ Feature set + FLOWS `-sms` (local)
- ⬜ Commit WT + push onto PR #26
- 🟨 PR #26 / CI green on **remote** tip (stale vs local HEAD)
- ✅ PostDeployment N/A
- 🟨 Merge UI `BLOCKED` — owner action after tip sync
- ✅ Tag/NuGet out of scope until `master`
