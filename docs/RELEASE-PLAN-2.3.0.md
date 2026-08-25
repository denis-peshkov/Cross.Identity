Ниже — **проблемы внутри библиотеки** (код `Cross.Identity/`), по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

**Версия:** `2.3.0`
**Ветка:** `release/fix-missed-issues` · **база:** `origin/master` · **дата:** `2026-08-26`
**Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.3.0

**Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker

**Предыдущий план:** [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)

**CodeRabbit:** `2026-08-26` · log `.cursor/skills/coderabbit/.cache/cr-release-fix-missed-issues-vs-origin-master-all-20260826-022142.jsonl` · 10 findings (0 Critical, 3 Major, 7 Minor)

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

### H12. triage-pr: не игнорировать failed fetch base
⬜ CodeRabbit Major: `.cursor/skills/triage-pr/SKILL.md` (~110–115) — при failed `git fetch origin "$BASE"` не fallback молча; fail или explicit offline mode.

### H13. triage-pr: resolve remote-only branch
⬜ CodeRabbit Major: `.cursor/skills/triage-pr/SKILL.md` (~117–127) — перед diff задать BRANCH на validated ref (в т.ч. `origin/$BRANCH`, если локальной ветки нет).

---

## Средний (противоречия / баги контрактов)

### M52. README: Logout/RefreshToken — Jti, не refresh string в payload
⬜ CodeRabbit Minor: `README.md` (~139) — убрать «`Token` / `RefreshToken` still use refresh tokens in the payload»; указать access/refresh `Jti` для `Logout`/`RefreshToken`, согласованно с `FLOWS.md` и 2.3.0.

### M53. RELEASE-PLAN-2.3.0: anchor `BREAKING.md` в строке #M51
⬜ CodeRabbit Minor: в «Закрыто» ссылка на секцию 2.3.0 — префикс `BREAKING.md#from-220-to-230`, не только `#from-220-to-230`.

### M54. `TokenTestHelpers`: `CancellationToken` в `IsRefreshTokenActiveAsync`
⬜ CodeRabbit Minor: `Cross.Identity.Tests/Helpers/TokenTestHelpers.cs` — параметр `CancellationToken` + проброс в `FirstOrDefaultAsync`; обновить callers.

### M55. `JwtTokenServiceTests`: `Helpers` → `GlobalUsings`
⬜ CodeRabbit Minor: перенести `Cross.Identity.Tests.Helpers` в `GlobalUsings.cs`, убрать file-scoped using из `JwtTokenServiceTests.cs`.

### M56. `release-plan-summary.mjs`: bullets только в section 10
⬜ CodeRabbit Minor: парсинг markdown — учитывать active section heading; BULLET matches только внутри section 10.

### M57. `repository-link.sh`: `ssh://git@github.com/` URL
⬜ CodeRabbit Minor: распознавать `ssh://git@github.com/org/repo.git` → `https://github.com/org/repo`.

### M58. `scaffold-breaking-section.sh`: ANCHOR при `--from` override
⬜ CodeRabbit Minor: после `--from` пересобрать ANCHOR из effective FROM, чтобы TOC совпадал с heading «From … to …».

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off / контракт хоста)

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ #H11 triage-pr: «External — ready» только при clean CI | `triage-pr/SKILL.md`: ready — только `SUCCESS`; unstable/dirty/unknown → problematic |
| ✅ #M51 `main.ChangePassword` collectForm `UserAccountId` | stock JSON / selector / tests; `FLOWS.md`; `BREAKING.md` § [From 2.2.0 to 2.3.0](#from-220-to-230) |

---

## Что в библиотеке уже нормально

- User-scoped flows и token lifecycle — без изменений относительно 2.2.0 (см. [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)).
- CodeRabbit по дельте 2.3.0: **нет** findings в `Cross.Identity/Services/JwtTokenService.cs`, flow steps, `main.*.json` — auth/JWT/rotation контракт CR не оспорил.

---

## Приоритет фиксов

1. **M52** — README host guidance (2.3.0 breaking, видно интеграторам).
2. **M53** — anchor в закрытой строке плана (быстрый doc fix).
3. **H12–H13** — triage-pr skill (tooling; не блокирует NuGet 2.3.0).
4. **M54–M58** — tests / release-plan scripts (по желанию до или после merge).
