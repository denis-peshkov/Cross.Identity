# Release readiness plan `2.18.0` → `master`

> **Analysis date:** 2026-11-25
> **Branch:** `release/oauth-email-always-verified` (WT on `324b279` = `origin/master`)
> **Comparison base:** `origin/master` (`v2.17.0` / `324b279`) · merge-base `324b279`
> **Version plan:** [`docs/RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.18.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Any non-empty OAuth provider email → `EmailVerified = true` (account + profile); auto-link only to existing **verified** same-email account, else create new verified account (no unverified re-attach). **BREAKING** From 2.17→2.18. Tag `v2.18.0` / NuGet — **after** merge to `master` (CI).
> **Sources:** `git` / version plan · PR — none yet
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **50** items — ✅ **28** (56%) · 🟨 **3** (6%) · ⬜ **19** (38%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **0** |
| Files | **9** · **+251 / −75** (WT vs `origin/master`; includes this checklist when saved) |
| Working tree | All ship delta uncommitted on tip `324b279` (= `master`) |
| Remote branch | local only / no tracking PR tip yet |
| Tests | In WT delta; local `dotnet` often AdvSimd/LSE abort → leave CI |
| PR | — (open `BREAKING:` PR → `master` when tip is pushed) |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** · closed H63 · M152 · M154 · L45 · L47–L48 |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 3 | Always-verified provider email; CompleteAsync force; verified same-email match only |
| Tests | 1 | Facebook/Microsoft/Google verified account; squat→new account; FB unlink negative + secondary fork |
| FLOWS | 1 | OAuth callback / Facebook notes |
| docs | 4+ | BREAKING / CHANGELOG / PLAN 2.18.0 / PLAN 2.17 published (+ this to-master) |

---

## Table of contents

1. [New functionality (2.18.0)](#1-new-functionality-2180)
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

## 1. New functionality (2.18.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Provider email always verified | Non-empty email → `ExternalOAuthProfile.EmailVerified = true` (all providers + CompleteAsync) | ✅ |
| F2 | Account email verified | New OAuth registration sets `UsersAccounts.EmailVerified = true` | ✅ |
| F3 | Same-email match | Auto-link only verified same-email; else new verified account | ✅ |
| F4 | Tests | Verified account asserts; squat→new account; sole FB unlink blocked; secondary FB unlink → new account | ✅ |
| F5 | Host docs | FLOWS + BREAKING + CHANGELOG | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Consumer-breaking NuGet surface | Provider email always verified; unverified same-email not reused | ✅ documented |
| B2 | [`docs/BREAKING.md`](BREAKING.md) | From 2.17.x→2.18.0 present | ✅ |
| B3 | PR title | Need `BREAKING:` prefix when PR opens | ⬜ no PR yet |
| B4 | `breaking-approved` label | Required for breaking-gate | ⬜ after PR |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) | ✅ empty |
| CG2 | Closed H63 · M152 · M154 · L45 · L47 · L48 | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Docs on remote tip | ⬜ entire delta still local WT |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push feature tip | ⬜ |
| P2 | PR → `master` | ⬜ |
| P3 | CI green on PR tip | ⬜ |
| P4 | Ready to merge → `master` | ⬜ after commit/push/PR/CI; tag/NuGet post-merge |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.18.0.md`](RELEASE-PLAN-2.18.0.md) | 🟨 draft (not finalized) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.18.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) From 2.17→2.18 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | 🟨 HW not bumped until finalize |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.17.0.md`](RELEASE-PLAN-2.17.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Facebook / Microsoft / Google → account `EmailVerified` | ✅ in delta |
| T2 | Unverified squat → new verified account | ✅ in delta |
| T3 | Facebook UnlinkAsync sole blocked; secondary verified removed → re-login forks | ✅ in delta |
| T4 | Suite evidence | ⬜ local AdvSimd/LSE; rely on CI after push |
| T5 | PR CI build | ⬜ no PR |

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
| R1 | Entire ship still local WT | Commit + push + open `BREAKING:` PR → `master` | ⬜ |
| R2 | Tag before master | Do not tag until after merge | ✅ policy |
| R3 | Trusting Facebook/Graph email as verified | Accepted (Принято); hosts update UI copy | ✅ accepted |
| R4 | Unverified same-email not auto-linked | Accepted; verified match or create only | ✅ accepted |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty
- ⬜ **2. Feature tip** — commit + push branch
- ⬜ **3. PR** — open `BREAKING:` PR → `master` + `breaking-approved`
- ⬜ **4. CI** — green on tip
- 🟨 **5. Docs** — finalize version plan when ready; sync TO-DO Принято/HW
- ⬜ **6. Ready to merge** — merge → `master`; tag `v2.18.0` / NuGet only after merge (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (always-verified provider email; verified same-email match only)
- ✅ BREAKING From 2.17→2.18 documented
- ⬜ Tip pushed + PR opened
- ⬜ Breaking-gate / label
- ⬜ Merge to `master`
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
