# Automated Triage Reports

Отчёты triage для репозитория Cross.Identity.

## Локально (Cursor Agent + skills)

```bash
# Полный triage
# В чате Cursor: «Запусти cross-identity-triage»

# Или по частям:
# «Запусти issue-triage»
# «Запусти pr-triage»
```

Skills: `.cursor/skills/{issue-triage,pr-triage,cross-identity-triage}/`

## Скрипты

```bash
# Сбор данных (rtk gh если установлен)
bash .cursor/triage/collect-data.sh

# CI-агент (нужен CURSOR_API_KEY, Node 20.19.4)
cd .cursor/triage && yarn install --ignore-engines && CURSOR_API_KEY=... yarn triage
```

На Node 20 SDK использует `JsonlLocalAgentStore` (`cursor-agent-local.mjs`), не `node:sqlite`.

## CI

Workflow `.github/workflows/triage.yml`:

- **Расписание**: понедельник 06:00 UTC
- **workflow_dispatch**: ручной запуск
- **issues opened**: сбор данных
- **pull_request** opened/synchronize: AI-комментарий в PR (wshm-style)

### Secrets

| Secret | Обязателен | Назначение |
|--------|------------|------------|
| `CURSOR_API_KEY` | Да (для AI-отчёта) | Cursor SDK в CI |
| `GITHUB_TOKEN` | Авто | `gh` CLI |

Создать ключ: [Cursor Dashboard → Integrations](https://cursor.com/dashboard/integrations)

### PR opened / updated

Workflow `triage.yml` → job **PR automated comment**:

- Cursor Agent анализирует diff
- Постит комментарий в стиле wshm (category, priority, confidence, summary, files)
- При новом push **обновляет** тот же комментарий (маркер `<!-- cross-identity-triage -->`)

Ручной тест: **Actions → Triage → Run workflow** → поле `pr_number`.

### Артефакты

- `.cursor/triage/docs/ci-report-YYYY-MM-DD.md`
- `.cursor/triage/docs/.data/*.json` (в artifact, не в git)

## RTK

Установка (опционально, для сжатия `gh` output):

```bash
curl -fsSL https://raw.githubusercontent.com/rtk-ai/rtk/master/install.sh | sh
rtk gain  # проверка: Rust Token Killer, не Type Kit
```

Скрипт `.cursor/triage/rtk-gh.sh` автоматически использует `rtk gh` или fallback на `gh`.
