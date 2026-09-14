Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.4.0` · **ветка:** `release/fix-missed-issues` · **база:** `origin/master` (`v2.3.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.3.0.md`](RELEASE-PLAN-2.3.0.md)
>
> Дельта: `origin/master...HEAD` — **73** коммита · **170** файлов · **+7 486 / −2 198**. Open C/H/M/L: **0C / 0H / 5M / 0L**.

**CodeRabbit:** `2026-09-14` · logs `.cursor/skills/coderabbit/.cache/cr-release-fix-missed-issues-vs-origin-master-*-20260914-*.jsonl` (dirs: Cross.Identity, Tests, Sample.Api, docs, .github, .cursor, rest-client, Infrastructure) · **35** findings (0 Critical, 26 Major, 9 Minor) → **5M открыты в плане** (#M84–#M88); #H23/#H24/#H26/#H27/#M83 закрыты; #H25/#H19/#H21/#M82 принято; #H20/#H22 закрыты.

**PR:** [#21](https://github.com/denis-peshkov/Cross.Identity/pull/21) (`ChangeAccountEmail, notification composer/templates, LanguageCode`).

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

### M84. `triage.yml` `ready_for_review`
⬜ Add `ready_for_review` to `pull_request` types (draft → ready).

### M85. triage-issue Risk: drop CQRS-only qualifier
⬜ Risk guidance should apply to any repo, not CQRS-only.

### M86. triage deep-review shell path placeholders
⬜ Fix invalid `<…>` path examples in deep-review `git diff` commands.

### M87. `post-pr-triage` gh pagination `--slurp`
⬜ Paginated comments/files fetch: add `--slurp` and flatten page arrays.

### M88. `repository-link.sh` fail on empty origin
⬜ Nonzero exit when origin missing / not GitHub URL (no empty prefix links).

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

### `ChangeAccountEmail` — account op + endpoint sync
Смена primary email — зона `IUserService` (не `ICommunicationEndpointService`). Upsert email-endpoint остаётся side-effect для синхрона delivery/OTP; flow может отдавать `endpoint` DTO хосту без второго `GetAll`.

### Optional `LanguageCode` + `Authentication:Notifications`
Хост кладёт `collectForm.LanguageCode` (2 буквы) до `ExecuteAsync`; library не читает `Accept-Language` / `HttpContext`. Brand placeholders (`{{brand}}`, `{{site}}`, …) из `Authentication:Notifications` — **host config** (additive; класс **без** built-in defaults). Stock templates: `{{supportEmail}}` (не `{{support}}`). Sample: `Sample.Api` / `appsettings.json`.

### `{{support}}` vs `{{supportEmail}}` — не breaking (CR #H25)
Поле всегда было `SupportEmail`; `{{support}}` в части stock-шаблонов 2.3 — опечатка/алиас (рантайм подставлял оба). В 2.4 унификация на `{{supportEmail}}` + снятие dual-replace — **не** consumer-breaking API; § в `BREAKING.md` не заводим.

### OTP in HTML email preheader (CR #H19)
Осознанно: код в hidden preview — UX (сниппет inbox / notification). Риск утечки OTP в list view принят; кастомный host template может убрать. Placeholders в preheader — `{{code}}` / `{{expires}}` (после ✅ #H20).

### `NotificationComposer` без HtmlEncode (CR #H21)
Осознанно: brand/placeholders в HtmlBody сырым `Replace`; санитайз — зона хоста / доверенный `Authentication:Notifications` + свои значения. Text/HTML один `Apply`.

### CHANGELOG `v2.4.0` dated pre-tag (CR #M82)
Осознанно: секция `## v2.4.0 — 14 Sep 2026` до GitHub release/tag — ок для ship prep; Unreleased не заводим.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H22 Sample.Api host templates | register/verify txt: url+helpLink + ru/ro; verify.ro.html `{{expires}}` |
| ✅ #H21 HTML-encode in NotificationComposer | отклонено: encode не делаем; доверие к host config / placeholders |
| ✅ #H20 Quadruple-brace placeholders | stock HTML `{{{{…}}}}` → `{{…}}` (93); tests `NotContain("{{{{")` + compose smoke |
| ✅ #H19 OTP in HTML email preheader | принято: код в preheader — желаемый UX; не убираем |
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
| ✅ #H27 `update-changelog` `--version`/`--from` | both flags → return directly, skip tag resolver; unit test |
| ✅ #H26 Change summary label | canonical `Change summary`; legacy Checklist* replaced; skill + tests |
| ✅ #M82 CHANGELOG dated pre-tag | принято: `v2.4.0 — 14 Sep 2026` до tag ок; Unreleased не заводим |
| ✅ #M83 Maintenance script path | already ok: `.cursor/skills/release-plan/scripts/release-plan-summary.mjs`; CR `docs/scripts/…` неверен |
| ✅ #H24 TO-DO ChangePassword `Id` | «Принято»: stale `Id` → `UserAccountId` (+ current password) |
| ✅ #H25 B2 `{{support}}` → `{{supportEmail}}` | принято: не breaking — поле всегда `SupportEmail`; `{{support}}` = typo/alias; § BREAKING снят |
| ✅ #H23 New 2.4.0 tests Given/When/Then | rename NotificationOptions/Composer/EmbeddedTemplate/HostSuppliedLanguage/SecurityNotifier/ChangeAccountEmail; no Async on tests |
| ✅ #L14 GitHub templates / rulesets | ISSUE/PR templates + rulesets README под Cross.Identity |
| ✅ #M76 register/verify templates committed | localize/standardize en/ru/ro в ветке |
| ✅ #M80 Sample.Api `Notifications` config | `Authentication:Notifications` в `appsettings.json` |
| ✅ #M81 Template polish pass | structure/placeholders/responsive en/ru/ro (post-M75) |
| ✅ #M79 `NotificationOptions` no library defaults | host config only; `NewInstance_HasNoBuiltInBrandDefaults` |
| ✅ #H18 back-merge `origin/master` | merge-base = `e3f7d34` (`v2.3.0`); tip master in `HEAD` (`ec24409`) |
| ✅ #L15 Sonar exclude email templates | `dotnet.yml` — templates out of duplicate-code detection |

---

## Что в библиотеке уже нормально

- Lifecycle bags: `Logout` / `RefreshToken` — `Jti`; `LogoutAll` / `ChangePassword` — `UserAccountId`; host resolves identity before `ExecuteAsync`.
- Смена account email через user service + sync communication endpoint.
- Notification templates: optional `LanguageCode`, shared composer, host `Authentication:Notifications`, `password-changed` / verify / register / confirm-email.

---

## Приоритет фиксов

1. **M84–M88** — triage/CI/scripts polish.

Ship: CI/Sonar на [PR #21](https://github.com/denis-peshkov/Cross.Identity/pull/21) · merge → tag `v2.4.0` + NuGet.

Внерелизный backlog → [`TO-DO.md`](TO-DO.md).
