# Release readiness plan `2.9.0` → `master`

> **Analysis date:** 2026-11-14
> **Branch:** `release/libphonenumber-csharp` (WT on `3bfdb61` / `v2.8.0`)
> **Comparison base:** `origin/master` (`v2.8.0` / `3bfdb61`) · merge-base `3bfdb61`
> **Version plan:** [`docs/RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md) · backlog [`TO-DO.md`](TO-DO.md)
> **Goal:** verification checklist before merge of **2.9.0** into `master`
> **Legend:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker
> **Note:** Replace `PhoneNumbersCore` with `libphonenumber-csharp` 9.0.43. Tip = master; **all work uncommitted**. No PR. Tag `v2.9.0` / NuGet — **after** merge to `master` (CI); out of scope for this pre-merge plan.
> **Sources:** `git` / version plan (drafted 2026-11-14)
> **Maintenance:** `node .cursor/skills/release-plan/scripts/release-plan-to-master.mjs --write`

**Change summary:** **39** items — ✅ **23** (59%) · 🟨 **1** (3%) · ⬜ **15** (38%) · ❌ **0** (0%)

---

## Change summary

| Metric | Value |
|---|---|
| Commits (`origin/master..HEAD`) | **0** (tip = `v2.8.0`) |
| Files (WT vs tip) | **10** · **+47 / −15** (incl. triage undici noise) |
| Working tree | dirty — csproj/nuspec/tests/docs + triage |
| Remote branch | **absent** / not tracking yet |
| Tests | local ⬜ (use CI after push) |
| PR | — |
| CodeRabbit | не запускался |
| Version-plan open C/H/M/L | **empty** · closed H35–H36 · M122–M124 · HW tip `C1` `H34` `M121` `L23` |

| Area | Files (≈) | Role in release |
|---|---|---|
| Library deps | 2 | csproj + nuspec → libphonenumber-csharp 9.0.43 |
| Tests | 1 | RO mobile assertion |
| Sample / README | 2 | drop unused ref; dep list |
| docs | 3 | PLAN 2.9.0, BREAKING, CHANGELOG |
| noise | 2 | `.cursor/triage` undici bump |

---

## Table of contents

1. [New functionality (2.9.0)](#1-new-functionality-290)
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

## 1. New functionality (2.9.0)

| # | Block | Key changes | Status |
|---|---|---|---|
| F1 | Phone metadata package | `PhoneNumbersCore` → `libphonenumber-csharp` 9.0.43 | ✅ WT |
| F2 | Coverage | RO `+40799618836` in `PhoneE164Tests` | ✅ WT |

---

## 2. Breaking / consumer notes

| # | Change | Action | Status |
|---|---|---|---|
| B1 | [`docs/BREAKING.md`](BREAKING.md) § From 2.8.0 to 2.9.0 | Swap phone NuGet; same `PhoneNumbers` namespace | ✅ WT |
| B2 | Hosts with direct `PhoneNumbersCore` | Switch PackageReference | ✅ documented |

---

## 3. Code gate

| # | Check | Status |
|---|---|---|
| CG1 | Open C/H/M/L in [`RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md) empty | ✅ |
| CG2 | Closed H35–H36 · M122–M124 | ✅ |
| CG3 | PR opened | ⬜ |
| CG4 | Commit feature + docs WT | ⬜ |

---

## 4. Practical ship steps

| # | Step | Status |
|---|---|---|
| P1 | Commit + push branch tip | ⬜ |
| P2 | Open PR → `master` | ⬜ |
| P3 | CI green on PR tip | ⬜ |
| P4 | Ready to merge → `master` | ⬜ after CI (tag/NuGet post-merge) |

---

## 5. Documentation

| # | Document | Status |
|---|---|---|
| D1 | [`RELEASE-PLAN-2.9.0.md`](RELEASE-PLAN-2.9.0.md) | ✅ draft (WT) |
| D2 | [`docs/CHANGELOG.md`](CHANGELOG.md) § v2.9.0 | ✅ WT |
| D3 | [`docs/BREAKING.md`](BREAKING.md) § From 2.8.0 to 2.9.0 | ✅ WT |
| D4 | README dependency list | ✅ WT |
| D5 | [`TO-DO.md`](TO-DO.md) | ✅ no leftovers |
| D6 | [`RELEASE-PLAN-to-master.md`](RELEASE-PLAN-to-master.md) | ✅ this file |
| D7 | Previous [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md) | ✅ `(published / closed)` WT |

---

## 6. Automated tests

| # | Check | Status |
|---|---|---|
| T1 | `PhoneE164Tests` RO mobile | ✅ in WT |
| T2 | Suite evidence | ⬜ via CI after push |
| T3 | PR CI build | ⬜ |

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
| R1 | Hosts pinning `PhoneNumbersCore` | BREAKING action | ✅ |
| R2 | Triage `undici` bump in same WT | Split commit or drop from ship | 🟨 |
| R3 | Uncommitted WT | Commit before PR | ⬜ |

---

## 10. Recommended work order (release gate)

- ✅ **1. Code gate** — open C/H/M/L empty; plan drafted
- ⬜ **2. Commit/push** phone deps + docs (optionally drop triage noise)
- ⬜ **3. PR → `master`** + CI green
- ✅ **4. Docs content** — CHANGELOG / BREAKING 2.8.0→2.9.0 / README
- ⬜ **5. Ready to merge** — tag `v2.9.0` / NuGet only after merge to `master` (CI)

### Minimum go/no-go checklist

- ✅ Version-plan open C/H/M/L empty
- ✅ Feature set in WT (libphonenumber-csharp swap)
- ⬜ Feature + docs committed + pushed
- ⬜ PR / CI green
- ✅ PostDeployment N/A
- ✅ Tag/NuGet out of scope until `master`
