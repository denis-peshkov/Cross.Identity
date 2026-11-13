# Release readiness plan `2.8.0` → `master`

> **Analysis date:** 2026-11-13
> **Branch:** `release/sms-otp-templates` (`c1f2474`)
> **Comparison base:** `origin/master` (`v2.7.0` / `4fd1703`) · merge-base `4fd1703`
> **Version plan:** [`docs/RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.8.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** SMS `{Template}-sms` + DE/PL/UA + `uk`→`ua` + LicenseKey docs + FLOWS L22. PR [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) OPEN · tip **in sync** · CI green (`breaking-approved`). GitHub `mergeStateStatus=BLOCKED` — owner/ruleset (not a pre-merge code gate). Tag `v2.8.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks 26` (re-verified 2026-11-13)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **44** items — ✅ **44** (100%) · 🟨 **0** (0%) · ⬜ **0** (0%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **9** |
| Files (committed three-dot) | **76** · **+2 386 / −693** |
| Working tree | clean |
| Local vs `origin/release/sms-otp-templates` | tip **in sync** (`c1f2474`) |
| Tests | PR CI `build` ✅ (local suite via CI) |
| PR | [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) OPEN · head `c1f2474` · `breaking-approved` |
| CodeRabbit | tip checks pass / skip |
| Version-plan open C/H/M/L | **empty** · HW `C1` `H34` `M121` `L23` · L22 closed |

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
| CG4 | Commit / push tip + docs | ✅ tip `c1f2474` in sync with origin |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip | ✅ in sync with origin |
| P2 | Open PR → `master` | ✅ [#26](https://github.com/denis-peshkov/Cross.Identity/pull/26) |
| P3 | CI green on PR tip | ✅ build · breaking-gate · Sonar · CodeQL · Owner-only |
| P4 | Ready to merge → `master` | ✅ go (CI + tip); merge UI BLOCKED = owner/ruleset — tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) | ✅ finalized `(closed)` |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.8.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.7.0 to 2.8.0 | ✅ |
| D4 | `docs/SMS-OTP-TEMPLATES.md` / README | ✅ |
| D5 | [`FLOWS.md`](../Cross.Identity/FLOWS.md) `sendCode` `-sms` | ✅ |
| D6 | [`TO-DO.md`](TO-DO.md) | ✅ HW `C1` `H34` `M121` `L23` · L22 closed |
| D7 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `SendCode` SMS unit coverage | ✅ |
| T2 | Suite evidence | ✅ via PR CI `build` |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR #26 (current tip) | ✅ |
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
| R2 | FLOWS.md `sendCode` `-sms` | ✅ #L22 | ✅ |
| R3 | Tip / docs on PR | Synced `c1f2474` | ✅ |
| R4 | Merge UI BLOCKED | Owner merge when ready (not a code gate) | ✅ accepted |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — plan `(closed)`; L22 closed
- ✅ **2. Commit/push** tip in sync with origin
- ✅ **3. PR #26** + CI green on tip
- ✅ **4. Docs content** — BREAKING / CHANGELOG / SMS-OTP / FLOWS `-sms`
- ✅ **5. Ready to merge** — code/CI go; tag `v2.8.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized `(closed)`
- ✅ Feature set committed + pushed
- ✅ Docs / FLOWS `-sms` on tip
- ✅ PR #26 / CI green on tip
- ✅ PostDeployment N/A
- ✅ Ready to merge (GitHub merge UI may show BLOCKED — owner/ruleset)
- ✅ Tag/NuGet out of scope until `master`
