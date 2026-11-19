Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.15.0` · **ветка:** `release/communication-endpoint-add` · **база:** `origin/master` (`v2.14.0`) · **дата:** `2026-11-19`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.15.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.14.0.md`](RELEASE-PLAN-2.14.0.md)
>
> Дельта: `origin/master...HEAD` — **1** коммит · **23** файла · **+1219 / −22**. Open C/H/M/L пустые.

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

- Service split: **`AddAsync`** = unverified Manual upsert (no OTP); **`SetVerifiedAsync`** = mark verified (no OTP check — `CommunicationEndpointSetVerified` step verifies first).
- `CommunicationEndpointAdd` OTP goes to the **submitted** email/phone (not `ResolveOtpTargetAsync` preferred/account contact).
- Only Email / SMS via OTP; messenger channels rejected (`ValidationException`).
- `SetVerified` may fill empty primary `UsersAccounts.Email` / `PhoneNumber` and set `*Verified` when the address matches; preferred is not forced.
- `FlowOperationEnum` **19** / **20** (`CommunicationEndpointAdd` / `CommunicationEndpointSetVerified`); ordinal **18** remains `ExternalLoginMerge`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H57 CommunicationEndpoint add OTP flows | `AddAsync` / `SetVerifiedAsync` + Add / SetVerified steps, DI, enum 19/20 |
| ✅ #M143 Add-endpoint tests | Flow / step / factory / service coverage |
| ✅ #L35 FLOWS / README / rest-client / CHANGELOG | Docs + samples for Add / SetVerified |
| ✅ #L36 Add/SetVerified tests → `TestCategory.INTEGRATION` | Method-level on five tests in `CommunicationEndpointServiceTests` |

---

## Что в библиотеке уже нормально

- Host still authorizes `UserAccountId` (same as other CommunicationEndpoints* flows).
- Conflict if address already verified on another account/endpoint (same messages as Register squat guards).
- Resend path: unverified Manual row can be upserted again; already-verified on same account → ValidationException.

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md) (вкл. leftover M141 rest-client mergeToken wording).
