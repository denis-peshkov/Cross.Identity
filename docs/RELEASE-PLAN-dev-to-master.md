# Release readiness plan `2.4.0` → `master`

> **Analysis date:** 2026-09-13  
> **Branch:** `release/fix-missed-issues` (`ce36561`)  
> **Comparison base:** `origin/master` (`v2.3.0` / `e3f7d34`) · merge-base `6a04f50` (`v2.2.0`)  
> **Version plan:** [`docs/RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) · backlog [`TO-DO.md`](TO-DO.md)  
> **Goal:** verification checklist before merge of **2.4.0** into `master`  
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker  
> **Note:** historical `dev`→`master` delta is **empty** (trees equal after v2.3.0 back-merges). This checklist tracks the **current** ship path.  
> **Sources:** `git fetch` / `git diff origin/master...HEAD` / `dotnet test` / version plan (verified 2026-09-13)  
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-summary.mjs --write`

**Checklist summary:** **54** items — ✅ **21** (39%) · 🟨 **7** (13%) · ⬜ **17** (31%) · ❌ **9** (17%)

---

## Change summary

| Metric | Value |
|---------|----------|
| Commits (`origin/master..HEAD`) | 54 |
| Files (three-dot `origin/master...HEAD`) | 170 · +5 474 / −1 656 |
| Files (tree vs master tip, two-dot) | 146 · +4 264 / −1 099 |
| Lib + Tests vs master tip | 83 · +1 957 / −230 |
| Local vs `origin/release/fix-missed-issues` | ahead **23** |
| Tests (`dotnet test`) | 516 total · **514** passed · **2** failed (`NotificationOptions` defaults) |
| PR | — |
| CodeRabbit | не запускался |

| Area | Files (≈) | Role in release |
|---------|--------|---------------|
| Cross.Identity | 76 | ChangeAccountEmail, composer/notifier, LanguageCode, templates, license |
| Cross.Identity.Tests | 27 | Flow / step / placeholder / license coverage |
| .cursor | 38 | release-plan / triage / coderabbit skills |
| .github | 10 | CI Sonar key, ISSUE/PR templates, rulesets |
| docs | 9 | PLAN 2.4.0, CHANGELOG, BREAKING, TO-DO |

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
| F4 | NotificationOptions | `Authentication:Notifications` brand placeholders (M73) | 🟨 property initializers missing — tests fail |
| F5 | Templates | `password-changed` + verify/register/confirm-email cleanup (M71, M75) | 🟨 WIP uncommitted register/verify (M76) |
| F6 | License | dotted product-type claim parse (M77) | ✅ |
| F7 | Package icons | `icon.png` / `icon.svg` (M78) | ✅ |
| F8 | EndpointId | GUID regex on SetPreferred (M41) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|--------|--------|--------|
| B1 | Lifecycle bags already on master (`Jti` / `UserAccountId`) | Consumers on `v2.3.0` — no new bag rename in 2.4 | ✅ |
| B2 | `{{support}}` → `{{supportEmail}}` in stock templates | Custom host templates must use new placeholder | 🟨 documented in CHANGELOG / Accepted; no `BREAKING` § 2.3→2.4 |
| B3 | `Authentication:Notifications` | Additive config; defaults intended = former hardcoded Peshkov values | 🟨 Accepted in plan; class initializers absent (see T2) |
| B4 | ChangeAccountEmail boundary | `IUserService` owns account email; endpoint upsert = delivery sync | ✅ Принято |

---

## 3. Code gate

| # | Check | Status |
|---|----------|--------|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) — only gate leftovers H18 / M76 | 🟨 |
| CG2 | Closed M66–M75 · M41 · L13 · M77–M78 · L14 in branch | ✅ |
| CG3 | Back-merge / rebase `origin/master` (`e3f7d34`) into branch (**H18**) | ⬜ |
| CG4 | Commit or drop WIP `register.*` / `verify.*` templates (**M76**) | ⬜ |
| CG5 | `dotnet test` green after master merge | ❌ 2 failing now; re-run after CG3–CG4 |

---

## 4. Practical ship steps

| # | Step | Status |
|---|------|--------|
| P1 | Push local ahead commits to `origin/release/fix-missed-issues` | ⬜ |
| P2 | Open PR → `master` | ⬜ |
| P3 | CI + Sonar QG on PR | ⬜ |
| P4 | CodeRabbit on branch (optional) | ⬜ |
| P5 | Tag `v2.4.0` + NuGet after merge | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|----------|--------|
| D1 | [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) | ✅ refreshed 2026-09-13 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.4.0 | ✅ dated `13 Sep 2026` |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.3.0 to 2.4.0 | ⬜ none (additive); confirm B2/B3 |
| D4 | `FLOWS.md` / README host-authorize for ChangeAccountEmail | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) cross-version backlog (H1, M13–14, …) | ✅ not a 2.4 merge blocker |

---

## 6. Automated tests

| # | Check | Status |
|---|----------|--------|
| T1 | Flow / step coverage for ChangeAccountEmail, SendCode, ResetPassword, placeholders | ✅ |
| T2 | `NotificationOptionsTests.Defaults_MatchLegacyHardcodedBrand` | ❌ defaults null — fix initializers or test |
| T3 | Full suite green after CG3 (master merge) | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|----------|--------|
| C1 | Sonar project key + name in `dotnet.yml` (L13) | ✅ |
| C2 | GitHub ISSUE/PR templates + rulesets (L14) | ✅ |
| C3 | `config.nuspec` / icons aligned | ✅ |
| C4 | CI green on PR to `master` | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|----------|--------|
| M1 | New SQL / EF migrations required for 2.4.0 library delta | ✅ none expected (app-level / templates / flows) |
| M2 | Existing `Infrastructure/Scripts` still valid vs master | ✅ no delta blocker |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|----------|----------------|--------|
| R1 | **H18** — `HEAD` missing `origin/master` tip (`e3f7d34`); merge-base `v2.2.0` | Back-merge/rebase before PR | ❌ |
| R2 | **M76** — uncommitted register/verify templates | Commit or revert before ship | ❌ |
| R3 | **T2** — NotificationOptions defaults vs tests | Restore property initializers (architecture E′) | ❌ |
| R4 | Dual OTP verify (architecture A) | Outside 2.4 gate → TO-DO / architecture canvas | ✅ deferred |
| R5 | History: branch re-landed Jti work from v2.2.0 base | Resolve via CG3; watch merge conflicts | 🟨 |

---

## 10. Recommended work order (release gate)

Execute in order; proceed after closing the previous step (or an explicit skip in the PR).

- ❌ **1. Blockers** — H18 back-merge master · M76 templates · T2 NotificationOptions defaults
- ⬜ **2. Push** — local +23 → `origin/release/fix-missed-issues`
- ⬜ **3. PR** → `master` with CHANGELOG § v2.4.0
- ⬜ **4. CI / Sonar** on PR
- 🟨 **5. Docs** — BREAKING 2.3→2.4 only if B2/B3 treated as consumer-breaking
- ⬜ **6. CodeRabbit** (optional)
- ⬜ **7. Merge + tag `v2.4.0` + NuGet**

### Minimum go/no-go checklist

- ❌ `origin/master` tip reachable from release branch (H18)
- ❌ WIP templates resolved (M76)
- ❌ `dotnet test` all green (T2 / CG5)
- ✅ Closed feature set M66–M75 / M77–M78 in version plan
- ⬜ PR + CI green
- ⬜ Tag / NuGet after merge
