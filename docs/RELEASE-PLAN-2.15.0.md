Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.15.0` (published / closed) · **ветка:** `release/communication-endpoint-add` · **база:** `origin/master` (`v2.14.0`) · **дата:** `2026-11-19`
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
- `SetVerified` may fill empty primary `UsersAccounts.Email` / `PhoneNumber` and set `*Verified` when the address matches; preferred only via upsert bootstrap (no existing preferred), otherwise unchanged.
- `FlowOperationEnum` **19** / **20** (`CommunicationEndpointAdd` / `CommunicationEndpointSetVerified`); ordinal **18** remains `ExternalLoginMerge`.
- OTP-reject log keeps masked Address (`MaskEmailForLog` / `MaskPhoneForLog`) instead of dropping the field entirely.
- Verified endpoint uniqueness: app pre-check (`EnsureAddressAvailableForAddAsync`) + existing `UsersAccounts` verified Email/Phone unique indexes; no extra filtered unique on `UsersCommunicationEndpoints` / DbUpdate→Conflict mapping for SetVerified races.
- Atomicity of `SetVerifiedAsync` (Upsert + primary + audit): host/ambient transactions outside the library; no extra `_context` transaction wrapper inside.
- Public `ICommunicationEndpointService` adds `AddAsync` / `SetVerifiedAsync` without default impl; only library `internal` implementer. Hosts using DI registration are unaffected; external interface implementers out of scope for to-master B1/B2.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H57 CommunicationEndpoint add OTP flows | `AddAsync` / `SetVerifiedAsync` + Add / SetVerified steps, DI, enum 19/20 |
| ✅ #M143 Add-endpoint tests | Flow / step / factory / service coverage |
| ✅ #L35 FLOWS / README / rest-client / CHANGELOG | Docs + samples for Add / SetVerified |
| ✅ #L36 Add/SetVerified tests → `TestCategory.INTEGRATION` | Method-level on five tests in `CommunicationEndpointServiceTests` |
| ✅ #M144 OTP-reject log masks email/phone | `MaskEmailForLog` / `MaskPhoneForLog` in `CommunicationEndpointSetVerifiedStep` (keep Address, no raw PII) |
| ✅ #H58 Remove `ProblemDetailsFactoryExtensions` | Host maps ProblemDetails / ErrorHandlers; BREAKING 2.14→2.15 |
| ✅ #L37 OTP-reject keep masked Address | Dismiss CR drop-Address; masking enough (Принято / #M144) |
| ✅ #H59 No filtered unique on verified endpoints | Dismiss CR; pre-check + UsersAccounts unique enough |
| ✅ #M145 No SetVerified inner transaction | Dismiss CR; transactions outside the library |
| ✅ #L38 SetVerified preferred bootstrap docs + test | XML bootstrap vs leave existing; assert IsPreferred in empty-primary test |
| ✅ #L39 to-master B1/B2 leave as-is | Dismiss CR; DI hosts OK; external interface implementers out of scope |
| ✅ #L40 Sample.Api.http SetVerified preferred comment | Bootstrap vs keep existing; SetPreferred for explicit change |

---

## Что в библиотеке уже нормально

- Host still authorizes `UserAccountId` (same as other CommunicationEndpoints* flows).
- Conflict if address already verified on another account/endpoint (same messages as Register squat guards).
- Resend path: unverified Manual row can be upserted again; already-verified on same account → ValidationException.

---

## Приоритет фиксов

1. Кросс-версионный backlog — [`TO-DO.md`](TO-DO.md) (вкл. leftover M141 rest-client mergeToken wording).
