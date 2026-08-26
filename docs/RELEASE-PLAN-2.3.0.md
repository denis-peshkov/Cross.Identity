Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.3.0` · **ветка:** `release/fix-missed-issues` · **база:** `origin/master` · **дата:** `2026-08-26`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.3.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)

**CodeRabbit:** `2026-08-26` · log `.cursor/skills/coderabbit/.cache/cr-release-fix-missed-issues-vs-origin-master-all-20260826-022142.jsonl` · 10 findings (0 Critical, 3 Major, 7 Minor) → все закрыты в этом плане.  
**PR:** [#20](https://github.com/denis-peshkov/Cross.Identity/pull/20) (`BREAKING:` Logout/RefreshToken → `Jti`, LogoutAll/ChangePassword → `UserAccountId`).

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off / контракт хоста)

### Lifecycle bags: host resolves identity before `ExecuteAsync`
`Logout` / `RefreshToken` принимают **`Jti`** (access / refresh JWT `jti` = DB id); `LogoutAll` / `ChangePassword` — **`UserAccountId`**. Библиотека **не** парсит compact refresh string на этих путях и не доказывает session через refresh payload. Хост валидирует client token / авторизует caller **до** `ExecuteAsync` (продолжение модели 2.2.0 для user-scoped; lifecycle выровнен в 2.3.0). См. [`FLOWS.md`](../Cross.Identity/FLOWS.md), [BREAKING § From 2.2.0 to 2.3.0](BREAKING.md#from-220-to-230).

### Scaffold `Release:` — PR optional
`scaffold-breaking-section.sh`: суффикс `([PR #N](…))` только при `--pr`; без флага — только `Release: [vX.Y.Z](…)`. CR «всегда PR» / auto-`gh` — **не принимаем** (осознанно). Template: omit PR placeholder until known.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ #H14 `main.Logout` → access `Jti` | stock JSON / `LogoutStep` / factory / tests; `RevokeSessionForLogoutAsync` |
| ✅ #H15 `main.RefreshToken` → refresh `Jti` | stock JSON / `RefreshTokenStep` / Guid rotation helpers / tests |
| ✅ #H16 `main.LogoutAll` → `UserAccountId` | stock JSON / step; `RevokeAllTokensForUserAsync`; drop `RevokeAllTokensForLogoutAsync` |
| ✅ #M51 `main.ChangePassword` collectForm `UserAccountId` | stock JSON / selector / tests; `FLOWS.md`; [BREAKING.md § From 2.2.0 to 2.3.0](BREAKING.md#from-220-to-230) |
| ✅ #M59 `IJwtTokenService` slim (string refresh helpers) | removed unused public string-based validate/rotate/revoke APIs; tests via `TokenTestHelpers` |
| ✅ #M60 BREAKING.md § From 2.2.0 to 2.3.0 | consumer migration + removed API table |
| ✅ #H11 triage-pr: «External — ready» только при clean CI | `triage-pr/SKILL.md`: ready — только `SUCCESS`; unstable/dirty/unknown → problematic |
| ✅ #H12 triage-pr: не игнорировать failed fetch base | Phase 1b: fetch fail → stop; `offline` — явный fallback на local base + warning |
| ✅ #H13 triage-pr: resolve remote-only branch | Phase 1b: `BRANCH_REF` (local → `origin/$BRANCH`); все log/diff через `$BRANCH_REF` |
| ✅ #M52 README: Logout/RefreshToken — Jti | `README.md` § host authorize: `Logout` / `RefreshToken` → `Jti`; `Token` → credentials/code |
| ✅ #M53 anchor `BREAKING.md` в строке #M51 | ссылка `(BREAKING.md#from-220-to-230)` вместо fragment-only |
| ✅ #M54 `TokenTestHelpers`: `CancellationToken` в `IsRefreshTokenActiveAsync` | параметр + проброс в `FirstOrDefaultAsync`; callers → `CancellationToken.None` |
| ✅ #M55 `JwtTokenServiceTests`: `Helpers` → `GlobalUsings` | убран file-scoped using; `global using Cross.Identity.Tests.Helpers` уже был |
| ✅ #M56 `release-plan-summary.mjs`: bullets только в §10 | active `##` heading; BULLET только при `/^10\b/` |
| ✅ #M57 `repository-link.sh`: `ssh://git@github.com/` | SSH SCP + `ssh://` → `https://github.com/org/repo` |
| ✅ #M58 `scaffold-breaking-section.sh`: ANCHOR при `--from` | ANCHOR всегда из effective FROM/TO (`from-{from}-to-{to}`) |
| ✅ #L12 triage orchestrator local/branch modes | `triage local` / `triage branch` vs `origin/master`; rule + README |
| ✅ #M61 coderabbit: Fix-in-same-turn keep existing `#Id` | already-open → same id in «Закрыто»; new → allocate next; no Minor→`L` |
| ✅ #M62 release-plan bump: `dev→master` example cell | третья колонка `n/a (ask first)`; ask minor/patch/major |
| ✅ #M63 release-plan Phase 3 numbering | dismissed: 2 peer steps (`BREAKING` + Language) — нумерация OK по Workflow numbering |
| ✅ #M64 scaffold-breaking: PR on Release optional | принято: суффикс PR только при `--pr`; без auto-`gh` / fail |
| ✅ #H17 triage-pr: strip leading `origin/` on BRANCH | `BRANCH#origin/` before fetch/`origin/$BRANCH`; no `origin/origin/…` |
| ✅ #C1 double UTF-8 BOM in Logout flow tests | CR Critical: `Main_Logout*_FlowTests.cs` — ровно один BOM (был двойной) |
| ✅ #M65 FLOWS.md MD055 / MD028 | Host table row closing `\|`; убрана blank line между Host/Transaction blockquotes |

---

## Что в библиотеке уже нормально

- User-scoped flows (2.2.0): host-authorized `UserAccountId` без library refresh session proof.
- Token lifecycle (2.3.0): rotation/logout по `Jti` / `UserAccountId`; host resolves claims before bag.
- CodeRabbit / follow-ups: открытых C/H/M/L нет (закрыто через #C1, H11–H17, M51–M65, L12).

---

## Приоритет фиксов

_(пусто — открытых пунктов дельты нет; внерелизный backlog → [`TO-DO.md`](TO-DO.md).)_
