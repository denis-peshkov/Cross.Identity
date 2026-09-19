# Release readiness plan `2.5.5` → `master`

> **Analysis date:** 2026-09-19
> **Branch:** `master` (`b394740` = tip / `v2.5.4`)
> **Comparison base:** `origin/master` (`v2.5.4` / `b394740`) · merge-base `b394740`
> **Version plan:** [`docs/RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before ship of **2.5.5**
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** committed delta vs tip empty; ship content in **WT** (NuGet OIDC + plans backfill 2.5.2–2.5.4).
> **Sources:** `git` / version plan (verified 2026-09-19)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **39** items — ✅ **20** (51%) · 🟨 **3** (8%) · ⬜ **16** (41%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | 0 (tip = `v2.5.4`) |
| Files (committed three-dot) | 0 |
| Working tree | **9** paths · **+271 / −67** (+ untracked `RELEASE-PLAN-2.5.5.md`) |
| Local vs `origin/master` | tip equal; **WT uncommitted** |
| Tests | n/a (CI + docs) |
| PR | — (work on `master`) |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** |

| Area | Files (≈) | Role in release |
|---|---|---|
| .github/workflows | 1 | NuGet OIDC login; `id-token: write` |
| docs | 6+ | PLAN 2.5.5; backfill 2.5.2–2.5.4; CHANGELOG; TO-DO; 2.5.1 published |

---

## Table of contents

1. [New functionality (2.5.5)](#1-new-functionality-255)
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

## 1. New functionality (2.5.5)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | NuGet OIDC | `NuGet/login@v1` + temp API key; drop long-lived `NUGET_API_KEY` in push (M98) | ✅ |
| F2 | Plans backfill | Finalized `2.5.2`–`2.5.4` + CHANGELOG; `2.5.1` published (M99) | ✅ |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | Library / NuGet API | No consumer API change | ✅ |
| B2 | `docs/BREAKING.md` § From 2.5.4 to 2.5.5 | None expected | ✅ |
| B3 | CI NuGet auth | Host/org must trust NuGet OIDC for `peshkov` | ✅ Принято |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md) empty | ✅ |
| CG2 | Closed M98 · M99 in plan | ✅ |
| CG3 | WT contains `dotnet.yml` OIDC + backfill docs | ✅ |
| CG4 | Commit WT | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit working tree | ⬜ |
| P2 | Push `master` (or PR path if required) | ⬜ |
| P3 | CI green (build + NuGet OIDC on master) | ⬜ |
| P4 | Tag `v2.5.5` + NuGet | ⬜ |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md) | ✅ drafted 2026-09-19 |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.5.5 | ✅ dated `19 Sep 2026` |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.5.4 to 2.5.5 | ✅ none |
| D4 | Plans `2.5.2`–`2.5.4` backfill | ✅ |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ HW unchanged mid-release (`L21`) |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | Library suite | 🟨 n/a (no library code) |
| T2 | CI build green | ⬜ |

---

## 7. CI / package

| # | Check | Status |
|---|---|---|
| C1 | CI green on ship | ⬜ |
| C2 | NuGet OIDC login succeeds | ⬜ |
| C3 | Tag `v2.5.5` + NuGet publish | ⬜ |

---

## 8. Database

| # | Check | Status |
|---|---|---|
| M1 | New SQL / EF for 2.5.5 | ✅ none |
| M2 | Scripts vs tip | ✅ no delta |

---

## 9. Blockers and risks

| # | Issue | Recommendation | Status |
|---|---|---|---|
| R1 | Uncommitted WT | Commit before relying on CI | ⬜ |
| R2 | NuGet OIDC misconfig | Verify trusted publishing / `user: peshkov` | 🟨 |
| R3 | Working directly on `master` | Prefer short-lived branch if policy requires | 🟨 |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate content** — M98 / M99 in WT; open C/H/M/L empty
- ⬜ **2. Commit** WT
- ⬜ **3. Push** / CI
- ✅ **4. Docs** — CHANGELOG § v2.5.5; no BREAKING
- ⬜ **5. Tag `v2.5.5` + NuGet** (OIDC)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set in WT (OIDC + backfill)
- ⬜ WT committed
- ⬜ CI / NuGet OIDC green
- ⬜ Tag / NuGet
