Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

**Версия:** `2.2.0` (planned)  
**Ветка:** `release/remove-refresh-session-proof-from-user-scoped-flows` · **база:** `origin/master` · **дата:** `2026-08-25`  
**Релиз (если есть):** —

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker  
**Критично / Высокий / Средний / Низкий** — только ⬜ open **этой дельты**. **Закрыто** — все ✅; столбец `#` = `✅ #34 Session IP binding config` (номера сохраняются).  
Backlog вне дельты → [`TO-DO.md`](TO-DO.md) (инкрементально; удалять при закрытии в версии).

**Источники дельты:** `collect-release-delta.sh` · `.cursor/skills/release-plan/.cache/delta-2.2.0-release-remove-refresh-session-proof-from-user-scoped-flows.md`

**Предыдущие планы:** [`RELEASE-PLAN-2.0.0.md`](RELEASE-PLAN-2.0.0.md) · [`RELEASE-PLAN-2.1.1.md`](RELEASE-PLAN-2.1.1.md)

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

### H6. `collect-release-delta.sh`: SIGPIPE / full consume
⬜ CodeRabbit: при лимите ~400 строк diff полностью consume stream `git diff` (не рвать reader → SIGPIPE under `pipefail`) для FLOWS/BREAKING секций.

### H7. Scripts README: ExternalLoginStates create + journal atomic
⬜ CodeRabbit: SQL Server PreDeployment example — одна transaction: create table + `__MigrationsHistory` insert; rollback при ошибке; idempotent checks сохранить.

---

## Средний (противоречия / баги контрактов)

### M45. `FLOWS.md` anchors → User-scoped authorization
⬜ CodeRabbit: ссылки у `UserAccountId` / affected ops — на `#user-scoped-authorization-host-responsibility`, не `#client-context-host`.

### M46. triage skill: `gh auth` via wrapper
⬜ CodeRabbit: в `.cursor/skills/triage/SKILL.md` — `gh auth status` через `.cursor/triage/gh-wrapper.sh`, не прямой `gh`.

---

## Низкий (техдолг / несогласованности)

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
| ✅ #H3 release-plan targeted reads before state change | skill: required read current plan/TO-DO before merge/close/finalize; no routine scan without state change |
| ✅ #H4 journal only if this script created table | dismissed: `ExternalLoginStates` не появляется иначе, чем из этого PreDeployment-потока; `OBJECT_ID` в примере ок |
| ✅ #H5 run-coderabbit-review.sh PIPESTATUS / tee | RC учитывает failure `coderabbit` и `tee` |

---

## Что в библиотеке уже нормально

- Token lifecycle flows (`Token` / `RefreshToken` / `Logout` / `LogoutAll`) по-прежнему принимают / выдают refresh.
- User-scoped operations доверяют host-authorized `UserAccountId` (без library RefreshToken session proof).

---

## Приоритет фиксов

1. **BREAKING.md § From 2.1.1 to 2.2.0** — consumer migration (CR major; код/FLOWS в дельте есть).
2. **H7:** atomic PreDeployment example (create + journal).
3. **H6:** collect-delta SIGPIPE / full consume.
4. **M45–M46:** FLOWS anchors / triage `gh-wrapper`.

_(общий backlog → [`TO-DO.md`](TO-DO.md); H1 DbUp heuristic уже в TO-DO — CR duplicate skipped)_
