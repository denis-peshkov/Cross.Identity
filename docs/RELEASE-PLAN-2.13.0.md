Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.13.0` · **ветка:** `feature/account-merge-external-login` · **база:** `origin/master` (`v2.12.0`) · **дата:** `2026-11-18`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.13.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.12.0.md`](RELEASE-PLAN-2.12.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · WT **43** файлов · **+1196 / −37**. Open: H6 M2 L0 (H44–H48, H50, M136–M138).

**CodeRabbit:**
- `2026-11-18` · log `.cursor/skills/coderabbit/.cache/cr-feature-account-merge-external-login-vs-origin-master-all-20261118-221101.jsonl` · 14 findings (0 Critical, 13 Major, 1 Minor) → Open: H44–H48, H50, M136–M138; closed #H49 #M135; skipped 1 (ProviderId fixtures — no columns).

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / licensing / auth model)

### H44. `FlowOperationEnum.ExternalLoginMerge` shifts ordinals
⬜ Inserted before `Logout` / `LogoutAll` / … — changes implicit int values of existing members vs `v2.12.0`. Move `ExternalLoginMerge` to the **end** of the enum.

### H45. Merge token not bound to conflicted provider link
⬜ Pending row stores only survivor/absorbed ids. Confirm does not re-check that the OAuth `(ProviderId, ProviderUserId)` that minted the token still belongs to absorbed. CR: persist + revalidate link; reject if removed/reassigned (needs schema columns + mint/confirm + tests).

### H46. Merge + token revoke not one atomic commit
⬜ `RevokeAllTokensForUserAsync` calls `SaveChanges` mid-merge (same `IdentityContext`) — persists login/endpoint/pending mutations before audit/`UserAccountsMerged` final save. Use one transaction / defer revoke save so failed merge leaves token retryable.

### H47. MySQL / PostgreSQL `1_12` missing `2_03` journal (+ MySQL index)
⬜ SqlServer `1_12` journals `2_03_auth_AccountMergePendings.sql` after create; MySQL/PG do not. MySQL also `CREATE UNIQUE INDEX` without existence guard (rerun risk). Align with SqlServer + project MySQL index pattern.

### H48. Concurrent merge confirm race
⬜ `ConcurrencyStamp` is a token but confirm does not rotate it before/with `ConsumedAt`; two confirms can both pass the early `ConsumedAt == null` read. Rotate stamp (or equivalent write-time guard) so stale confirm fails before effects persist.

### H50. Duplicate endpoint collapse drops Verified / Preferred
⬜ When absorbed endpoint matches survivor `(Channel, Address)`, row is removed without transferring `IsVerified` / preferred onto survivor. Preserve verification (and preferred if survivor has none).

---

## Средний (противоречия / баги контрактов)

### M136. Empty `mergeToken` → `ArgumentException`
⬜ `MergeAccountsAsync` uses `ArgumentException.ThrowIfNullOrWhiteSpace`; unknown token uses `ValidationException`. Align empty/whitespace to `ValidationException`.

### M137. Endpoint duplicate keys vs DB collation
⬜ In-memory `HashSet<(Channel, Address)>` may not match unique-index collation (case/accent). Prefer DB comparison rules when detecting duplicates before move.

### M138. `alreadyLinked` `ErrorCode` not in ProblemDetails
⬜ `ToValidationProblemDetails` maps `PropertyName` → `ErrorMessage` only; FluentValidation `ErrorCode` (`alreadyLinked`) is dropped. `mergeToken` still appears as a property. Preserve code in host-facing payload if required.

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

- Merge переносит external logins + communication endpoints; absorbed `IsActive=false` + revoke `ACCOUNT_MERGED`; email/password/profile absorbed → survivor **не** копируются (email/phone merge — later).
- TTL `AccountMergePendings` = `ExternalLoginOptions.StateLifetime`; host обязан авторизовать `UserAccountId` survivor до `ExternalLoginMerge`.
- `UserAccountEntity.IsActive` без C#-default `true` — явная установка на create; тесты/фикстуры правят сами (documented in BREAKING).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H43 ExternalLogin account merge | `ThrowIfLinkConflictNeedsMergeAsync` + `MergeAccountsAsync` + flow/step/DI + audit |
| ✅ #M133 Merge tests | Service + flow + step factory coverage |
| ✅ #M134 DbUp `AccountMergePendings` | `1_12` PreDeployment + `2_03` Initial ×3 providers; EF config |
| ✅ #L28 CHANGELOG / BREAKING 2.13.0 | CHANGELOG section + BREAKING DbUp + mergeToken contract |
| ✅ #H49 `IsActive` default in BREAKING | From 2.12→2.13: no C# `= true`; hosts/fixtures set explicitly |
| ✅ #M135 FLOWS / README `ExternalLoginMerge` | Flow section + link-conflict note; README + Scripts README + Sample.Api.http |

---

## Что в библиотеке уже нормально

- Non-link sign-in path unchanged (existing external → same user; no merge mint).
- Duplicate provider on survivor during merge → drop absorbed row (no second link).
- Preferred endpoint: at most one preferred on survivor after merge (when moving non-duplicate rows).
- `ToValidationProblemDetails` already exposes `mergeToken` / ids as error properties (message values).

---

## Приоритет фиксов

1. **H44** — enum ordinals (breaking for int consumers).
2. **H46** / **H48** — atomic merge + concurrency.
3. **H47** — MySQL/PG journal (+ index guard).
4. **H45** / **H50** — provider-link bind + endpoint verify transfer.
5. **M136**–**M138** — ValidationException / collation / ErrorCode.
6. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
