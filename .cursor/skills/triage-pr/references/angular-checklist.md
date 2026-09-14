# Чеклист review PR — Angular (`client/`)

Использовать для deep review PR в `triage-pr` и `bugbot`, когда затронут `client/`. Канон — `.cursor/rules/200–211`, `301-testing-angular.mdc`.

## Security & Auth (критично)

- Не логировать пароли, tokens, PII в `console.*` и UI
- Auth-формы: `withCredentials` / cookies там, где API требует session (см. identity flows)
- Не хранить secrets/tokens в `localStorage` без явной необходимости и review
- UGC/HTML: `DomSanitizer` для пользовательского HTML; без `[innerHTML]` на сыром вводе
- Runtime config / env: без hardcode production keys в `environment.ts` (см. runtime-config)
- См. `.cursor/rules/210-frontend-error-handling.mdc`, `105-backend-security.mdc` (cross-cutting)

## Angular — компоненты и структура

- Новые компоненты: `standalone: true`, явный `imports: [...]`, без NgModules
- Обязательны `template`/`templateUrl` и `styles`/`styleUrls` (или inline); не `styles: []`
- `private readonly` для инжектированных сервисов; `protected` для шаблонного API
- Строгая типизация: без `any`; интерфейсы/`export type` для DTO
- См. `.cursor/rules/200-frontend-angular-general.mdc`

## RxJS и подписки

- Только RxJS для async; Promises — оборачивать в Observable при необходимости
- Каждая подписка: `takeUntilDestroyed(this.destroyRef)`; не `ngOnDestroy` для отписки
- Методы сервисов с Observable — суффикс `$` (`get$`, `loadResource$`)
- Ошибки: `catchError`; side effects — `tap`; переключение потоков — `switchMap`
- См. `.cursor/rules/201-frontend-rxjs-only.mdc`

## HTTP и API

- Только `ApiService`; не инжектировать `HttpClient` в компоненты
- Endpoints из `API_ENDPOINTS` (`core/constants`); без строковых литералов путей
- Общие ошибки (401/403/5xx) — в `ApiService`; специфичные — `catchError` в компоненте
- Interceptors для общих заголовков (language, correlation id)
- См. `.cursor/rules/203-frontend-http.mdc`

## i18n

- Пользовательский текст только через `translate` pipe / `TranslationService`; без hardcoded UI strings
- Папки i18n по страницам; `menu.*.json` только в корне `i18n`
- При изменении ключей — синхронизировать все `strings.*.json` (источник истины — см. `001-team-workflow.mdc` / skill `translate-resources`)
- Паттерн `isResourceLoaded` + `*ngIf="isResourceLoaded"` для page resources
- См. `.cursor/rules/205-frontend-i18n.mdc`

## Формы и ввод

- Reactive Forms (`FormBuilder`, `ReactiveFormsModule`)
- Переиспользовать `shared/components` (`InputText`, `InputEmail`, …); не дублировать поля
- Валидаторы: `form.validators.ts` — `noWhitespaceOnly`, пресеты `emailValidators`, `codeValidators`, `textValidators`
- Пароль на клиенте **не нормализовать**; email/OTP/text — по матрице `211`
- Телефоны: `libphonenumber-js` + `CountryService`
- Label/placeholder — только ключи переводов
- См. `.cursor/rules/206-frontend-forms-ugc.mdc`, `211-frontend-input-normalization.mdc`

## Routing и guards

- Lazy loading: `loadComponent` / `loadChildren`; без eager imports страниц в `app.routes`
- Пути из `ROUTE_PATH` (`core/constants/route.path.ts`)
- Guards: функциональные (`CanMatchFn`, `CanActivateFn`) + `inject()`; не классовые guards
- См. `.cursor/rules/208-frontend-angular-routing.mdc`, `209-frontend-guards.mdc`

## UI / Tailwind

- Предпочитать Tailwind в шаблонах; SCSS — только при необходимости
- Не создавать `.component.scss` по умолчанию; не дублировать `@guru/ui` компоненты
- `@apply` — только для простых паттернов; сложные градиенты — inline CSS
- См. `.cursor/rules/204-frontend-ui-tailwind.mdc`

## Ошибки и UX

- Сообщения пользователю — через переводы (`TranslationService.get` / pipe)
- Перед submit: `form.invalid` → `markAllAsTouched()`
- Не показывать stack traces / технические детали пользователю
- См. `.cursor/rules/210-frontend-error-handling.mdc`

## Стиль и imports

- 2 пробела; одинарные кавычки; пробелы после `:` в объектах
- Порядок imports: `@angular/*` → внешние → `@guru/*` → app absolute → relative (`.`)
- См. `.cursor/rules/207-frontend-formatting-and-style.mdc`, `.editorconfig`

## Тесты

Канон: `.cursor/rules/301-testing-angular.mdc`.

- Jasmine + Karma; `describe` / `it` / `beforeEach`
- Standalone component в `TestBed.configureTestingModule({ imports: [Component] })`
- Моки/spy для зависимостей
- Async: параметр `done: DoneFn`
- Запуск: `cd client && yarn test`
- Новое поведение компонентов/сервисов — добавить или обновить `*.spec.ts`

## Hotspots (auth / account / checkout)

При изменениях в этих областях проверять особенно:

- `client/src/app/core/` — HTTP, guards, runtime-config, validators
- Identity/account pages — cookies, redirect flows, error states
- Stripe/checkout UI — не логировать payment data; корректные return URLs

## Breaking / contract

- Изменения публичных routes или API contracts — согласованы с backend и README
- Изменения i18n keys — все локали обновлены
