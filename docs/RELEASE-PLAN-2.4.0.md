Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.4.0` · **ветка:** `release/fix-missed-issues` · **база:** `origin/master` (`v2.3.0`) · **дата:** `2026-09-13`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.3.0.md`](RELEASE-PLAN-2.3.0.md)
>
> Дельта: `origin/master...HEAD` — **53** коммита · **170** файлов · **+5 454 / −1 656**. Open: H1 M1.

**CodeRabbit:** не запускался.

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

### H18. Branch missing `origin/master` tip (`v2.3.0`)
`HEAD` (`3ef55fe`) **не** содержит `e3f7d34`; merge-base с `origin/master` = `6a04f50` (`v2.2.0`). Перед PR — back-merge / rebase `origin/master`, затем `dotnet test`.

---

## Средний (противоречия / баги контрактов)

### M76. Uncommitted register/verify templates
В working tree: правки `register.*` / `verify.*` (вкл. новые `register.ru.html`, `verify.ru.html`) **не** в `HEAD`. До ship — commit или revert, иначе M75/CHANGELOG drift.

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

### `ChangeAccountEmail` — account op + endpoint sync
Смена primary email — зона `IUserService` (не `ICommunicationEndpointService`). Upsert email-endpoint остаётся side-effect для синхрона delivery/OTP; flow может отдавать `endpoint` DTO хосту без второго `GetAll`.

### Optional `LanguageCode` + `Authentication:Notifications`
Хост кладёт `collectForm.LanguageCode` (2 буквы) до `ExecuteAsync`; library не читает `Accept-Language` / `HttpContext`. Brand placeholders (`{{brand}}`, `{{site}}`, …) из `Authentication:Notifications` с defaults = прежние hardcoded Peshkov values — additive config, не breaking. Stock templates: `{{supportEmail}}` (не `{{support}}`).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M66 `main.ChangeAccountEmail` flow | stock JSON / `ChangeAccountEmailStep` / factory / flow-тесты; host-authorized `UserAccountId` |
| ✅ #M67 `IUserService.ChangeAccountEmailAsync` | account `Email`/`EmailVerified` + upsert endpoint; auto-verify via linked `ProviderEmail` |
| ✅ #M68 Audit `AccountEmailChanged` | `AuditOperation.AccountEmailChanged`; `EntityType=UserAccount`, `EntityId=userAccountId` |
| ✅ #M69 OAuth EmailVerified docs | XML: new registration copies provider attestation; re-login/link не трогает `UsersAccounts.EmailVerified` |
| ✅ #M70 README / FLOWS / Sample.http | `ChangeAccountEmail` в host-authorize list; FLOWS §; rest-client sample |
| ✅ #M71 `password-changed` templates | `ResetPasswordStep` notify: txt/html из `Definitions/Templates`; JSON `template`/`subject` |
| ✅ #M72 `HostSuppliedLanguageContext` | `collectForm.LanguageCode` (2 letters, opt.); `SendCode`/`ResetPassword` template lang + fallback `en` |
| ✅ #M73 `Authentication:Notifications` | brand/site/company/fullName/supportEmail для template placeholders |
| ✅ #M74 `NotificationComposer` + `ISecurityNotifier` | общая загрузка/brand/placeholders; OTP → CodeService; FYI notify → SecurityNotifier |
| ✅ #M75 Templates placeholders cleanup | `verify`/`register`/`confirm-email` rewritten (en/ru/ro); unified placeholders; stock flows: `verify` / `reset` / `password-changed` |
| ✅ #M41 `EndpointId` GUID regex | `main.CommunicationEndpointSetPreferred.json` — `min/max: 36` + Guid regex (verified on branch / master) |
| ✅ #L13 Sonar project key in CI | `dotnet.yml` / SonarCloud project key + name aligned |
| ✅ #M77 License dotted product claim | `License` parse dotted JWT product names ↔ underscore enum; tests |
| ✅ #M78 Package icons `icon.png`/`icon.svg` | rename from IdentityServer.*; `config.nuspec` / `.slnx` |
| ✅ #L14 GitHub templates / rulesets | ISSUE/PR templates + rulesets README под Cross.Identity |

---

## Что в библиотеке уже нормально

- Lifecycle bags: `Logout` / `RefreshToken` — `Jti`; `LogoutAll` / `ChangePassword` — `UserAccountId`; host resolves identity before `ExecuteAsync`.
- Смена account email через user service + sync communication endpoint.
- Notification templates: optional `LanguageCode`, shared composer, `password-changed` / verify / register / confirm-email.

---

## Приоритет фиксов

1. **H18** — back-merge `origin/master` (`v2.3.0`) в ветку, прогнать тесты.
2. **M76** — закоммитить или откатить WIP `register`/`verify` templates.
3. PR → `master` · CI/Sonar · (опц.) CodeRabbit · tag `v2.4.0`.

Внерелизный backlog → [`TO-DO.md`](TO-DO.md).
