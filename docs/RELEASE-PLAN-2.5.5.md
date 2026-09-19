Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.5` · **ветка:** `master` · **база:** `origin/master` (`v2.5.4`) · **дата:** `2026-09-19`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.5
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.4.md`](RELEASE-PLAN-2.5.4.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · **0** файлов (tip = `v2.5.4`). **WT:** **9** файлов · **+271 / −67** (incl. this plan + to-master). Open C/H/M/L пустые.

**CodeRabbit:** не запускался.

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

### NuGet push via OIDC
CI: `NuGet/login@v1` + `id-token: write` вместо долгоживущего `secrets.NUGET_API_KEY` в workflow. Временный API key из OIDC на job; scope push — `master` / `release/*` / `hotfix/*` / `dev`.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M98 NuGet push OIDC | `dotnet.yml`: `NuGet/login@v1` (`user: peshkov`); push с `steps.nuget-login.outputs.NUGET_API_KEY`; `permissions.id-token: write` |

---

## Что в библиотеке уже нормально

- Дельта **не** трогает library / flows / NuGet public API — CI auth + docs backfill.

---

## Приоритет фиксов

1. Commit WT на `master` (или hotfix → `master`).
2. CI: убедиться, что NuGet OIDC login + push работают на `master`.
3. Tag `v2.5.5` + NuGet (CI).

Внерелизный backlog → [`TO-DO.md`](TO-DO.md).
