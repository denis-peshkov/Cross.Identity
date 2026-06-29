# План готовности релиза `dev` → `master`

> **Дата анализа:** 2026-06-29  
> **Ветка:** `dev`  
> **База сравнения:** `master...dev` (merge-base `163b8a5`)  
> **Цель:** исчерпывающий перечень нового функционала и чеклист проверки перед merge в `master`  
> **Легенда:** ⬜ open · ✅ done · 🟨 partial · ❌ blocker  
> **Источники:** `dotnet test`, `git diff master...dev`, `gh run list` (проверено 2026-06-29)

**Сводка чеклистов:** **107** пунктов — ✅ **55** (51%) · 🟨 **23** (21%) · ⬜ **26** (24%) · ❌ **3** (3%)

---

## Сводка изменений

| Метрика | Значение |
|---------|----------|
| Коммитов | 110 |
| Файлов | 299 |
| Строк | +8 626 / −3 733 |
| Тестов (`dotnet test`) | 292 total · 292 passed · 0 failed |

| Область | Файлов | Роль в релизе |
|---------|--------|---------------|
| Cross.Identity | 138 | NuGet-библиотека: JWT, refresh, OAuth flows, licensing, cleanup |
| Cross.Identity.Tests | 62 | Unit + integration (NUnit) |
| Sample.Api | 4 | Минимальный API, smoke через Swagger |
| .github/workflows | 2 | CI/CD (`dotnet.yml`, `triage.yml`) |
| .cursor (rules + triage) | 21 | Конвенции, automated triage (не runtime) |
| docs | 2 | Triage-отчёты, этот план |
| Удалённые in-repo пакеты | ~65 | `Cross.Notification`, `Cross.PepperVault.*` → NuGet |

---

## Оглавление

