Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.6.0` · **ветка:** `release/facebook-oauth` · **база:** `origin/master` (`v2.5.6`) · **дата:** `2026-11-08`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.6.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.5.md`](RELEASE-PLAN-2.5.5.md)
>
> Дельта: `origin/master...HEAD` — **2** коммита · **20** файлов · **+268 / −29**. Open C/H/M/L пустые.

**CodeRabbit:** `2026-11-08` · log `.cursor/skills/coderabbit/.cache/cr-release-facebook-oauth-vs-origin-master-all-20261108-223736.jsonl` · 1 finding (0 Critical, 1 Major, 0 Minor) → все закрыты в этом плане.

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
Branch also carries CI/workflow and Cross package bumps alongside Facebook OAuth — review/ship as one release or split PRs consciously.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H28 Facebook EmailVerified false | Graph email kept; `EmailVerified = false` (no OIDC attestation); success test |
| ✅ Facebook OAuth provider | `ExternalOAuthProviders` Graph v21; initiate/complete profile + avatar; tests |
| ✅ Seed Facebook provider | `4_01` greenfield `ProviderId=5`; `5_01_auth_Providers_Facebook` PostDeployment ×3 |
| ✅ Docs / sample stub | README, CONTRIBUTING, Sample.Api Providers.Facebook |

---

## Что в библиотеке уже нормально

- Apple remains stub (`NotSupportedException`) — unchanged.
- Existing Google / Microsoft / GitHub paths untouched.

---

## Приоритет фиксов

_(пусто — открытых пунктов дельты нет; внерелизный backlog → [`TO-DO.md`](TO-DO.md).)_

Ship: PR → `master` · CI · operators `5_01_auth_Providers_Facebook` · tag `v2.6.0` + NuGet.
