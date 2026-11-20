Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.16.0` · **ветка:** `release/2.16.0` · **база:** `origin/master` (`v2.15.0`) · **дата:** `2026-11-20`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.16.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.15.0.md`](RELEASE-PLAN-2.15.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · WT **8** файлов · **+56 / −7**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

**PR:** —

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

- Linked OAuth email → communication endpoint always verified (`ExternalProvider`); no OTP for that address.
- `ExternalOAuthProfile.EmailVerified` remains the gate for **account** `EmailVerified` and verified-email collision / merge — not for endpoint sync.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H60 OAuth email endpoint always verified | `SyncProviderEmailEndpointAsync` → `isVerified: true`; drop unused `emailVerified` arg |
| ✅ #M146 FLOWS OAuth endpoint attestation note | Registration note: endpoint always verified; account flag still from profile |
| ✅ #M147 Tests Microsoft unverified → endpoint verified | Graph / unverified-attestation complete asserts endpoint verified |
| ✅ #L40 CHANGELOG / BREAKING 2.16.0 | Consumer notes for always-verified ExternalProvider email endpoint |

---

## Что в библиотеке уже нормально

- Missing provider email still fails login (`ValidationException`).
- Re-login / link still does not flip `UsersAccounts.EmailVerified`.

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
