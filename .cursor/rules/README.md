# Правила проекта (Cursor Rules)

## Структура файлов

### 000-099: Глобальные правила
- `000-global.mdc` - Глобальные правила проекта
- `001-team-workflow.mdc` - Правила командной работы
- `002-multi-repo.mdc` - Правила работы с мульти-репозиторием
- `003-triage.mdc` - GitHub triage (issues/PRs, skills, RTK, CI)
- `004-release-plan.mdc` - План релиза `dev`→`master`, синхронизация «Сводка чеклистов»

### Cursor triage scripts

- `.cursor/triage/` - RTK/gh wrappers, CI runners (`collect-data.sh`, `post-pr-triage.mjs`), отчёты в `docs/`

### 100-199: Backend (.NET)
- `100-backend-dotnet-general.mdc` - Общие правила .NET бэкенда
- `101-backend-architecture.mdc` - Архитектура бэкенда
- `102-backend-efcore.mdc` - Entity Framework Core
- `103-backend-api-style.mdc` - Стиль API
- `104-backend-auth.mdc` - Аутентификация и авторизация
- `105-backend-security.mdc` - Безопасность бэкенда (валидация, санитизация, CORS, HTTPS)
- `106-backend-formatting-and-style.mdc` - Форматирование и стиль кода бэкенда
- `107-backend-observability.mdc` - Наблюдаемость бэкенда

### 200-299: Frontend (Angular)
- `200-frontend-angular-general.mdc` - Общие правила Angular
- `201-frontend-rxjs-only.mdc` - Правила работы с RxJS
- `202-frontend-state-stores-signals.mdc` - Управление состоянием: Stores и Signals
- `203-frontend-http.mdc` - Правила работы с HTTP
- `204-frontend-ui-tailwind.mdc` - Правила работы с UI и Tailwind CSS
- `205-frontend-i18n.mdc` - Правила интернационализации
- `206-frontend-forms-ugc.mdc` - Правила работы с формами
- `207-frontend-formatting-and-style.mdc` - Форматирование и стиль кода фронтенда
- `208-frontend-angular-routing.mdc` - Роутинг в Angular
- `209-frontend-guards.mdc` - Guards (Защита роутов)
- `210-frontend-error-handling.mdc` - Обработка ошибок

### 300-399: Testing
- `300-testing-dotnet.mdc` - Тестирование .NET
- `301-testing-angular.mdc` - Тестирование Angular

### 400-499: Output Format
- `400-output-format.mdc` - Правила форматирования вывода

## Использование

Все правила написаны на русском языке и применяются автоматически при работе с проектом через Cursor IDE.
