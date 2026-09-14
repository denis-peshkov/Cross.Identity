Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.4.0` · **ветка:** `release/fix-missed-issues` · **база:** `origin/master` (`v2.3.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.4.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.3.0.md`](RELEASE-PLAN-2.3.0.md)
>
> Дельта: `origin/master...HEAD` — **73** коммита · **170** файлов · **+7 486 / −2 198**. Open C/H/M/L: **0C / 8H / 7M / 0L**.

**CodeRabbit:** `2026-09-14` · logs `.cursor/skills/coderabbit/.cache/cr-release-fix-missed-issues-vs-origin-master-*-20260914-*.jsonl` (dirs: Cross.Identity, Tests, Sample.Api, docs, .github, .cursor, rest-client, Infrastructure) · **35** findings (0 Critical, 26 Major, 9 Minor) → **8H + 7M открыты в плане** (#H20–#H27, #M82–#M88); #H19 принято (OTP в preheader).

**PR:** [#21](https://github.com/denis-peshkov/Cross.Identity/pull/21) (`ChangeAccountEmail, notification composer/templates, LanguageCode`).

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

### H20. Quadruple-brace placeholders vs `{{…}}` Replace
⬜ Stock HTML uses `{{{{name}}}}`; `NotificationComposer` replaces `{{name}}` → leftover `{{value}}` (demo: `{{{{code}}}}` + `123456` → `{{123456}}`). Normalize to `{{…}}` across verify/reset/register/confirm-email/password-changed. `Contain("{{code}}")` tests false-pass on quads.

### H21. HTML-encode dynamic values in `NotificationComposer`
⬜ `Apply` inserts brand + caller placeholders into HtmlBody without HtmlEncode; keep TextBody raw.

### H22. Sample.Api host templates quality
⬜ register `*.txt`: missing `{{url}}`/`{{helpLink}}`; ru/ro txt still English; `verify.ro.html` hardcodes «3 hours» instead of `{{expires}}`.

### H23. New 2.4.0 tests: `Given/When/Then` (+ `Async`)
⬜ Rename NotificationOptions / Composer / EmbeddedTemplate / HostSuppliedLanguage / SecurityNotifier / ChangeAccountEmail tests per `.cursor/rules/300-testing-dotnet.mdc`.

### H24. TO-DO «Принято»: ChangePassword still says `Id`
⬜ Stale line — contract is `UserAccountId` (+ current password), not historical `Id` (2.3).

### H25. B2 `{{support}}` → `{{supportEmail}}` as breaking
⬜ `RELEASE-PLAN-dev-to-master` B2/D3: document host custom-template migration; add/confirm `BREAKING.md` § From 2.3.0 to 2.4.0 (no `supportEmail` § yet).

### H26. `release-plan-summary` SUMMARY_RE case
⬜ Match documented `Checklist Summary` vs `Checklist summary` so `--write` replaces instead of duplicating.

### H27. `update-changelog.mjs` honor `--version` / `--from`
⬜ When both flags set, return them directly (bypass tag-requiring resolver).

---

## Средний (противоречия / баги контрактов)

### M82. CHANGELOG v2.4.0 dated before tag
⬜ Mark Unreleased until GitHub release/tag exists (heading currently dated 14 Sep 2026).

### M83. `RELEASE-PLAN-dev-to-master` maintenance script path
⬜ Documented path should match real script (`.cursor/skills/release-plan/scripts/release-plan-summary.mjs --write`).

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

### OTP in HTML email preheader (CR #H19)
Осознанно: код в hidden preview — UX (сниппет inbox / notification). Риск утечки OTP в list view принят; кастомный host template может убрать. После **#H20** braces preheader должен реально подставлять `{{code}}`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
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

1. **H20** — `{{{{…}}}}` → `{{…}}` (stock templates + placeholder tests; preheader тоже заработает).
2. **H21** — HtmlEncode in `NotificationComposer` HTML path.
3. **H22** — Sample.Api template localization / placeholders / `{{expires}}`.
4. **H23** — Given/When/Then rename for new 2.4.0 tests.
5. **H24** / **H25** / **H26** / **H27** — docs/tooling contract fixes.
6. **M82–M88** — CHANGELOG Unreleased, triage/CI/scripts polish.

Ship: CI/Sonar на [PR #21](https://github.com/denis-peshkov/Cross.Identity/pull/21) · merge → tag `v2.4.0` + NuGet.

Внерелизный backlog → [`TO-DO.md`](TO-DO.md).
