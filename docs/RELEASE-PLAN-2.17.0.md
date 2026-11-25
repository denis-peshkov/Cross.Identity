Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.17.0` (published / closed) · **ветка:** `release/oauth-unlink-conditional-email` · **база:** `origin/master` (`v2.16.0`) · **дата:** `2026-11-25`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.17.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.16.0.md`](RELEASE-PLAN-2.16.0.md)
>
> Дельта: `origin/master...HEAD` — **4** коммита · **10** файлов · **+639 / −10**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

**PR:** [#36](https://github.com/denis-peshkov/Cross.Identity/pull/36) (`BREAKING:` conditionally remove OAuth email endpoint on unlink).

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

- Unlink removes non-preferred secondary OAuth email endpoint only when user has >1 email endpoint; preferred / sole → ValidationException (UI via `CanUnlink`).
- Empty `ProviderEmail` → login-only unlink (skip EntityId/address match); populated email → match current address (`EntityId`+address, else address); never bare stale `EntityId`.
- OAuth email sync clears `EntityId` on superseded address rows (one login↔endpoint association).
- Account email retarget to remaining preferred email when primary matched removed address (else clear); `EmailVerified` follows remaining endpoint `IsVerified` (incl. always-verified ExternalProvider).
- Last-login guard unchanged and folds into `CanUnlink`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H61 Unlink conditional email endpoint remove | `UnlinkAsync` match EntityId/address; remove secondary; retarget account email |
| ✅ #M148 `CanUnlink` on overview DTO | Same formula + last-login in `GetAllAsync` |
| ✅ #M149 Unlink / CanUnlink tests | Secondary remove; preferred/sole reject; overview flags; empty ProviderEmail keeps endpoint |
| ✅ #L41 CHANGELOG / BREAKING / FLOWS 2.17.0 | Consumer notes + flow docs |
| ✅ #L42 README CanUnlink / secondary unlink | External OAuth bullet mentions `CanUnlink` + secondary email drop |
| ✅ #M150 Empty ProviderEmail before EntityId | login-only when email blank; EntityId/address only if populated |
| ✅ #M151 Stale EntityId unlink match | Address-prefer match + clear EntityId on sync superseded rows |
| ✅ #H62 Retarget EmailVerified from endpoint | Dismiss CR; keep `EmailVerified = preferred.IsVerified` |
| ✅ #L43 Release-plan delta summary stale | Fixed on finalize: 4 commits · 10 files · +639/−10 |
| ✅ #L44 Release-plan PR line missing #36 | Fixed on finalize: PR [#36](https://github.com/denis-peshkov/Cross.Identity/pull/36) |

---

## Что в библиотеке уже нормально

- Host still authorizes `UserAccountId` for Unlink / GetAll.
- Token revoke on unlink (`EXTERNAL_LOGIN_REMOVED`) unchanged.

---

## Приоритет фиксов

_(пусто — релиз `2.17.0` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
