Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

**Версия:** `2.2.0`
**Ветка:** `release/remove-refresh-session-proof-from-user-scoped-flows` · **база:** `origin/master` · **дата:** `2026-08-25`
**Релиз (если есть):** —

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker

**Предыдущий план:** [`RELEASE-PLAN-2.1.1.md`](RELEASE-PLAN-2.1.1.md)

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

### L10. triage skill: убрать опцию `ru`
⬜ CodeRabbit: в `.cursor/skills/triage/SKILL.md` (и связанных usage) оставить English как единственный язык output; убрать documented `ru`.

---

## Принято (осознанный trade-off / контракт хоста)

### User-scoped flows: session proof на стороне хоста
Библиотека **не** требует `RefreshToken` на `CommunicationEndpointsGetAll` / `CommunicationEndpointSetPreferred`, `ExternalLogin` (link), `ExternalLoginUnlink`, `ExternalLoginGetAll`. Bag / API принимают **`UserAccountId`**; хост обязан авторизовать caller для этого id (access token / principal) **до** `ExecuteAsync`. `IJwtTokenService.EnsureRefreshTokenBelongsToUserAsync` — **optional** host helper (stock steps больше не вызывают).

**Unchanged:** `Token` / `RefreshToken` / `Logout` / `LogoutAll` по-прежнему используют refresh как payload операции.

**Edge:** без host `[Authorize]` / claim match клиент может подставить чужой `UserAccountId` — это контракт хоста (см. `FLOWS.md` — User-scoped authorization), не library session proof.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ User-scoped: нет library RefreshToken session proof | flows JSON / steps / factories — без `RefreshToken`; host authorizes `UserAccountId` |
| ✅ `ICommunicationEndpointService` refreshToken removed | `GetAllAsync` / `SetPreferredAsync` — параметр `refreshToken` убран |
| ✅ `IExternalLoginService` без refresh session proof | link / unlink / getAll — без refresh session proof params |
| ✅ `EnsureRefreshTokenBelongsToUserAsync` optional helper | XML: optional host helper; stock user-scoped paths не вызывают |
| ✅ `FLOWS.md` User-scoped authorization | секция User-scoped authorization; collectForm без обязательного RefreshToken |
| ✅ Tests user-scoped RefreshToken removal | ExternalLogin / CommunicationEndpoints / service tests под новый контракт |
| ✅ Versioned RELEASE-PLAN docs | `RELEASE-PLAN-2.0.0` / `2.1.1` / `2.2.0` (вместо единого `RELEASE-PLAN.md`) |
| ✅ DbUp / Scripts docs (repo) | skill + `Infrastructure/Scripts/README` + rule `102`; `1_00_Predeployment.sql` |
| ✅ #H2 Scripts README MERGE SystemId scope | CR dismissed: README upsert — **пример** lookup/MERGE, не open work |
| ✅ #H3 release-plan targeted reads before state change | current plan + TO-DO; also previous open + relevant «Закрыто» for harvest/dedupe; no routine full-history scan |
| ✅ #H4 journal only if this script created table | dismissed: `ExternalLoginStates` не появляется иначе, чем из этого PreDeployment-потока; `OBJECT_ID` в примере ок |
| ✅ #H5 run-coderabbit-review.sh PIPESTATUS / tee | RC учитывает failure `coderabbit` и `tee` |
| ✅ #H6 collect-release-delta.sh SIGPIPE / consume | `sed -n` / pipe consume; later: FLOWS/BREAKING diffs **без** лимита 800 (полный `git diff`) |
| ✅ #H7 Scripts README create+journal atomic txn | dismissed: каждый DbUp-скрипт уже в собственной транзакции; лишний BEGIN TRAN в примере не нужен |
| ✅ #M45 FLOWS.md User-scoped auth anchors | `### User-scoped authorization…` + links → `#user-scoped-authorization-host-responsibility` |
| ✅ #M46 triage skill gh auth via wrapper | Phase 0: `.cursor/triage/gh-wrapper.sh auth status` |
| ✅ BREAKING.md § From 2.1.1 to 2.2.0 | consumer migration: user-scoped APIs without RefreshToken session proof; host auth |
| ✅ #H8 FLOWS library auth callback for `UserAccountId` | CR dismissed: host authorizes `UserAccountId` (см. **Принято**); library session proof / callback не возвращаем |
| ✅ DbUp `Layer` = dependency stage | docs: format `<FolderNumber>_<Layer>_<Entity>` confirmed; shared `Layer` for independent scripts; bump only for deps; CR max-file+1 / gap-fill-as-sequence dismissed |
| ✅ #M47 bootstrap `1_00_Predeployment.sql` casing | canon name + PG/MySQL bootstrap files; docs/skill aligned |
| ✅ #M48 run-coderabbit-review.sh Python indent | heredoc: 2 spaces per level |
| ✅ #M49 CLI scripts require flag values | `--base`/`--dir`/`--out`/`--version`: non-option value or explicit error |
| ✅ #L11 `RELEASE-PLAN-2.0.0` BREAKING.md workflow row | закрыто: newest-first (**сверху** + TOC), не append в конец |
| ✅ #H9 SeedLookup MERGE DELETE absent-from-source | CR dismissed: lookup seed **намеренно** sync-to-VALUES (DELETE лишнее); мусор в lookup не храним |
| ✅ #M50 `AvatarUrl` XML ProfileUrl fallback | Minor: `ExternalLoginProviderItemDto` docs = `AvatarUrl ?? ProfileUrl` as in `ExternalLoginService` |
| ✅ #H10 TO-DO Id high-water (finalize merge) | CR Major: shared C/H/M/L namespace via `Id high-water` in `TO-DO.md`; allocate = HW+1; finalize bumps max(release ids); merge по id (не reallocate) |

---

## Что в библиотеке уже нормально

- Token lifecycle flows (`Token` / `RefreshToken` / `Logout` / `LogoutAll`) по-прежнему принимают / выдают refresh.
- User-scoped operations доверяют host-authorized `UserAccountId` (без library RefreshToken session proof).

---

## Приоритет фиксов

1. **L10:** triage skill — убрать documented `ru` (или won’t-fix, если bilingual нужен).
