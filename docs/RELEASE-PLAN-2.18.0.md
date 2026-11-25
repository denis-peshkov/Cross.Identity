Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.18.0` · **ветка:** `release/oauth-email-always-verified` · **база:** `origin/master` (`v2.17.0`) · **дата:** `2026-11-25`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.18.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.17.0.md`](RELEASE-PLAN-2.17.0.md)
>
> Дельта: `origin/master...HEAD` — **3** коммита · **11** файлов · **+425 / −144**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался. → все закрыты в этом плане.

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

- Any non-empty provider email → `ExternalOAuthProfile.EmailVerified = true` (providers + CompleteAsync force); missing email still rejected.
- New OAuth registration sets `UsersAccounts.EmailVerified = true` when email is present.
- OAuth new account leaves `UserName` / `NormalizedUserName` null (not invented from provider profile).
- Same-email resolution: auto-link only to an existing **verified** row; otherwise create a new verified account (no legacy re-attach/promote of unverified rows).
- Facebook / Microsoft Graph emails without OIDC attestation are treated as verified when present (product trust of provider-returned address).
- Communication endpoint for linked OAuth email remains always `IsVerified = true` (unchanged from prior ship).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H63 Provider email always verified when present | Providers set `EmailVerified` from non-empty email; CompleteAsync forces true |
| ✅ #M152 Drop legacy same-email re-attach/promote | Unverified rows not reused; verified match or create |
| ✅ #M154 OAuth always-verified tests | Facebook/Microsoft/Google verified; squat→new account; sole FB unlink blocked; secondary verified FB unlink → fork; `EnsureProviderEmailVerified` unit coverage |
| ✅ #L45 CHANGELOG drop orphan `v2.17.1` | Folded into 2.18.0 / removed reuse bullets |
| ✅ #L47 FLOWS / ExternalOAuthProfile docs | Callback / Facebook / profile XML updated |
| ✅ #L48 BREAKING + CHANGELOG 2.18.0 | Always-verified; no unverified same-email reuse |
| ✅ #H64 OAuth CreateUserAsync UserName null | Stop inventing username from email/display/provider id; leave null |

---


## Что в библиотеке уже нормально

- Missing provider email still → `ValidationException` (no account / link).
- Endpoint sync for linked OAuth email stays always verified (`ExternalProvider`).
- Link-conflict merge / `AccountMergePendings` path unchanged.
- Real `UnlinkAsync`: preferred/sole OAuth email blocked; secondary verified email endpoint removed (`IsVerified` does not protect). Re-login after secondary unlink creates a **new** account (old row no longer holds that address).

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md).
