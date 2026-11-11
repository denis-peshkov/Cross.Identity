Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.6.0` (published / closed) · **ветка:** `release/facebook-oauth` · **база:** `origin/master` (`v2.5.6`) · **дата:** `2026-11-10`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.6.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md)
>
> Дельта: `origin/master...HEAD` — **18** коммитов · **75** файлов · **+2 134 / −669**. Open C/H/M/L пустые.

**CodeRabbit:**
- `2026-11-08` · log `.cursor/skills/coderabbit/.cache/cr-release-facebook-oauth-vs-origin-master-all-20261108-225534.jsonl` · 1 finding (0 Critical, 0 Major, 1 Minor) → Open C/H/M/L пустые.

**PR:** [#24](https://github.com/denis-peshkov/Cross.Identity/pull/24) (`Add Facebook and Apple external OAuth providers`).

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

### Mixed release slice
Branch also carries CI/workflow and Cross package bumps alongside Facebook / Apple OAuth — review/ship as one release or split PRs consciously.

### Facebook `ProviderId=5` occupied (`#M107`)
If id `5` is already used by a non-Facebook row, `5_01` may skip insert without error. Accepted: do not fail or reassign ids; hosts with custom provider ids handle seed manually.

### Apple DisplayName column length (`#M106`)
Oversized Apple form_post names are not truncated or specially ignored in app code — dismissed. Rely on normal provider payloads / SQL constraints if needed.

### Setext headings out of scope (`release-plan-to-master`)
Plans / to-master use ATX (`##`) + HR with a blank line before `---`, never setext (`Title` + `---`). `applySummaryLine` need not preserve setext; CR **#M100** dismissed.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M108 Facebook Name CI reconcile (PostgreSQL) | `5_01` PG only: LOWER(Name)=facebook → one row, remap logins, canonical Name/Scheme; SS/MySQL unchanged |
| ✅ #M107 Facebook ProviderId=5 occupied skip | dismissed: leave silent skip when id 5 taken without Facebook; no alt-id / fail |
| ✅ #M106 Apple DisplayName max length | dismissed: no truncate/cap; leave DB length to provider/SQL |
| ✅ #M105 Facebook Providers scheme collision | `5_01` ×3 fail if `Scheme=facebook` under other Name; MySQL drop INSERT IGNORE; idempotent for Facebook |
| ✅ #H31 Facebook `appsecret_proof` on `/me` | HMAC-SHA256(access_token, ClientSecret); FetchProfile gets secret; secret not in URL; test |
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

_(пусто — релиз `2.6.0` закрыт; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
