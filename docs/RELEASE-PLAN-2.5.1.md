Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.5.1` (published / closed) · **ветка:** `hotfix/gitversion-hotfix-patch-increment` · **база:** `origin/master` (`v2.5.0`) · **дата:** `2026-09-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.5.1
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.5.0.md`](RELEASE-PLAN-2.5.0.md)
>
> Дельта: `origin/master...HEAD` — **3** коммита · **8** файлов · **+273 / −170**. Open C/H/M/L пустые.

**CodeRabbit:** не запускался.

**PR:** [#23](https://github.com/denis-peshkov/Cross.Identity/pull/23) (`Fix GitVersion increment`).

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
| ✅ #L19 TO-DO Принято без SemVer | skill + strip version numbers from «Принято» bullets |

---

## Что в библиотеке уже нормально

- Дельта **не** трогает library / flows / NuGet API — только versioning + CI + docs rename.
- Предыдущий ship Register optional Password на `master` под тегом `v2.5.0` (local); GitHub Release page может отставать.

---

## Приоритет фиксов

_(пусто — релиз `2.5.1` закрыт; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
