Ниже — **проблемы внутри библиотеки**, по уровню критичности. Аудит по дельте ветки относительно базовой ветки (обычно `master`).

> **Версия:** `2.9.0` (closed) · **ветка:** `release/libphonenumber-csharp` · **база:** `origin/master` (`v2.8.0`) · **дата:** `2026-11-14`
>
> **Релиз (если есть):** https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.9.0
>
> **Легенда:** ⬜ open · ✅ done · 🟨 partial / принято · ❌ blocker
>
> **Предыдущий план:** [`RELEASE-PLAN-2.8.0.md`](RELEASE-PLAN-2.8.0.md)
>
> Дельта: `origin/master...HEAD` — **4** коммита · **13** файлов · **+225 / −122**. Open C/H/M/L пустые.

**CodeRabbit:**
- не запускался.

**PR:** —

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

- Keep public `PhoneE164` API / `PhoneNumbers` namespace; swap underlying NuGet metadata package only (`PhoneNumbersCore` → `libphonenumber-csharp`).
- Triage Cursor skill `undici` resolution bump in the same WT — tooling noise, not part of the phone ship slice (commit or drop separately).

---

## Закрыто (проверено в коде)

| # | Суть |
|---|---|
| ✅ #H35 PhoneNumbersCore → libphonenumber-csharp | Library `9.0.43` on net8/9/10; MSBuild prop rename |
| ✅ #M122 config.nuspec + README deps | Consumer dependency list / nuspec groups updated |
| ✅ #M123 Sample.Api drop PhoneNumbersCore | Unused direct PackageReference removed (comes via library) |
| ✅ #M124 PhoneE164Tests RO mobile | `+40799618836` asserts valid under new metadata |
| ✅ #H36 BREAKING phone metadata package | `docs/BREAKING.md` From 2.8.0→2.9.0 + CHANGELOG `v2.9.0` |
| ✅ #H37 Cross.Messaging + Microsoft.* bumps | Messaging `2.4.1`; EF/Http/JWT TFM-aligned latest stables; Sample/Tests net8 refs |
| ✅ #M125 README Supported TFMs | Badge, section, intro line (`net8.0` / `net9.0` / `net10.0`) |

---

## Что в библиотеке уже нормально

- `PhoneE164` still uses `PhoneNumberUtil.GetInstance()` + E.164 regex gate; only metadata source package changes.
- Form `PhoneNumber` / `CollectFormStep` paths unchanged.

---

## Приоритет фиксов

_(пусто — релиз `2.9.0` опубликован; открытый backlog → [`TO-DO.md`](TO-DO.md).)_
