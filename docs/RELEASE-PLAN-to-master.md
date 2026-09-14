# Release readiness plan `2.4.1` → `master`

> **Analysis date:** 2026-09-14
> **Branch:** `hotfix/register-password-optional` (`8146ebb` = tip `master` / `v2.4.0`)
> **Comparison base:** `origin/master` (`v2.4.0` / `8146ebb`) · merge-base `8146ebb`
> **Version plan:** [`docs/RELEASE-PLAN-2.4.1.md`](RELEASE-PLAN-2.4.1.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.4.1** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** committed delta vs `master` is **empty**; ship content is in the **working tree**. Previous release [`v2.4.0`](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.0) published.
> **Sources:** `git` / version plan / WT (verified 2026-09-14)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **46** items — ✅ **27** (59%) · 🟨 **0** (0%) · ⬜ **19** (41%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | 0 (branch tip = `master`) |
| Files (committed three-dot) | 0 |
| Working tree | **20** files · **+406 / −240** |
| Local vs `origin/hotfix/…` | **not pushed** |
| Tests | Registration / `UserService` passwordless register coverage in WT |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** |

| Area | Files (≈) | Role in release |
|---|---|---|
| Cross.Identity | 3 | Register `Password` optional + `CreateUserAsync` skip blank |
| Cross.Identity.Tests | 2 | Registration flow / UserService |
| docs | 6 | PLAN 2.4.1, CHANGELOG, TO-DO, checklist rename, PLAN 2.4.0/2.3.0 refs |
| README / FLOWS / CONTRIBUTING | 3 | optional Password + maintainer-only release notes |
| .cursor / .github | 6 | `401-markdown`, skill, `release-plan-to-master.mjs`, PR template |

---

## Table of contents

1. [New functionality (2.4.1)](#1-new-functionality-241)
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

## 1. New functionality (2.4.1)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Register Password optional | `main.Register.json` `required: false`; blank → `PasswordPhc` null (M91) | ✅ |
| F2 | FLOWS Register bag | `userAccountIdKey` → `UserAccountId` (M40) | ✅ |
| F3 | FLOWS sync | 18 flows; ChangeAccountEmail host-authorize; Notifications host-only (M90) | ✅ |
| F4 | Markdown tables rule | `.cursor/rules/401-markdown.mdc` (L16) | ✅ |
| F5 | to-master rename | checklist + `release-plan-to-master.mjs`; contributor docs cleaned (M92) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Optional Register `Password` | Additive logic change; clients that always send Password unchanged | ✅ |
| B2 | `docs/BREAKING.md` § From 2.4.0 to 2.4.1 | None expected | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.4.1.md`](RELEASE-PLAN-2.4.1.md) empty | ✅ |
| CG2 | Closed M91 · M40 · M90 · M92 · L16 (dismiss M43 / L4 / L6) in plan | ✅ |
| CG3 | WT contains Register + `CreateUserAsync` + tests | ✅ |
| CG4 | Commit WT onto `hotfix/register-password-optional` | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit working tree (20 files) | ⬜ |
| P2 | Push `hotfix/register-password-optional` | ⬜ |
| P3 | Open PR → `master` | ⬜ |
| P4 | CI + Sonar QG on PR | ⬜ |
| P5 | Tag `v2.4.1` + NuGet after merge | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.4.1.md`](RELEASE-PLAN-2.4.1.md) | ✅ refreshed 2026-09-14 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.4.1 | ✅ dated `14 Sep 2026` |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.4.0 to 2.4.1 | ✅ none |
| D4 | `FLOWS.md` / README Register optional Password | ✅ in WT |
| D5 | [`TO-DO.md`](TO-DO.md) cross-version backlog | ✅ not a 2.4.1 merge blocker |
| D6 | [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) published / closed | ✅ |
| D7 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) renamed from `dev-to-master` | ✅ |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Registration flow covers register without password | ✅ in WT |
| T2 | `UserService` blank password → null `PasswordPhc` | ✅ in WT |
| T3 | Full suite green on PR CI | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR → `master` | ⬜ |
| C2 | Tag `v2.4.1` + NuGet publish | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | New SQL / EF migrations for 2.4.1 library delta | ✅ none expected |
| M2 | Existing `Infrastructure/Scripts` still valid vs master | ✅ no delta blocker |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Uncommitted WT (ship content not on branch tip) | Commit before PR | ⬜ |
| R2 | Passwordless register — host may OTP / set-password after | Documented in FLOWS/README; not a gate blocker | ✅ |
| R3 | Clients that always send Password | No break | ✅ |

---

## 10. Recommended work order (release gate)

Execute in order; proceed after closing the previous step (or an explicit skip in the PR).

- ✅ **1. Code gate content** — M91 / M40 / M90 / M92 / L16 in WT; open C/H/M/L empty
- ⬜ **2. Commit** — stage + commit WT on hotfix branch
- ⬜ **3. Push** — `hotfix/register-password-optional`
- ⬜ **4. PR** → `master`
- ⬜ **5. CI / Sonar** on PR
- ✅ **6. Docs** — CHANGELOG § v2.4.1; no BREAKING 2.4.0→2.4.1
- ⬜ **7. Merge + tag `v2.4.1` + NuGet**

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set in WT (Register optional Password + FLOWS + to-master rename)
- ⬜ WT committed
- ⬜ PR opened
- ⬜ PR CI green
- ⬜ Tag / NuGet after merge
