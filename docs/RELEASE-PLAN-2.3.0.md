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

---

## Средний (противоречия / баги контрактов)

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
| ✅ #H12 triage-pr: не игнорировать failed fetch base | Phase 1b: fetch fail → stop; `offline` — явный fallback на local base + warning |
| ✅ #H13 triage-pr: resolve remote-only branch | Phase 1b: `BRANCH_REF` (local → `origin/$BRANCH`); все log/diff через `$BRANCH_REF` |
| ✅ #M51 `main.ChangePassword` collectForm `UserAccountId` | stock JSON / selector / tests; `FLOWS.md`; [BREAKING.md § From 2.2.0 to 2.3.0](BREAKING.md#from-220-to-230) |
| ✅ #M52 README: Logout/RefreshToken — Jti | `README.md` § host authorize: `Logout` / `RefreshToken` → `Jti`; `Token` → credentials/code |
| ✅ #M53 anchor `BREAKING.md` в строке #M51 | ссылка `(BREAKING.md#from-220-to-230)` вместо fragment-only |
| ✅ #M54 `TokenTestHelpers`: `CancellationToken` в `IsRefreshTokenActiveAsync` | параметр + проброс в `FirstOrDefaultAsync`; callers → `CancellationToken.None` |
| ✅ #M55 `JwtTokenServiceTests`: `Helpers` → `GlobalUsings` | убран file-scoped using; `global using Cross.Identity.Tests.Helpers` уже был |
| ✅ #M56 `release-plan-summary.mjs`: bullets только в §10 | active `##` heading; BULLET только при `/^10\b/`; summary на плане без изменений (98) |
| ✅ #M57 `repository-link.sh`: `ssh://git@github.com/` | SSH SCP + `ssh://` → `https://github.com/org/repo`; HTTPS/fallback без изменений |

---

## Что в библиотеке уже нормально

- User-scoped flows и token lifecycle — без изменений относительно 2.2.0 (см. [`RELEASE-PLAN-2.2.0.md`](RELEASE-PLAN-2.2.0.md)).
- CodeRabbit по дельте 2.3.0: **нет** findings в `Cross.Identity/Services/JwtTokenService.cs`, flow steps, `main.*.json` — auth/JWT/rotation контракт CR не оспорил.

---

## Приоритет фиксов

1. **M58** — `scaffold-breaking-section.sh` ANCHOR при `--from` (по желанию до или после merge).
