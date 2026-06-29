---
name: pr-triage
description: >-
  PR triage для Cross.Identity: аудит открытых PR, deep review, draft review
  comments. Использует rtk gh для сжатия вывода. Args: "all", номера PR,
  "en"/"ru" для языка таблиц.
---

# PR Triage — Cross.Identity

## Когда использовать

| Сценарий | Действие |
|----------|----------|
| «Разбери PRs» / «pr triage» | Запустить skill |
| >5 открытых PR без review | Предложить audit |
| PR stale >14 дней | Флаг в таблице |

## Предусловия

```bash
git rev-parse --is-inside-work-tree
gh auth status
```

Команды GitHub — через `.cursor/triage/rtk-gh.sh` (RTK сжимает вывод, fallback на `gh`).

## Язык

- Таблицы: русский (по умолчанию), `en` — английский
- GitHub comments: английский

## Phase 1 — Audit

### Data gathering

```bash
REPO=$(.cursor/triage/rtk-gh.sh repo view --json nameWithOwner -q .nameWithOwner)

.cursor/triage/rtk-gh.sh pr list --state open --limit 50 \
  --json number,title,author,createdAt,updatedAt,additions,deletions,changedFiles,isDraft,mergeable,reviewDecision,statusCheckRollup,body

.cursor/triage/rtk-gh.sh api "repos/${REPO}/collaborators" --jq '.[].login'
```

Для каждой PR (приоритет — кандидаты на overlap):

```bash
.cursor/triage/rtk-gh.sh api "repos/${REPO}/pulls/{num}/reviews" \
  --jq '[.[] | .user.login + ":" + .state] | join(", ")'

.cursor/triage/rtk-gh.sh pr view {num} --json files --jq '[.files[].path] | join(",")'
```

### Классификация

**Размер**: XS <50, S 50–200, M 200–500, L 500–1000, XL >1000 additions.

**Детекции**: overlaps >50% файлов, clusters (3+ PR от автора), stale >14d, CI clean/dirty.

**Наши PRs**: автор в collaborators.

**Внешние — готовые**: ≤1000 additions, ≤10 files, не CONFLICTING, CI clean/unstable.

**Внешние — проблемные**: XL, конфликт, CI dirty, overlap.

### Таблицы output

Секции: Nos PRs / Externes prêtes / Externes problématiques + Résumé.

0 PRs → завершить.

### Cross.Identity file hotspots

При overlap/review обращать внимание на:

- `Cross.Identity/Services/` — JWT, OAuth, codes
- `Cross.Identity/ProcessEngine/` — flows, steps
- `Cross.Identity/Entities/` — EF configurations
- `Cross.Identity.Tests/` — coverage

## Phase 2 — Deep Review (opt-in)

`Task` с `subagent_type: bugbot` или `generalPurpose` параллельно.

```bash
.cursor/triage/rtk-gh.sh pr diff {num}
```

Чеклист: `references/dotnet-checklist.md` + `.cursor/rules/105-backend-security.mdc`.

Структура ответа: Critical 🔴 / Important 🟡 / Suggestions 🟢 / What's Good ✅.

## Phase 3 — Comments (AskQuestion обязателен)

Шаблон: `templates/review-comment.md` (ручной deep review).

### Автокомментарий в PR (CI)

При `pull_request` opened/synchronize CI постит **wshm-style** комментарий:

- Скрипт: `.cursor/triage/post-pr-triage.mjs`
- Шаблон: `.cursor/triage/templates/pr-automated-triage-comment.md`
- Маркер: `<!-- cross-identity-triage -->` (обновление при push, не дублирование)
- Draft PR — пропуск

```bash
PR_NUMBER=42 CURSOR_API_KEY=... yarn pr-triage
```

```bash
.cursor/triage/rtk-gh.sh pr comment {num} --body-file -
```

## Сохранение

`.cursor/triage/docs/prs-YYYY-MM-DD.md`