1. [Полный перечень нового функционала](#1-полный-перечень-нового-функционала)
2. [Breaking changes](#2-breaking-changes-обязательно-для-потребителей-пакета)
3. [Чеклисты проверки по областям](#3-чеклист-по-функциональным-блокам)
4. [Автотесты — матрица покрытия](#4-автотесты--матрица-покрытия)
5. [Ручное/E2E через Sample.Api](#5-ручноеe2e-тестирование-через-sampleapi)
6. [CI/CD и инфраструктура](#6-cicd-и-инфраструктура)
7. [Документация и release notes](#7-документация-и-release-notes)
8. [Миграция БД](#8-миграция-бд-для-существующих-инсталляций)
9. [Blockers и риски](#9-блокеры-и-риски-перед-merge)
10. [Go / No-Go и порядок прогона](#10-рекомендуемый-порядок-работ-release-gate)

---

## 1. Полный перечень нового функционала

| # | Блок | Ключевые изменения | Коммиты/файлы |
|---|------|-------------------|---------------|
| A | **Регистрация (US-129)** | `license.Register` — email+password, без `ConfirmPassword`; `createUser` → `sendCode` → `LastCode`+`UserId` | `license.Register.json`, `UserService`, flow-тесты |
| B | **External OAuth** | Google/Microsoft/GitHub/Apple; шаги `initiateExternalLogin` / `completeExternalLogin`; flows `ExternalLogin`, `ExternalLoginCallback`; linking | `ExternalLoginService`, `ExternalOAuthProviders`, 2 JSON-flow |
| C | **Refresh Token / «Remember me»** | `AbsoluteExpiresAt`, цепочка `FamilyId`, ротация, фоновая очистка | `JwtTokenService`, `RefreshTokenStep`, `ExpiredRefreshTokenCleanupHostedService` |
| D | **Лицензирование JWT** | Проверка при первом `ExecuteAsync`; секция `CrossIdentity:LicenseKey` | `LicenseAccessor`, `LicenseValidator`, `FlowExecutor` |
| E | **Developer Mode** | Коды сохраняются в БД без отправки email/SMS | `CodeService`, `SendCodeStep`, `Authentication:DeveloperMode` |
| F | **Token / TokenByCode / RequestCode** | Сквозной сценарий request code → token by code | `License_RequestCode_TokenByCode_FlowTests` |
| G | **Reset Password** | Уведомление по email/SMS после смены; новые поля формы | `ResetPasswordStep`, `license.ResetPassword.json` |
| H | **Инфраструктура** | `UnitTests` → `Tests`, CI triage, PR-триггеры, обновление зависимостей | `.github/workflows/`, `.cursor/triage/` |

---

## 2. Breaking changes (обязательно для потребителей пакета)

### 2.1 API / контракты flow

| Изменение | Было (master) | Стало (dev) | Действие |
|-----------|---------------|-------------|----------|
| Операция `GetUser` | `FlowOperationEnum.GetUser` | **`GetUserId`** | Обновить клиенты: `license/GetUserId` |
| Flow-файл | `license.GetUser.json` | **`license.GetUserId.json`** | Обновить кастомные overrides |
| Удалённые flows | `license.Auth.json`, `license.register1.json` | удалены | Проверить, что никто их не вызывает |
| Ответ `collectResult` с 1 полем | голое значение (`"abc"`) | **всегда объект** `{ "field": "abc" }` | Обновить десериализацию на клиентах |
| `IFlowExecutor` / `FlowExecutor` | public class | **internal class** | Публичный контракт — только `IFlowExecutor` |

### 2.2 Модель данных

| Изменение | Действие |
|-----------|----------|
| Удалено поле `NormalizedEmail` | Миграция БД: колонка `NormalizedEmail` → использовать `Email`; при EF — новая миграция |
| `RefreshTokenEntity.AbsoluteExpiresAt` | Новая колонка; backfill для существующих токенов |
| `UserExternalLoginEntity` + FK на `ProviderEntity` | Проверить схему и seed провайдеров (Google, Microsoft, …) |

### 2.3 Зависимости NuGet

| Было | Стало |
|------|-------|
| `System.IdentityModel.Tokens.Jwt` | **`Microsoft.IdentityModel.JsonWebTokens` 8.16** |
| In-repo `Cross.Notification`, `Cross.PepperVault.*` | **`Cross.Messaging`**, **`Cross.PepperVault`** (NuGet) |
| `Magick.NET.Core` | **удалён** |
| `Cross.ErrorHandlers` 7.3 → **7.6**, `Cross.Headers` 1.0 → **1.2.1** | Обновить у потребителей при конфликтах |

> **Замечание:** `config.nuspec` синхронизирован с `.csproj` (TFM-группы, актуальные версии).

---

## 3. Чеклист по функциональным блокам

### A. Регистрация (`license.Register`)

**Автотесты:** `License_Registration_FlowTests`, `LicenseRegisterFlowTests`, `CreateUser_StepTests`

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| A1 | Регистрация с валидным email+password → `UserId` + `LastCode` в ответе | Integration | ✅ `License_Registration_FlowTests` |
| A2 | Повторная регистрация с тем же email → ошибка | Integration | 🟨 unit `CreateUserAsync_ShouldThrowWhenEmailExists`; flow ⬜ |
| A3 | Валидация пароля (min 8, max 128) | Unit/Integration | ✅ `Handle_InvalidInput_ShouldThrowValidationException` |
| A4 | `ConfirmPassword` больше не требуется — старые клиенты не ломаются | Integration | ✅ тест без `ConfirmPassword` |
| A5 | Код подтверждения сохраняется в `EmailVerifications` | Integration | 🟨 `CodeServiceTests`; в registration flow не проверяется БД |
| A6 | `edoctors.Register` — по-прежнему с `ConfirmPassword` | Integration | ✅ `EDoctors_Registration_FlowTests` |
| A7 | `createUser` маппинг полей (`FullName`, `Company`, флаги) | Integration | ⬜ нет теста на расширенный маппинг |

---

### B. External OAuth Login

**Автотесты:** `ExternalLoginServiceTests` (~717 строк), step/factory unit-тесты. **Нет integration flow-тестов** для `license.ExternalLogin` / `ExternalLoginCallback`.

#### B1. Конфигурация (обязательно перед любым ручным тестом)

```json
{
  "Authentication": {
    "ExternalLogin": {
      "CallbackUrl": "https://your-spa/callback",
      "StateLifetime": "00:10:00",
      "Providers": {
        "Google": {
          "ClientId": "...",
          "ClientSecret": "...",
          "IsEnabled": true
        }
      }
    }
  }
}
```

Env: `Authentication__ExternalLogin__CallbackUrl`, `Authentication__ExternalLogin__Providers__Google__ClientId`, и т.д.

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| B1 | `CallbackUrl` задан — иначе `InvalidOperationException` | Unit | ✅ `ExternalLoginServiceTests` |
| B2 | Провайдер не в конфиге → `ValidationException` | Unit | ✅ |
| B3 | Провайдер не в БД (`Providers` table) / disabled → `NotFoundException` | Unit | ✅ |
| B4 | **Initiate:** `POST license/ExternalLogin` → `{ url }` с OAuth redirect | Integration | ✅ `License_ExternalOAuth_FlowTests` |
| B5 | **Callback:** `POST license/ExternalLoginCallback` → токены + `user_id` | Integration | ✅ `ExternalLoginCallback_ShouldReturnTokens_*` |
| B6 | OAuth error (`Error`, `ErrorDescription`) → корректная ошибка | Unit + Integration | ✅ |
| B7 | State TTL истёк → отказ | Unit | ✅ `CompleteAsync_ShouldThrow_WhenStateExpired` |
| B8 | **Новый пользователь** — auto-provision | Integration | ✅ callback создаёт user + external login в БД |
| B9 | **Существующий пользователь** — логин по provider+subject | Unit | ✅ `CompleteAsync_ShouldReturnExistingUser_*` |
| B10 | **Linking:** `LinkUserId` → `is_linking: true` | Unit + Integration | 🟨 unit `CompleteAsync_ShouldLinkProviderToExistingUser`; flow ⬜ |
| B11 | **Linking без auth** → `NotAuthorizedException` | Unit | ✅ |
| B12 | Повторный link того же провайдера → `ValidationException` | Unit | ✅ |
| B13 | Google / Microsoft / GitHub / Apple — профиль | Unit / Manual | 🟨 Google в flow-тестах; остальные — только unit fetch |
| B14 | Multi-instance: state в `IMemoryCache` — не для LB без sticky | Arch review | ⬜ задокументировать ограничение |

---

### C. Refresh Token / Remember Me

Реализовано через `AbsoluteExpiresAt` + `FamilyId` (см. `RefreshToken.md`), не через отдельный флаг `RememberMe`.

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| C1 | Выдача пары access+refresh при `license.Token` (пароль) | Integration | ✅ |
| C2 | Выдача пары при `license.TokenByCode` | Integration | ✅ |
| C3 | Ротация: старый refresh инвалидируется, новый работает | Unit | ✅ `JwtTokenServiceTests` |
| C4 | `AbsoluteExpiresAt` сохраняется при ротации (цепочка) | Unit | ✅ |
| C5 | Refresh после `AbsoluteExpiresAt` → отказ | Unit | 🟨 логика в `ValidateRefreshTokenAsync`; отдельного теста на expired absolute нет |
| C6 | `RefreshTokenAbsoluteExpires` в конфиге влияет на новые цепочки | Unit | ✅ `GenerateRefreshTokenAsync_ShouldUseConfiguredRollingLifetime` |
| C7 | `ExpiredRefreshTokenCleanupHostedService` удаляет просроченные | Unit | ✅ |
| C8 | Интервал очистки `Authentication:TokenCleanupInterval` (default 1h) | Manual | ⬜ |
| C9 | `license.RefreshToken` flow end-to-end | Integration | ✅ `License_RefreshToken_FlowTests` |
| C10 | Reuse старого refresh после ротации → отказ | Unit | 🟨 `InvalidateRefreshTokenAsync` + invalid step; полный reuse-chain ⬜ |

**Конфиг для проверки:**

```json
"Authentication": {
  "Jwt": {
    "AccessTokenExpires": "00:15:00",
    "RefreshTokenExpires": "30.00:00:00",
    "RefreshTokenAbsoluteExpires": "60.00:00:00"
  },
  "TokenCleanupInterval": "01:00:00"
}
```

---

### D. Лицензирование JWT

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| D1 | Ключ не задан → `LogCritical`, flow работает | Unit | ✅ |
| D2 | Невалидный JWT → `LogError`, flow работает | Unit | ✅ |
| D3 | Просроченный ключ → `LogError` + `LogCritical` | Unit | ✅ |
| D4 | Валидный ключ → `LogInformation` с edition/expiry | Unit | ✅ |
| D5 | Проверка только при **первом** вызове (singleton flag) | Unit | ✅ |
| D5b | `CheckLicense` при первом `ExecuteAsync`, flow не блокируется | Integration | ✅ `License_LicenseCheck_FlowTests` |
| D6 | `CrossIdentity:LicenseKey` из appsettings | Manual | 🟨 `LicenseAccessor` + `Sample.Api` appsettings; E2E ⬜ |
| D7 | `CrossIdentity__LicenseKey` из env | Manual | ⬜ |
| D8 | Неверный `ProductType` в ключе | Unit | ✅ |
| D9 | **Production policy:** hard-fail без ключа? (сейчас soft-fail) | Product decision | ⬜ |

---

### E. Developer Mode

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| E1 | `Authentication:DeveloperMode=true` → код в БД, email/SMS **не** отправляются | Unit | ✅ |
| E2 | `DeveloperMode=false` → отправка + сохранение | Unit | ✅ |
| E3 | `LastCode` возвращается в flow-ответе (для dev) | Integration | ✅ |
| E4 | Production: `DeveloperMode` **не задан** или `false` | Manual | ⬜ |
| E5 | `SendCodeStep` тоже учитывает DeveloperMode | Unit | 🟨 код + flow с `DeveloperMode=true`; dedicated step-тест ⬜ |

---

### F. Token / TokenByCode / RequestCode

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| F1 | `license.Token` — пароль ИЛИ код (валидация `atLeastOneRequired`) | Integration | ✅ |
| F2 | `license.TokenByCode` — только код | Integration | ✅ |
| F3 | RequestCode → TokenByCode сквозной сценарий | Integration | ✅ |
| F4 | Неверный код → `IsInvalidCode` / пустой токен | Integration | 🟨 валидный путь ✅; негативный сценарий ⬜ |
| F5 | Истёкший код (TTL) | Unit | ✅ `CodeServiceTests` |
| F6 | Превышение `MaxAttempts` (3) | Unit | ✅ `CodeServiceTests` |
| F7 | `game.Token`, `shop.*` flows — регрессия | Integration | ⬜ только `edoctors.Register` |

---

### G. Reset Password / Forgot Password

| # | Проверка | Тип | Статус |
|---|----------|-----|--------|
| G1 | `license.ForgotPassword` | Integration | ✅ |
| G2 | `ResetPasswordStep` — смена пароля + email-уведомление | Unit | ✅ |
| G3 | Уведомление при ошибке отправки — логируется, flow не падает | Unit | ✅ |
| G4 | `license.ResetPassword.json` — `passwordKey: collectForm.Password`, форма: `Email`, `Code`, `Password` | Code review | ✅ |
| G5 | Integration flow test для `license.ResetPassword` | Integration | ✅ `License_ResetPassword_FlowTests` |
| G6 | Старый пароль / код + новый пароль — бизнес-логика | Manual | ⬜ |

> **Рекомендация:** перед релизом прогнать `license.ResetPassword` вручную через Sample.Api (смена пароля + уведомление).

---

### H. Прочие изменения

| # | Проверка | Статус |
|---|----------|--------|
| H1 | `GetUserId` flow возвращает `{ user_id }` | 🟨 unit `GetUser_StepTests`; flow integration ⬜ |
| H2 | `FlowExecutor` — `collectResult` всегда объект | ✅ flow-тесты возвращают `Dictionary<string, object?>` |
| H3 | `UserService` — provisioning, `ValidateCode`, `SetPassword` | ✅ `UserServiceTests` |
| H4 | Удаление `NormalizedEmail` — поиск по email case-insensitive | 🟨 `ToLowerInvariant` в `UserService`; explicit lookup-тест ⬜ |
| H5 | `PasswordHasher` + Pepper через NuGet `Cross.PepperVault` | 🟨 `PasswordHasherTests` (pepper); Sample.Api wiring ⬜ |
| H6 | JWT encryption (`UseEncryption`, `EncryptionKey` Base64 32 bytes) | ⬜ тесты с `UseEncryption=false` |
| H7 | Переход на `Microsoft.IdentityModel.JsonWebTokens` | 🟨 пакет подключён; downstream validation ⬜ |

---

## 4. Автотесты — матрица покрытия

```bash
# Полный прогон
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj

# По категориям
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# С покрытием (opencover)
dotnet test Cross.Identity.Tests/Cross.Identity.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

| Область | Unit | Integration Flow | Пробел |
|---------|------|------------------|--------|
| Registration | ✅ | ✅ | — |
| Token / TokenByCode | ✅ | ✅ | — |
| RefreshToken | ✅ | ✅ | `License_RefreshToken_FlowTests` |
| External OAuth | ✅ (service) | ✅ | `License_ExternalOAuth_FlowTests` |
| ResetPassword | ✅ (step) | ✅ | `License_ResetPassword_FlowTests` |
| Licensing | ✅ | ✅ | `License_LicenseCheck_FlowTests` |
| ForgotPassword | ✅ | ✅ | `ForgotPassword_StepTests`, `ForgotPassword_StepFactoryTests`, `License_ForgotPassword_FlowTests` |
| game/shop/edoctors flows | ⬜ | 🟨 | `edoctors.Register` only |

**Текущий статус:** 292/292 passed.

---

## 5. Ручное/E2E тестирование через Sample.Api

```bash
dotnet run --project Sample.Api
# Swagger: POST /api/identity/{flow}/{operation}
```

| Сценарий | Endpoint | Body (пример) |
|----------|----------|---------------|
| Регистрация | `license/Register` | `{ "Email": "...", "Password": "..." }` |
| Запрос кода | `license/RequestCode` | `{ "Email": "...", "Ttl": "00:05:00" }` |
| Токен по коду | `license/TokenByCode` | `{ "Email": "...", "Code": "..." }` |
| Токен по паролю | `license/Token` | `{ "Email": "...", "Password": "..." }` |
| Refresh | `license/RefreshToken` | `{ "RefreshToken": "..." }` |
| GetUserId | `license/GetUserId` | `{ "Email": "..." }` |
| OAuth start | `license/ExternalLogin` | `{ "Provider": "Google" }` |
| OAuth callback | `license/ExternalLoginCallback` | `{ "Code": "...", "State": "..." }` |

**Перед E2E:**

1. Реальная БД (не только InMemory) — PostgreSQL/SQL Server
2. Seed таблицы `Providers` (Google, Microsoft, …)
3. Настроить OAuth credentials + `CallbackUrl`
4. `Authentication:DeveloperMode=false` + рабочий Cross.Messaging (email/SMS)
5. `CrossIdentity:LicenseKey` — валидный тестовый ключ

---

## 6. CI/CD и инфраструктура

| # | Проверка | Статус |
|---|----------|--------|
| CI1 | `dotnet.yml` — build + test на PR в `master`/`dev` | ❌ NU1605: `Sample.Api` → `Microsoft.Extensions.Http` 8.0.0 vs 8.0.1 |
| CI2 | SonarCloud quality gate wait на PR | ⬜ |
| CI3 | `triage.yml` — automated PR triage | ✅ последний run ok |
| CI4 | GitVersion: `dev` теперь **не** release branch | 🟨 конфиг изменён; поведение при merge не проверено |
| CI5 | NuGet pack из `config.nuspec` — зависимости актуальны | ✅ синхронизирован с `.csproj` |

---

## 7. Документация и release notes

| # | Документ | Статус | Действие |
|---|----------|--------|----------|
| DOC1 | `README.md` | ✅ обновлён (licensing, структура) | — |
| DOC2 | `FLOWS.md` | ✅ | Актуализирован (18 flows, External OAuth) |
| DOC3 | `RefreshToken.md` | ✅ актуален | — |
| DOC4 | `config.nuspec` releaseNotes | 🟨 | licensing + OAuth; полный breaking list ⬜ |
| DOC5 | `LICENSE.md` | ✅ | обновлён (peshkov.biz) |
| DOC6 | Migration guide для потребителей | ⬜ | `docs/MIGRATION.md` не создан |
| DOC7 | CHANGELOG / GitHub Release | ⬜ | Перед релизом |

---

## 8. Миграция БД (для существующих инсталляций)

```sql
-- Примерные шаги (уточнить под вашу СУБД / EF migrations)

-- 1. Refresh tokens: новая колонка
ALTER TABLE RefreshTokens ADD AbsoluteExpiresAt datetime2 NOT NULL
  DEFAULT DATEADD(day, 30, CreatedAt);

-- 2. Users: убрать NormalizedEmail (если была)
-- UPDATE Users SET Email = NormalizedEmail WHERE Email IS NULL;
-- ALTER TABLE Users DROP COLUMN NormalizedEmail;

-- 3. Providers: seed OAuth-провайдеров
INSERT INTO Providers (Id, Name, IsEnabled) VALUES (...);

-- 4. UserExternalLogins: FK на Providers
```

| # | Проверка | Статус |
|---|----------|--------|
| M1 | EF migration создана и протестирована на staging | ⬜ |
| M2 | Backfill `AbsoluteExpiresAt` для существующих refresh-токенов | ⬜ |
| M3 | Seed `Providers` для OAuth | ⬜ |
| M4 | Rollback-план | ⬜ |

---

## 9. Блокеры и риски перед merge

| Приоритет | Проблема | Рекомендация |
|-----------|----------|--------------|
| **P0** | `license.ResetPassword.json` — `passwordKey` | ✅ Исправлено + `License_ResetPassword_FlowTests` |
| **P0** | Нет integration flow-тестов External OAuth | ✅ `License_ExternalOAuth_FlowTests` |
| **P1** | `config.nuspec` — устаревшие зависимости | ✅ Синхронизирован с `.csproj` |
| **P1** | `FLOWS.md` не соответствует коду | ✅ Обновлён |
| **P1** | Breaking change `collectResult` (1 поле) | 🟨 задокументировать в `docs/MIGRATION.md` (файл ⬜) |
| **P1** | OAuth state в `IMemoryCache` — не для multi-instance | ⬜ документировать / Redis |
| **P2** | Лицензия soft-fail в production | Продуктовое решение |
| **P2** | Нет EF migrations в репо | Добавить или документировать SQL |
| **P2** | `Sample.Api` — InMemory DB, нет OAuth config | Расширить пример |

---

## 10. Рекомендуемый порядок работ (release gate)

Выполнять по порядку; следующий шаг — после закрытия предыдущего (или явного решения «пропустить» с записью в PR).

- ✅ **1. P0-блокеры** — `license.ResetPassword.json`, `License_ResetPassword_FlowTests`, `License_ExternalOAuth_FlowTests`
- 🟨 **2. Тесты** — `dotnet test` 292/292 ✅ локально; coverage (opencover) ⬜; CI restore ❌
- 🟨 **3. Документация и пакет** — `config.nuspec`, `FLOWS.md` ✅; `docs/MIGRATION.md` + CHANGELOG ⬜
- ⬜ **4. БД** — EF migration (или SQL): `AbsoluteExpiresAt`, seed `Providers`, план rollback
- ⬜ **5. E2E Sample.Api** — все 10 операций `license/*` через Swagger/POST
- 🟨 **6. OAuth** — integration flow-тесты ✅ (mocked Google); реальный Google E2E ⬜
- ❌ **7. CI** — `dotnet.yml` падает на restore (`Sample.Api` NU1605); triage ✅
- 🟨 **8. Breaking changes** — описаны в плане; migration guide + согласование с потребителями ⬜
- ⬜ **9. Релиз** — merge в `master`, tag, NuGet publish

### Минимальный «go/no-go» чеклист

- ✅ Все 292 теста green (локально)
- ✅ P0 исправлены (`ResetPassword` JSON + flow-тесты, External OAuth flow-тесты)
- ⬜ 10 flow operations проверены через Sample.Api
- 🟨 OAuth initiate+callback (integration ✅ mocked; ручной Google E2E ⬜)
- ⬜ Refresh rotation + absolute expiry проверены вручную
- ⬜ DeveloperMode выключен в prod-конфиге
- ⬜ LicenseKey настроен (или осознанно soft-fail)
- 🟨 Breaking changes — в плане; `docs/MIGRATION.md` ⬜
- ✅ `config.nuspec` синхронизирован
- ❌ CI green на PR (`NU1605` в `Sample.Api`)
