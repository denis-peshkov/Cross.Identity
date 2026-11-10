# Release readiness plan `2.6.0` → `master`

> **Analysis date:** 2026-11-10
> **Branch:** `release/facebook-oauth` (`9967433`)
> **Comparison base:** `origin/master` (`v2.5.6` / `24447a5`) · merge-base `24447a5`
> **Version plan:** [`docs/RELEASE-PLAN-2.6.0.md`](RELEASE-PLAN-2.6.0.md) (**closed**) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.6.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Facebook + Apple OAuth + seeds + Sample callback bridge + breaking-gate / Cursor tooling (mixed slice). PR [#24](https://github.com/denis-peshkov/Cross.Identity/pull/24) OPEN. Tag `v2.6.0` absent.
> **Sources:** `git` / version plan / `gh pr checks 24` (verified 2026-11-10)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **48** items — ✅ **34** (71%) · 🟨 **1** (2%) · ⬜ **13** (27%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **18** |
| Files (committed three-dot) | **75** · **+2 134 / −669** |
| Working tree | **4** dirty docs (`CHANGELOG` · plan · to-master · `TO-DO`) |
| Local vs `origin/release/facebook-oauth` | tip **in sync**; WT uncommitted |
| Tests | PR CI build ✅ · local 🟨 (1 flaky watcher) |
| PR | [#24](https://github.com/denis-peshkov/Cross.Identity/pull/24) OPEN |
| CodeRabbit | `2026-11-08` · 1 Minor → plan empty |
| Version-plan open C/H/M/L | **empty** (plan finalized / closed) |

| Area | Files (≈) | Role in release |
|---|---|---|
| External OAuth | ~15 | Facebook + Apple providers, token `id_token`, callback `User` |
| Tests | ~3 | ExternalLoginService / step / factory |
| Infrastructure Scripts | ~6 | Facebook seed `4_01` + `5_01` ×3 |
| Sample.Api | ~3 | Providers stubs + `/oauth/callback` bridge |
| docs | ~5 | PLAN 2.6.0, CHANGELOG, BREAKING 2.5.6→2.6.0, FLOWS, TO-DO |
| CI / .cursor | ~40 | breaking-gate, triage, rules, pr-message skill |

---

## Table of contents

1. [New functionality (2.6.0)](#1-new-functionality-260)
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

## 1. New functionality (2.6.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Facebook OAuth | Graph v21; `EmailVerified=false`; `appsecret_proof` (H28 H31) | ✅ |
| F2 | Apple Sign In | `form_post` + `id_token` profile; optional `user` → DisplayName | ✅ |
| F3 | Callback wiring | `User` / `userKey`; Sample `/oauth/callback` bridge (H30 M102) | ✅ |
| F4 | Facebook seed | `ProviderId=5` greenfield + PostDeployment fail-closed / PG reconcile (M105 M108) | ✅ |
| F5 | Email required | empty provider email → `ValidationException` (BREAKING) | ✅ |
| F6 | CI / Cursor tooling | breaking-gate fail-closed; triage/rules/pr-message (H29 M101) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.5.6 to 2.6.0 | External OAuth email required | ✅ |
| B2 | Operators existing DBs | Run `5_01_auth_Providers_Facebook` × engines | ⬜ |
| B3 | Mixed release slice | OAuth + CI/deps in one PR — accepted | ✅ Принято |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.6.0.md`](RELEASE-PLAN-2.6.0.md) empty | ✅ |
| CG2 | Closed H28–H31 · M99–M108 (+ feature rows); plan **closed** | ✅ |
| CG3 | PR #24 opened | ✅ |
| CG4 | Commit remaining docs refresh (4 files) | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Push branch tip | ✅ in sync with origin |
| P2 | Open PR → `master` | ✅ [#24](https://github.com/denis-peshkov/Cross.Identity/pull/24) |
| P3 | CI green on PR tip | ✅ build · Sonar · CodeQL · Owner-only (re-check after docs commit) |
| P4 | Operators: Facebook PostDeployment seeds | ⬜ |
| P5 | Merge + tag `v2.6.0` + NuGet | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.6.0.md`](RELEASE-PLAN-2.6.0.md) | ✅ finalized `(closed)` 2026-11-10 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.6.0 | ✅ dated `10 Nov 2026` (local WT) |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.5.6 to 2.6.0 | ✅ |
| D4 | `FLOWS.md` / README / Sample callback | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) · HW `H31`/`M108`; M98 Apple JWKS | ✅ not a merge blocker |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file (local WT) |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | ExternalLogin Facebook/Apple unit coverage | ✅ |
| T2 | Full suite locally | 🟨 528 passed · 1 flaky (`FileSystemProviderWatcherTests`) |
| T3 | PR CI build | ✅ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR #24 (current tip) | ✅ |
| C2 | Tag `v2.6.0` + NuGet publish | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | Facebook seed scripts present ×3 engines | ✅ |
| M2 | Existing DBs need PostDeployment | ⬜ operator |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Apple JWKS not re-validated | Deferred → TO-DO M98 | ✅ deferred |
| R2 | ProviderId=5 occupied | Accepted silent skip (M107) | ✅ |
| R3 | Facebook scheme collision | Fail-closed in `5_01` (M105) | ✅ |
| R4 | Mixed OAuth + tooling PR | Accepted slice | ✅ |
| R5 | Uncommitted finalize docs | Commit/push CHANGELOG · plan · to-master · TO-DO | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; plan finalized `(closed)`
- ⬜ **2. Commit/push** remaining docs WT (4 files)
- ✅ **3. PR #24** + CI green on tip
- ⬜ **4. Operator seeds** `5_01_auth_Providers_Facebook`
- ✅ **5. Docs content** — CHANGELOG / BREAKING 2.5.6→2.6.0
- ⬜ **6. Merge + tag `v2.6.0` + NuGet**

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty / finalized
- ✅ Feature set committed (Facebook + Apple + seeds)
- ⬜ Finalize docs committed + pushed
- ✅ PR #24 / CI green on tip
- ⬜ PostDeployment on existing DBs
- ⬜ Tag / NuGet
