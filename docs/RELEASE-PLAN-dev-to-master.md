# Release readiness plan `2.4.0` → `master`

> **Analysis date:** 2026-09-14  
> **Branch:** `release/fix-missed-issues` (`ec24409`)  
> **Comparison base:** `origin/master` (`v2.3.0` / `e3f7d34`) · merge-base `e3f7d34`  
> **Version plan:** [`docs/RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) · backlog [`TO-DO.md`](TO-DO.md)  
> **Goal:** verification checklist before merge of **2.4.0** into `master`  
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker  
> **Note:** historical `dev`→`master` delta is **empty** (trees equal after v2.3.0 back-merges). This checklist tracks the **current** ship path.  
> **Sources:** `git` / `dotnet test` / version plan / [PR #21](https://github.com/denis-peshkov/Cross.Identity/pull/21) (verified 2026-09-14)  
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-summary.mjs --write`

**Checklist summary:** **56** items — ✅ **46** (82%) · 🟨 **0** (0%) · ⬜ **10** (18%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---------|----------|
| Commits (`origin/master..HEAD`) | 73 |
| Files (three-dot `origin/master...HEAD`) | 170 · +7 486 / −2 198 |
| Local vs `origin/release/fix-missed-issues` | **in sync** |
| Tests | `NotificationOptionsTests` ✅ (no built-in defaults); full suite — on CI |
| PR | [#21](https://github.com/denis-peshkov/Cross.Identity/pull/21) → `master` (OPEN) |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** |

| Area | Files (≈) | Role in release |
|---------|--------|---------------|
| Cross.Identity | ~85 | ChangeAccountEmail, composer/notifier, LanguageCode, templates, license |
| .cursor | ~38 | release-plan / triage / coderabbit skills |
| Cross.Identity.Tests | ~27 | Flow / step / placeholder / license coverage |
| Sample.Api | ~16 | Notifications config + host samples |
| .github | ~10 | CI Sonar key, template exclude, ISSUE/PR templates |
| docs | ~9 | PLAN 2.4.0, CHANGELOG, BREAKING, TO-DO |

---

## Table of contents

1. [New functionality (2.4.0)](#1-new-functionality-240)
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

## 1. New functionality (2.4.0)

| # | Block | Key changes | Status |
|---|------|-------------------|--------|
| F1 | ChangeAccountEmail | `main.ChangeAccountEmail` + `IUserService.ChangeAccountEmailAsync` + audit `AccountEmailChanged` (M66–M70) | ✅ |
| F2 | Notifications stack | `INotificationComposer` + `ISecurityNotifier`; OTP vs FYI split (M74) | ✅ |
| F3 | LanguageCode | `HostSuppliedLanguageContext`; optional `collectForm.LanguageCode` (M72) | ✅ |
| F4 | NotificationOptions | host `Authentication:Notifications`; no library defaults (M73, M79) | ✅ |
| F5 | Templates | `password-changed` + verify/register/confirm-email (M71, M75, M76, M81) | ✅ |
| F6 | License | dotted product-type claim parse (M77) | ✅ |
| F7 | Package icons | `icon.png` / `icon.svg` (M78) | ✅ |
| F8 | EndpointId | GUID regex on SetPreferred (M41) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|--------|--------|--------|
| B1 | Lifecycle bags already on master (`Jti` / `UserAccountId`) | Consumers on `v2.3.0` — no new bag rename in 2.4 | ✅ |
| B2 | `{{support}}` → `{{supportEmail}}` in stock templates | Typo/alias cleanup; config always `SupportEmail` | ✅ принято: не breaking (H25); no BREAKING § |
| B3 | `Authentication:Notifications` | Additive host config; **no** built-in library defaults | ✅ Принято (M79) |
| B4 | ChangeAccountEmail boundary | `IUserService` owns account email; endpoint upsert = delivery sync | ✅ Принято |

---

## 3. Code gate

| # | Check | Status |
|---|----------|--------|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) empty | ✅ |
| CG2 | Closed M66–M81 · M41 · L13–L15 · H18 in branch | ✅ |
| CG3 | Back-merge `origin/master` (`e3f7d34`) into branch (**H18**) | ✅ |
| CG4 | register/verify templates committed (**M76**) | ✅ |
| CG5 | `NotificationOptions` contract + unit test (**M79**) | ✅ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|------|--------|
| P1 | Push to `origin/release/fix-missed-issues` | ✅ in sync |
| P2 | Open PR → `master` | ✅ [#21](https://github.com/denis-peshkov/Cross.Identity/pull/21) |
| P3 | CI + Sonar QG on PR | ⬜ |
| P4 | CodeRabbit on branch (optional) | ⬜ |
| P5 | Tag `v2.4.0` + NuGet after merge | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|----------|--------|
| D1 | [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) | ✅ refreshed 2026-09-14 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.4.0 | ✅ dated `14 Sep 2026` |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.3.0 to 2.4.0 | ✅ none — B2 not consumer-breaking (H25 Accepted) |
| D4 | `FLOWS.md` / README host-authorize for ChangeAccountEmail | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) cross-version backlog | ✅ not a 2.4 merge blocker |

---

## 6. Automated tests

| # | Check | Status |
|---|----------|--------|
| T1 | Flow / step coverage for ChangeAccountEmail, SendCode, ResetPassword, placeholders | ✅ |
| T2 | `NotificationOptionsTests` — no built-in brand defaults (**M79**) | ✅ |
| T3 | Full suite green on PR CI after master merge | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|----------|--------|
| C1 | Sonar project key + name in `dotnet.yml` (L13) | ✅ |
| C2 | GitHub ISSUE/PR templates + rulesets (L14) | ✅ |
| C3 | `config.nuspec` / icons aligned | ✅ |
| C4 | Sonar exclude email templates (L15) | ✅ |
| C5 | CI green on PR #21 | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|----------|--------|
| M1 | New SQL / EF migrations required for 2.4.0 library delta | ✅ none expected |
| M2 | Existing `Infrastructure/Scripts` still valid vs master | ✅ no delta blocker |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|----------|----------------|--------|
| R1 | **H18** — missing master tip | Merged; merge-base `e3f7d34` | ✅ |
| R2 | **M76** — uncommitted templates | Committed | ✅ |
| R3 | **M79** — NotificationOptions defaults | Accepted: host config only | ✅ |
| R4 | Dual OTP verify (architecture A) | Outside 2.4 gate → TO-DO | ✅ deferred |
| R5 | History divergence pre-merge | Resolved via master merge | ✅ |

---

## 10. Recommended work order (release gate)

Execute in order; proceed after closing the previous step (or an explicit skip in the PR).

- ✅ **1. Blockers** — H18 / M76 / M79 closed
- ✅ **2. Push** — branch in sync with origin
- ✅ **3. PR** → `master` [#21](https://github.com/denis-peshkov/Cross.Identity/pull/21)
- ⬜ **4. CI / Sonar** on PR
- ✅ **5. Docs** — no BREAKING 2.3→2.4 (B2/H25 Accepted: not breaking)
- ⬜ **6. CodeRabbit** (optional)
- ⬜ **7. Merge + tag `v2.4.0` + NuGet**

### Minimum go/no-go checklist

- ✅ `origin/master` tip reachable from release branch (H18)
- ✅ WIP templates resolved (M76)
- ✅ `NotificationOptions` contract green (M79)
- ✅ Closed feature set in version plan (open C/H/M/L empty)
- ✅ PR opened (#21)
- ⬜ PR CI green
- ⬜ Tag / NuGet after merge
