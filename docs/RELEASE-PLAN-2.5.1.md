Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.1` · **ветка:** `hotfix/gitversion-hotfix-patch-increment` · **база:** `origin/master` (`v2.5.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.1
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.0.md`](RELEASE-PLAN-2.5.0.md)
>
> Дельта: `origin/master...HEAD` — **0** коммитов · **0** файлов (ветка tip = `master` / `v2.5.0`). **WT:** **6** файлов · **+136 / −117**. Open C/H/M/L пустые.

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

### GitVersion: hotfix → Patch, release → Minor
После squash hotfix в `master` GV5 мог дать **Minor** вместо **Patch**. Конфиг GV6: `main.source-branches` = только `release` + `hotfix` (без `develop`); root `increment: Patch`; `commit-message-incrementing: Disabled`; digits в имени ветки игнорируются. Ожидание: squash `hotfix/*` → Patch, `release/*` → Minor.

### `next-version: 1.0.0` как ConfiguredNextVersion fallback
Якорь низкий; при наличии тегов побеждает `TaggedCommit`. Не поднимать `next-version` под каждый ship.

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #M93 GitVersion 6 hotfix=Patch | `GitVersion.yml` GV6: Inherit с hotfix/release; root Patch; без VersionInBranchName |
| ✅ #M94 CI GitVersion 6.8.2 | actions `v4.7.0` / `versionSpec: 6.8.2`; убран `useConfigFile` |
| ✅ #M95 Tag only stable SemVer | create/push tag если `semVer` без `-`; NuGet push только master/release/hotfix/dev |
| ✅ #M96 Docs align tag `v2.5.0` | plan/CHANGELOG/to-master/TO-DO: `2.4.1` → `2.5.0` под фактический тег |
| ✅ #L18 Sonar `projectKey` | `dCross.Identity` → `Cross.Identity` |

---

## Что в библиотеке уже нормально

- Дельта **не** трогает library / flows / NuGet API — только versioning + CI + docs rename.
- Предыдущий релиз `v2.5.0` (Register optional Password) уже на `master` / NuGet.

---

## Приоритет фиксов

1. Commit WT на `hotfix/gitversion-hotfix-patch-increment`.
2. PR → `master` · CI · убедиться, что после merge GitVersion на master даёт **`2.5.1`** (Patch), не `2.6.0`.
3. Tag `v2.5.1` + NuGet (CI).

Внерелизный backlog → [`TO-DO.md`](TO-DO.md).
