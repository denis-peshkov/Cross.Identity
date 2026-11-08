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

**CodeRabbit:** `2026-11-08` · log `.cursor/skills/coderabbit/.cache/cr-release-facebook-oauth-vs-origin-master-all-20261108-225534.jsonl` · 1 finding (0 Critical, 0 Major, 1 Minor) → все закрыты в этом плане.

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

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
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

_(пусто — открытых пунктов дельты нет; внерелизный backlog → [`TO-DO.md`](TO-DO.md), вкл. **M98** Apple JWKS.)_

Ship: PR → `master` · CI · operators `5_01_auth_Providers_Facebook` · tag `v2.6.0` + NuGet.