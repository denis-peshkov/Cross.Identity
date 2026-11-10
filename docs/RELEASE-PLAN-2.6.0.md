Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.6.0` · **ветка:** `release/facebook-oauth` · **база:** `origin/master` (`v2.5.6`) · **дата:** `2026-11-08`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.6.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md)
>
> Дельта: `origin/master...HEAD` — **4** коммита · **32** файла · **+745 / −59**. Open C/H/M/L пустые.

**CodeRabbit:**
- `2026-11-08` · log `.cursor/skills/coderabbit/.cache/cr-release-facebook-oauth-vs-origin-master-all-20261108-225534.jsonl` · 1 finding (0 Critical, 0 Major, 1 Minor) → Open C/H/M/L пустые.

**PR:** —

---

## Критично (безопасность)

---

## Высокий (логика / auth model)

---

## Средний (противоречия / баги контрактов)

---

## Низкий (техдолг / несогласованности)

---

## Принято (осознанный trade-off)

### Mixed release slice
Branch also carries CI/workflow and Cross package bumps alongside Facebook / Apple OAuth — review/ship as one release or split PRs consciously.

### Setext headings out of scope (`release-plan-to-master`)
Plans / to-master use ATX (`##`) + HR with a blank line before `---`, never setext (`Title` + `---`). `applySummaryLine` need not preserve setext; CR **#M100** dismissed.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M104 Apple re-login keeps DisplayName | upsert skips DisplayName when profile name null; relogin test |
| ✅ External OAuth email required | `CompleteAsync` → `ValidationException` if profile email empty; Apple/Facebook tests; BREAKING 2.5.6→2.6.0 |
| ✅ #M103 Apple user JSON root Object guard | `ApplyAppleUserJson`: `ValueKind == Object` before `TryGetProperty`; non-object (`[]`) ignored; test |
| ✅ #H30 Apple form_post callback bridge | Sample.Api `GET\|POST /oauth/callback` → `main.ExternalLoginCallback`; FLOWS / CallbackUrl / README; `response_mode=form_post` kept |
| ✅ #M102 Step User→CompleteAsync | `UserKey=collectForm.User`; Verify exact userJson (not It.IsAny) |
| ✅ #M101 Breaking gate full fetch | no `--depth=1` on BASE/`pull/*/head` so `BASE...HEAD` finds merge-base |
| ✅ #H29 Breaking gate fail-closed | missing BASE/HEAD or failed `git diff` → exit 1; BREAKING.md detect unchanged when diff OK |
| ✅ #M100 setext summary insert | dismissed: setext not used; ATX + blank-line HR only |
| ✅ #M99 Apple initiate exact scope | query parse; `scope` == `name email` |
| ✅ #H28 Facebook EmailVerified false | Graph email kept; `EmailVerified = false` (no OIDC attestation); success test |
| ✅ Facebook OAuth provider | `ExternalOAuthProviders` Graph v21; initiate/complete profile + avatar; tests |
| ✅ Seed Facebook provider | `4_01` greenfield `ProviderId=5`; `5_01_auth_Providers_Facebook` PostDeployment ×3 |
| ✅ Apple Sign In provider | `id_token` profile; `response_mode=form_post`; form_post `user` → `DisplayName`; tests |
| ✅ Docs / sample stub | README, CONTRIBUTING, Sample.Api Providers.Facebook + Apple |

---

## Что в библиотеке уже нормально

- Apple already seeded as `ProviderId=2` in `4_01` — no PostDeployment seed required.
- Existing Google / Microsoft / GitHub paths untouched (token response now carries optional `id_token`).
- Apple JWKS re-validation deferred → [`TO-DO.md`](TO-DO.md) **M98** (не дублировать в open дельты).

---

## Приоритет фиксов

1. Ship: PR → `master` · CI · operators `5_01_auth_Providers_Facebook` · tag `v2.6.0` + NuGet.

Внерелизный backlog → [`TO-DO.md`](TO-DO.md) (вкл. **M98** Apple JWKS).
