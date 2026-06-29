---
name: issue-triage
description: >-
  Issue triage для Cross.Identity: аудит открытых issues, категоризация,
  дубликаты, cross-ref с PR, оценка риска (auth/JWT/security), draft-комментарии.
  Args: "all" — deep analysis всех; номера "42 57" — фокус; "en"/"fr" — язык
  таблиц (по умолчанию ru).
---

# Issue Triage — Cross.Identity

Триаж GitHub issues для репозитория **Cross.Identity** (NuGet-библиотека identity/auth: JWT, process engine, OAuth flows).

## Когда использовать

| Сценарий | Действие |
|----------|----------|
| «Разбери issues» / «issue triage» | Запустить этот skill |
| >10 открытых issues без triage | Предложить audit |
| Issue stale >30 дней | Включить в таблицу Stale |

Связанные skills: `pr-triage`, `cross-identity-triage`, `repo-recap` (если добавлен).

## Предусловия

```bash
git rev-parse --is-inside-work-tree
gh auth status
```

Для сжатия вывода команд использовать обёртку:

```bash
.cursor/triage/rtk-gh.sh
```

Если `rtk` не установлен — скрипт прозрачно вызывает `gh`.

## Язык

- Таблицы и резюме: **русский** (по умолчанию), `en` — английский
- Комментарии в GitHub: **всегда английский**

## Workflow — 3 фазы

### Phase 1 — Audit (всегда)

#### Data gathering (параллельно)

```bash
REPO=$(.cursor/triage/rtk-gh.sh repo view --json nameWithOwner -q .nameWithOwner)

.cursor/triage/rtk-gh.sh issue list --state open --limit 100 \
  --json number,title,author,createdAt,updatedAt,labels,assignees,body,comments

.cursor/triage/rtk-gh.sh pr list --state open --limit 50 --json number,title,body

.cursor/triage/rtk-gh.sh issue list --state closed --limit 20 \
  --json number,title,labels,closedAt

.cursor/triage/rtk-gh.sh api "repos/${REPO}/collaborators" --jq '.[].login'
```

**Fallback collaborateurs** (403/404):

```bash
.cursor/triage/rtk-gh.sh pr list --state merged --limit 10 --json author --jq '.[].author.login' | sort -u
```

`author` — объект `{login: "..."}`; извлекать `.author.login`.

#### Анализ — 6 измерений

**1. Категоризация** (labels > инференс по title/body):

- **Bug**: crash, error, fail, broken, regression, token, jwt, auth
- **Feature**: add, implement, support, new, flow, oauth
- **Enhancement**: improve, optimize, refactor, performance
- **Question**: how, why, help, docs, documentation
- **Duplicate Candidate**: см. п.3

**2. Cross-ref PRs**:

- Сканировать body PR: `fixes #N`, `closes #N`, `resolves #N`
- Map: `issue_number → [PR numbers]`
- PR merged + issue open → рекомендовать закрытие

**3. Дубликаты**:

- Jaccard по словам заголовков >60% → кандидат
- Overlap keywords в body >50% → усиление сигнала
- Сравнивать с 20 последними closed

**4. Риск** (для identity-библиотеки — приоритет security):

- **Красный**: CVE, vulnerability, injection, auth bypass, security, exploit, token leak, credentials, RCE, XSS, jwt bypass, refresh token
- **Жёлтый**: breaking change, migration, deprecation, API removal, incompatible
- **Зелёный**: остальное

**5. Staleness**:

- >30d без активности → Stale
- >90d → Very Stale

**6. Рекомендации**:

- `Accept & Prioritize`, `Label needed`, `Comment needed`, `Linked to PR`,
  `Duplicate candidate`, `Close candidate` (не для collaborator), `PR merged → close`

#### Output — 5 таблиц

См. формат в оригинальном workflow (Critiques / Linked to PR / Active / Duplicates / Stale + Résumé).

0 issues → `Нет открытых issues.` и завершить.

После таблиц — копировать в буфер (`pbcopy` / `xclip` / `wl-copy`).

### Phase 2 — Deep Analysis (opt-in)

Для выбранных issues — `Task` с `subagent_type: generalPurpose` параллельно.

Контекст Cross.Identity для агента:

- Библиотека: `Cross.Identity/` — process engine, JWT, OAuth, flows в `ProcessEngine/Definitions/Flows/`
- Тесты: `Cross.Identity.Tests/`
- Документация: `FLOWS.md`, `RefreshToken.md`
- Правила: `.cursor/rules/104-backend-auth.mdc`, `105-backend-security.mdc`

Шаблон комментария: `templates/issue-comment.md`.

### Phase 3 — Actions (только с подтверждением)

- `.cursor/triage/rtk-gh.sh issue comment {num} --body-file -`
- `.cursor/triage/rtk-gh.sh issue edit {num} --add-label "{label}"`
- `.cursor/triage/rtk-gh.sh issue close {num} --reason "not planned"`

**Никогда** не постить/закрывать без `AskQuestion`.

## Cross.Identity — специфика в комментариях

Для bug reports запрашивать:

- Версию NuGet / commit
- Target framework (net7/net8)
- Flow name (`license.TokenByCode`, `shop.auth`, и т.д.)
- Шаги воспроизведения без реальных токенов/паролей

## Edge cases

| Ситуация | Поведение |
|----------|-----------|
| 0 issues | Сообщить и выйти |
| >50 comments | Резюме 5 последних |
| Rate limit | Уменьшить `--limit`, уведомить |
| Issue collaborator | Не предлагать close без явного запроса |

## Сохранение отчёта

При полном triage сохранять в `.cursor/triage/docs/issues-YYYY-MM-DD.md`.
