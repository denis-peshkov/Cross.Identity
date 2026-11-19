Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.13.0` (published / closed) · **ветка:** `release/account-merge-external-login` · **база:** `origin/master` (`v2.12.0`) · **дата:** `2026-11-19`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.13.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md)
>
> Дельта: `origin/master...HEAD` — **14** коммитов · **52** файла · **+1988 / −120**. Open C/H/M/L пустые.

**CodeRabbit:**
- `2026-11-18` · log `.cursor/skills/coderabbit/.cache/cr-feature-account-merge-external-login-vs-origin-master-all-20261118-221101.jsonl` · 14 findings (0 Critical, 13 Major, 1 Minor) → все закрыты в этом плане.

**PR:** [#31](https://github.com/denis-peshkov/Cross.Identity/pull/31) (`BREAKING:` add external-login account merge via mergeToken) · head still `feature/account-merge-external-login`.

---

## Критично (безопасность)

---

## Высокий (логика / licensing / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

- Merge переносит external logins + communication endpoints; absorbed `IsActive=false` + revoke `ACCOUNT_MERGED`; email/password/profile absorbed → survivor **не** копируются (email/phone merge — later).
- Merge pending bound to survivor/absorbed accounts only (not `ProviderId`/`ProviderUserId`); confirm merges whole B→A by design (CR H45 dismissed).
- Transaction unit = flow step; `RevokeAllTokensForUserAsync` may `SaveChanges` mid-`MergeAccountsAsync` (shared JWT service) — accepted (CR H46 dismissed).
- Duplicate endpoint on merge: drop absorbed row as-is (no Verified/Preferred transfer). Survivor is already signed-in; unverified-A vs verified-B not a realistic case (CR H50 dismissed).
- Endpoint `Address` already normalized on write (`ChannelEnum.NormalizeAddress` / email lower); in-memory HashSet vs CI index not a real mismatch (CR M137 dismissed).
- Link-conflict machine code is `errors.code = alreadyLinked` (ErrorMessage); FluentValidation `ErrorCode` not mapped to ProblemDetails — redundant, accepted (CR M138 dismissed).
- TTL `AccountMergePendings` = `ExternalLoginOptions.StateLifetime`; host обязан авторизовать `UserAccountId` survivor до `ExternalLoginMerge`.
- `UserAccountEntity.IsActive` без C#-default `true` — явная установка на create; тесты/фикстуры правят сами (documented in BREAKING).
- Link-conflict mint: no extra survivor pre-check / FK→NotFound in library — host already authorizes existing `UserAccountId` (FLOWS); missing survivor is host/bug path (CR M139 dismissed).
- Merge: `EnsureIsActive` only on survivor; absorbed may be `IsActive=false` — disabled B must not block link-conflict merge / reclaim of externals (CR H51 dismissed).
- Late Resolve/Upsert cross-account `ValidationException` without mergeToken — accepted: race window tiny; retry link hits `ThrowIf…` merge flow (CR H53 dismissed).
- PG `1_12` journal `EXISTS`+`NOT EXISTS` in one `IF` — accepted: DbUp always has `__MigrationsHistory` before PreDeployment (CR L32 dismissed).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H43 ExternalLogin account merge | `ThrowIfLinkConflictNeedsMergeAsync` + `MergeAccountsAsync` + flow/step/DI + audit |
| ✅ #M133 Merge tests | Service + flow + step factory coverage |
| ✅ #M134 DbUp `AccountMergePendings` | `1_12` PreDeployment + `2_03` Initial ×3 providers; EF config |
| ✅ #L28 CHANGELOG / BREAKING 2.13.0 | CHANGELOG section + BREAKING DbUp + mergeToken contract |
| ✅ #H44 `FlowOperationEnum` explicit ordinals | Existing 0–17 unchanged vs 2.12; `ExternalLoginMerge = 18` (additive, not BREAKING) |
| ✅ #H45 ProviderId/ProviderUserId on pending | Dismissed: merge is account-level B→A; no bind to conflicted OAuth link |
| ✅ #H46 Merge+revoke single atomic commit | Dismissed: step is the transaction unit; mid-merge SaveChanges via JWT revoke OK |
| ✅ #H47 MySQL/PG `1_12` journal + index | Journal `2_03` in `__MigrationsHistory`; MySQL guarded unique index; PG `IF NOT EXISTS` |
| ✅ #H48 Concurrent merge confirm race | Already covered: `IsConcurrencyToken` in EF config + `IdentityContext` rotates stamp on SaveChanges |
| ✅ #H50 Duplicate endpoint Verified/Preferred | Dismissed: signed-in survivor; drop absorbed duplicate without transfer |
| ✅ #M136 Empty mergeToken → ValidationException | `Merge token is required.` (same path as other merge validation → host 406) |
| ✅ #M137 Endpoint HashSet vs collation | Dismissed: addresses normalized on write (`NormalizeAddress`) before persist |
| ✅ #M138 alreadyLinked ErrorCode in ProblemDetails | Dismissed: client already gets `errors.code: ["alreadyLinked"]`; ErrorCode field redundant |
| ✅ #H49 `IsActive` default in BREAKING | From 2.12→2.13: no C# `= true`; hosts/fixtures set explicitly |
| ✅ #M135 FLOWS / README `ExternalLoginMerge` | Flow section + link-conflict note; README + Scripts README + Sample.Api.http |
| ✅ #L29 Merge test `MovedProviders` exact | `BeEquivalentTo("Microsoft")` + multi-provider test Microsoft+Apple |
| ✅ #M139 Survivor check before AccountMergePendings | Dismissed: host authorizes existing survivor `UserAccountId` (FLOWS); no library pre-check / FK→NotFound |
| ✅ #H51 EnsureIsActive on absorbed before merge | Dismissed: disabled absorbed must not block link/merge; only survivor must be active |
| ✅ #H52 SqlServer `1_12` independent guards | Table / UX index / journal `2_03` each guarded; heal missing index or journal when table exists |
| ✅ #M140 Merge validation / endpoint coverage | Extra `ExternalLoginServiceTests` for merge validation + endpoint move paths |
| ✅ #L30 GitVersion feature `BranchName` | `feature` regex named group so `{BranchName}` label substitutes (GV6) |
| ✅ #H53 Late cross-account without mergeToken | Dismissed: tiny race; retry link gets mergeToken via ThrowIf |
| ✅ #L31 `MergeAccountsAsync` XML `NotAuthorizedException` | Document disabled survivor → `NotAuthorizedException` |
| ✅ #L32 PG `1_12` nested journal IF | Dismissed: `__MigrationsHistory` always exists before PreDeployment (DbUp) |

---

## Что в библиотеке уже нормально

- Non-link sign-in path unchanged (existing external → same user; no merge mint).
- Duplicate provider on survivor during merge → drop absorbed row (no second link).
- Preferred endpoint: at most one preferred on survivor after merge (when moving non-duplicate rows).
- `ToValidationProblemDetails` already exposes `mergeToken` / ids as error properties (message values).

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
2. PR #31 head: переключить на `release/account-merge-external-login` (сейчас `feature/…`), если ship идёт с release-ветки.
