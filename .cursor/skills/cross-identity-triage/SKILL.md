---
name: cross-identity-triage
description: >-
  Полный triage Cross.Identity: issue-triage + pr-triage параллельно, кросс-анализ
  (двойное покрытие, security gaps, P0 без PR, dirty CI). Сохраняет отчёт в
  docs/triage/. Args: "en"/"ru", "no save" — без файла.
---

# Cross-Identity Triage (оркестратор)

Объединяет `issue-triage` + `pr-triage` + кросс-анализ issues × PRs.

## Когда использовать

- Еженедельно или перед релизом NuGet
- Перед sprint planning
- После CI workflow `triage.yml` — для интерпретации артефактов

## Phase 0 — Предусловия

```bash
git rev-parse --is-inside-work-tree
gh auth status
date +%Y-%m-%d
```

Или запустить сбор данных:

```bash
.cursor/triage/collect-data.sh
```

## Phase 1 — Data gathering (параллельно)

**Issues** (через RTK):

```bash
.cursor/triage/rtk-gh.sh issue list --state open --limit 150 \
  --json number,title,author,createdAt,updatedAt,labels,assignees,body

.cursor/triage/rtk-gh.sh issue list --state closed --limit 20 \
  --json number,title,labels,closedAt
```

**PRs**:

```bash
.cursor/triage/rtk-gh.sh pr list --state open --limit 200 \
  --json number,title,author,createdAt,updatedAt,additions,deletions,changedFiles,isDraft,mergeable,reviewDecision,statusCheckRollup,body
```

Файлы PR — для overlap detection (см. `pr-triage`).

## Phase 2 — Индивидуальный triage

Выполнить логику `issue-triage` и `pr-triage` (Phase 1 каждого) — таблицы issues и PRs.

## Phase 3 — Кросс-анализ

### 3.1 Двойное покрытие — 2 PR на 1 issue

| Issue | PR1 | PR2 | Verdict |
|-------|-----|-----|---------|

Правила: меньший scope, CI clean, internal PR, overlap >80% → конфликт.

### 3.2 Security gaps

Для issues с риском «красный» — findings без PR (особенно JWT, refresh tokens, OAuth).

### 3.3 P0/P1 без PR

Labels/m keywords: crash, auth, token, jwt, security.

### 3.4 Наши PR dirty

CI dirty / CONFLICTING — причина (overlap, нужен rebase).

### 3.5 PR без `fixes #N`

Внутренние PR без привязки к issue.

## Phase 4 — Output

Резюме:

| Категория | Count |
|-----------|-------|
| PRs готовы к merge (наши) | N |
| Quick wins (внешние) | N |
| Double coverage | N |
| P0/P1 без PR | N |
| Security без PR | N |
| Dirty PRs | N |

### Сохранение

`docs/triage/Cross.Identity-YYYY-MM-DD.md` (если не `no save`).

Структура файла:

```markdown
# Cross.Identity Triage — YYYY-MM-DD

## Issues (таблицы)
## PRs (таблицы)
## 1. Double coverage
## 2. Security gaps
## 3. P0/P1 без PR
## 4. Dirty PRs
## 5. Actions prioritaires
## Résumé chiffré
```

## CI integration

После `triage.yml` читать:

- `docs/triage/.data/*.json` — сырые данные
- `docs/triage/ci-report-YYYY-MM-DD.md` — отчёт агента CI

Дополнить кросс-анализом вручную при необходимости.

## Правила

- GitHub actions (комментарии/close) — только `AskQuestion`
- Langue tableaux: ru (default), en по аргументу
- RTK: всегда `.cursor/triage/rtk-gh.sh` для gh-команд
