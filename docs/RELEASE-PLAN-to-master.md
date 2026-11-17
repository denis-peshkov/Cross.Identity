# Release readiness plan `2.12.0` → `master`

> **Analysis date:** 2026-11-17
> **Branch:** `release/apple-form-callback-url` (`fa1a021`)
> **Comparison base:** `origin/master` (`v2.11.0` / `7beeada`) · merge-base `7beeada`
> **Version plan:** [`docs/RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.12.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Apple Sign In requires `FormCallbackUrl` (no `CallbackUrl` fallback). PR [#30](https://github.com/denis-peshkov/Cross.Identity/pull/30). Tag `v2.12.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan / `gh pr checks` (2026-11-17, tip CI **in progress**)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **41** items — ✅ **33** (80%) · 🟨 **2** (5%) · ⬜ **6** (15%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **8** |
| Files | **20** · **+465 / −56** (+ this checklist WT) |
| Working tree | to-master update pending commit · tip `fa1a021` = `origin/release/apple-form-callback-url` |
| Remote branch | tracking · up to date (except local to-master) |
| Tests | CI build ⬜ in progress on PR #30 tip |
| PR | [#30](https://github.com/denis-peshkov/Cross.Identity/pull/30) → `master` · MERGEABLE |
| CodeRabbit | not run on this tip |
| Version-plan open C/H/M/L | **empty** · closed H41 H42 · M130–M132 · L26 L27 · HW `C1` `H42` `M132` `L27` |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library | 7 | `FormCallbackUrl` resolve; `sendCode` `userAccountIdKey`; Register JSON |
| Tests | 4 | Apple redirect_uri / empty FormCallbackUrl; SendCode bind |
| FLOWS / README / Sample | 4 | Apple note; squat; Sample `FormCallbackUrl` + appsettings |
| docs | 5 | BREAKING / CHANGELOG / PLAN 2.12.0 / PLAN 2.11.0 / TO-DO (+ this to-master) |

---

## Table of contents

1. [New functionality (2.12.0)](#1-new-functionality-2120)
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

## 1. New functionality (2.12.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Apple `FormCallbackUrl` | Required `redirect_uri` for authorize + token exchange | ✅ |
| F2 | Register `sendCode` bind | Optional `userAccountIdKey` → stock `createUser.UserAccountId` | ✅ |
| F3 | Squat contract docs | FLOWS Register + `IUserService` + TO-DO Принято | ✅ |
| F4 | Sample host URLs | SPA `CallbackUrl` + API `FormCallbackUrl` (`…/OAuthReturn`) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.11.0 to 2.12.0 | Set `FormCallbackUrl` for Apple; no fallback | ✅ |
| B2 | Apple with only `CallbackUrl` | Expect `InvalidOperationException` | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md) empty | ✅ `(closed)` |
| CG2 | Closed H41 H42 · M130–M132 · L26 L27 | ✅ |
| CG3 | PR opened | ✅ [#30](https://github.com/denis-peshkov/Cross.Identity/pull/30) |
| CG4 | Finalize docs on remote tip | ✅ (`fa1a021`) |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push finalize plan / TO-DO | ✅ |
| P2 | PR → `master` | ✅ #30 |
| P3 | CI green on PR tip | ⬜ in progress |
| P4 | Ready to merge → `master` | ⬜ (wait CI; tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md) | ✅ `(closed)` |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.12.0 | ✅ |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.11.0 to 2.12.0 | ✅ |
| D4 | [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md) Apple + Register / sendCode | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW + Принято synced |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.11.0.md`](RELEASE-PLAN-2.11.0.md) | ✅ `(published / closed)` |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `ExternalLoginServiceTests` Apple `FormCallbackUrl` / empty fails | ✅ |
| T2 | `SendCode_StepTests` / factory `userAccountIdKey` | ✅ |
| T3 | PR CI build | ⬜ in progress (#30) |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR tip | ⬜ in progress |
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
| R1 | Hosts using Apple with only `CallbackUrl` | BREAKING — set `FormCallbackUrl` to POST API bridge | ✅ documented |
| R2 | Tag before master | Do not tag until after merge | ✅ policy |
| R3 | PR title still says «optional» | Prefer rename to `BREAKING:` require FormCallbackUrl | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; H41–H42 / M130–M132 / L26–L27 closed
- ✅ **2. Commit/push** finalize docs on tip (`fa1a021`)
- 🟨 **3. PR → `master`** #30 · CI in progress
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.11.0→2.12.0 / FLOWS / README
- ⬜ **5. Ready to merge** — wait CI green; tag `v2.12.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set on branch (`FormCallbackUrl` required + Register sendCode bind)
- ✅ Finalize docs on remote tip
- ⬜ PR / CI green (#30)
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
