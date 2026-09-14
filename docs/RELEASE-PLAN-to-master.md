# Release readiness plan `2.5.1` → `master`

> **Analysis date:** 2026-09-14
> **Branch:** `hotfix/gitversion-hotfix-patch-increment` (`1662a73`)
> **Comparison base:** `origin/master` (`v2.5.0` / `9d20000`) · merge-base `9d20000`
> **Version plan:** [`docs/RELEASE-PLAN-2.5.1.md`](RELEASE-PLAN-2.5.1.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.5.1** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** **1** commit vs `master` (GitVersion 6 + CI); remaining WT = plan/CHANGELOG/to-master/skill refresh. Previous release tag `v2.5.0` (Register optional Password; docs were briefly labeled `2.4.1`).
> **Sources:** `git` / version plan / WT (verified 2026-09-14)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **47** items — ✅ **23** (49%) · 🟨 **4** (9%) · ⬜ **20** (43%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **1** (`1662a73`) |
| Files (committed three-dot) | **7** · **+268 / −163** |
| Working tree | **4** файла · **+18 / −20** (plan / CHANGELOG / to-master / skill) |
| Local vs `origin/hotfix/…` | **not pushed** (check) |
| Tests | n/a (CI / GitVersion config only) |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** |

| Area | Files (≈) | Role in release |
|---|---|---|
| GitVersion.yml | 1 | GV6: hotfix→Patch, release→Minor; ignore branch digits |
| .github/workflows | 1 | GV actions 6.8.2; stable-only tags; NuGet push scope; Sonar key |
| docs | 5 | PLAN 2.5.1; align PLAN/CHANGELOG/to-master/TO-DO with tag `v2.5.0` |
| .cursor | 1 | release-plan skill: Принято without SemVer |

---

## Table of contents

1. [New functionality (2.5.1)](#1-new-functionality-251)
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

## 1. New functionality (2.5.1)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | GitVersion 6 Patch/Minor | hotfix→Patch; release→Minor; root Patch; no branch-name SemVer (M93) | ✅ |
| F2 | CI GitVersion 6.8.2 | actions v4.7.0 / tool 6.8.2 (M94) | ✅ |
| F3 | Stable-only tags | no tag when `semVer` contains `-`; NuGet push scoped (M95) | ✅ |
| F4 | Docs = tag `v2.5.0` | rename/relabel former `2.4.1` docs (M96) | ✅ |
| F5 | Sonar projectKey | `Cross.Identity` (L18) | ✅ |
| F6 | TO-DO Принято без SemVer | skill + strip version numbers (L19) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Library / NuGet API | No consumer API change | ✅ |
| B2 | `docs/BREAKING.md` § From 2.5.0 to 2.5.1 | None expected | ✅ |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.5.1.md`](RELEASE-PLAN-2.5.1.md) empty | ✅ |
| CG2 | Closed M93 · M94 · M95 · M96 · L18 · L19 in plan | ✅ |
| CG3 | Commit contains `GitVersion.yml` + `dotnet.yml` + docs align | ✅ |
| CG4 | Commit remaining WT (plan / CHANGELOG / to-master / skill) | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit remaining WT (4 files) | ⬜ |
| P2 | Push `hotfix/gitversion-hotfix-patch-increment` | ⬜ |
| P3 | Open PR → `master` | ⬜ |
| P4 | CI green on PR | ⬜ |
| P5 | After merge: SemVer on master is **`2.5.1`** (not `2.6.0`) | ⬜ |
| P6 | Tag `v2.5.1` + NuGet | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.5.1.md`](RELEASE-PLAN-2.5.1.md) | ✅ drafted 2026-09-14 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.5.1 | ✅ dated `14 Sep 2026` |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.5.0 to 2.5.1 | ✅ none |
| D4 | [`RELEASE-PLAN-2.5.0.md`](RELEASE-PLAN-2.5.0.md) aligned with tag | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW unchanged mid-release |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Library unit/integration suite | 🟨 n/a for this delta (no library code) |
| T2 | PR CI green (build + existing tests) | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on PR → `master` | ⬜ |
| C2 | Post-merge GitVersion → `2.5.1` | ⬜ |
| C3 | Tag `v2.5.1` + NuGet publish | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | New SQL / EF for 2.5.1 | ✅ none |
| M2 | Scripts vs master | ✅ no delta |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Uncommitted WT | Commit before PR | ⬜ |
| R2 | Wrong SemVer again (Minor) | Gate on P5 / C2 before trusting tag | ⬜ |
| R3 | Sonar `projectKey` rename | Confirm SonarCloud project exists as `Cross.Identity` | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate content** — M93–M96 / L18–L19; open C/H/M/L empty
- 🟨 **2. Commit** — main delta committed; remaining WT (docs/skill)
- ⬜ **3. Push** + PR → `master`
- ⬜ **4. CI** on PR
- ⬜ **5. Merge** — verify SemVer **`2.5.1`**
- ✅ **6. Docs** — CHANGELOG § v2.5.1; no BREAKING 2.5.0→2.5.1
- ⬜ **7. Tag `v2.5.1` + NuGet**

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set committed (GitVersion 6 + CI + docs align)
- 🟨 Remaining WT (plan refresh)
- ⬜ PR opened / CI green
- ⬜ Post-merge SemVer = `2.5.1`
- ⬜ Tag / NuGet
