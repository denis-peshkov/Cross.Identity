Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.4.0` · **ветка:** `release/fix-missed-issues` · **база:** `origin/master` (`v2.3.0`) · **дата:** `2026-09-06`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.3.0.md`](RELEASE-PLAN-2.3.0.md)

**Дельта:** рабочее дерево после `v2.3.0` / `origin/master` (committed tree ветки ≡ master; новый product work — WIP).  
**База bump:** `latest_published=2.3.0` → target `2.4.0` (`resolve-target-version.sh`).

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off / контракт хоста)

### `ChangeAccountEmail` — account op + endpoint sync
Смена primary email — зона `IUserService` (не `ICommunicationEndpointService`). Upsert email-endpoint остаётся side-effect для синхрона delivery/OTP; flow может отдавать `endpoint` DTO хосту без второго `GetAll`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|------|
| ✅ #M66 `main.ChangeAccountEmail` flow | stock JSON / `ChangeAccountEmailStep` / factory / flow-тесты; host-authorized `UserAccountId` |
| ✅ #M67 `IUserService.ChangeAccountEmailAsync` | account `Email`/`EmailVerified` + upsert endpoint; auto-verify via linked `ProviderEmail` |
| ✅ #M68 Audit `AccountEmailChanged` | `AuditOperation.AccountEmailChanged`; `EntityType=UserAccount`, `EntityId=userAccountId` |
| ✅ #M69 OAuth EmailVerified docs | XML: new registration copies provider attestation; re-login/link не трогает `UsersAccounts.EmailVerified` |
| ✅ #M70 README / FLOWS / Sample.http | `ChangeAccountEmail` в host-authorize list; FLOWS §; rest-client sample |
| ✅ #M71 `password-changed` templates | `ResetPasswordStep` notify: txt/html из `Definitions/Templates` (как `SendCodeStep`); JSON `template`/`subject` |

---

## Что в библиотеке уже нормально

- Lifecycle bags: `Logout` / `RefreshToken` — `Jti`; `LogoutAll` / `ChangePassword` — `UserAccountId`; host resolves identity before `ExecuteAsync`.
- Смена account email через user service + sync communication endpoint.

---

## Приоритет фиксов

_(пусто — открытых пунктов дельты нет; внерелизный backlog → [`TO-DO.md`](TO-DO.md).)_
