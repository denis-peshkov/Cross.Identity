[![License](https://img.shields.io/github/license/denis-peshkov/Cross.Identity)](LICENSE)
[![GitHub Release Date](https://img.shields.io/github/release-date/denis-peshkov/Cross.Identity?label=released)](https://github.com/denis-peshkov/Cross.Identity/releases)
[![NuGetVersion](https://img.shields.io/nuget/v/Cross.Identity.svg)](https://nuget.org/packages/Cross.Identity/)
[![NugetDownloads](https://img.shields.io/nuget/dt/Cross.Identity.svg)](https://nuget.org/packages/Cross.Identity/)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Cross.Identity&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Cross.Identity)
[![issues](https://img.shields.io/github/issues/denis-peshkov/Cross.Identity)](https://github.com/denis-peshkov/Cross.Identity/issues)
[![.NET PR](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml/badge.svg?event=pull_request)](https://github.com/denis-peshkov/Cross.Identity/actions/workflows/dotnet.yml)

![Size](https://img.shields.io/github/repo-size/denis-peshkov/Cross.Identity)
[![GitHub contributors](https://img.shields.io/github/contributors/denis-peshkov/Cross.Identity)](https://github.com/denis-peshkov/Cross.Identity/contributors)
[![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/denis-peshkov/Cross.Identity/latest?label=new+commits)](https://github.com/denis-peshkov/Cross.Identity/commits/master)
![Activity](https://img.shields.io/github/commit-activity/w/denis-peshkov/Cross.Identity)
![Activity](https://img.shields.io/github/commit-activity/m/denis-peshkov/Cross.Identity)
![Activity](https://img.shields.io/github/commit-activity/y/denis-peshkov/Cross.Identity)

# Cross.Identity

Библиотека идентификации и аутентификации для .NET: настраиваемые сценарии (регистрация, вход, восстановление пароля, выдача и обновление токенов), JWT, Argon2, верификация по email/SMS, процессный движок с JSON-описанием потоков.

## Возможности

- **Process Engine** — выполнение сценариев (flow) по JSON-дефинициям с последовательными шагами (steps).
- **Потоки** — регистрация, вход по паролю/коду, forgot password, token, refresh token, получение пользователя, запрос и проверка кодов (email/SMS).
- **JWT** — выпуск и валидация access/refresh токенов, настраиваемые claims и время жизни.
- **Безопасность** — хеширование паролей (Argon2), одноразовые коды, нормализация телефонов.
- **Каналы** — email и SMS (отправка кодов через Cross.Messaging).
- **Формы** — декларативное описание полей и правил валидации (equal, requiredIf, atLeastOneRequired и др.).
- **Лицензирование (JWT)** — проверка ключа Peshkov при первом вызове flow; без ключа в dev/test работа продолжается с предупреждением в логах.

## Требования

- .NET 8.0

## Структура репозитория

```
Cross.Identity.slnx
├── Cross.Identity/                  # NuGet-библиотека
│   ├── FlowExecutor.cs, IFlowExecutor.cs
│   ├── Entities/, Infrastructure/     # EF Core (пользователи, токены, верификации, external login)
│   ├── Services/                    # User, Code, JwtToken; Crypto/; ExternalOAuth/
│   ├── Licensing/                   # JWT-лицензия Peshkov (Accessor, Validator, ProductInfo)
│   ├── Options/                     # AuthenticationOptions, IdentityServiceConfiguration
│   ├── Extensions/, Helpers/, Dtos/, Enums/
│   ├── ProcessEngine/
│   │   ├── Core/                    # Bag, StepRegistry, ProcessLoader, Forms/валидация
│   │   ├── Steps/, Factories/       # Шаги и их DI-фабрики
│   │   └── Definitions/           # Flows/*.json, Templates/, Providers/
│   ├── FLOWS.md                     # Описание flow и шагов
│   └── config.nuspec
├── Cross.Identity.Tests/            # NUnit (unit + integration)
├── Sample.Api/                      # Пример минимального API (ASP.NET Core)
├── .cursor/triage/docs/               # Отчёты automated triage (.data/, ci-report-*.md)
├── .github/workflows/               # dotnet.yml, triage.yml
├── RefreshToken.md
├── LICENSE.md
└── README.md
```

## Использование

1. **Подключение** в приложении (ASP.NET Core):

```csharp
services.AddCrossIdentity(configuration);
// Регистрирует: IFlowExecutor, StepRegistry, все IStepFactory, UserService, CodeService, JwtTokenService,
// LicenseAccessor, LicenseValidator, ILicenseProductInfo, провайдер дефиниций (файлы + embedded), формы и т.д.
```

Ключ лицензии (опционально) — секция `CrossIdentity` в конфигурации или переменная окружения `CrossIdentity__LicenseKey`:

```json
{
  "CrossIdentity": {
    "LicenseKey": "<license key here>"
  }
}
```

Проверка выполняется автоматически при **первом** вызове `IFlowExecutor.ExecuteAsync` — дополнительный код не нужен. Ключи: [peshkov.biz](https://peshkov.biz).

Поведение:

| Сценарий | Результат |
|----------|-----------|
| Ключ не задан | `LogCritical`, flow выполняется (dev/test) |
| Невалидный JWT | `LogError`, flow выполняется |
| Просроченный / неверный тип продукта | `LogError` + `LogCritical`, flow выполняется |
| Валидный ключ | `LogInformation` с edition и датой истечения |

2. **Выполнение сценария** — в контроллере или минимальном API передайте тело запроса как словарь и вызовите:

```csharp
var result = await _flowExecutor.ExecuteAsync(
    input: requestBodyAsDictionary,
    flow: "license",           // например: license, game, shop, edoctors
    operation: FlowOperationEnum.Token,
    cancellationToken);
// result.Data — словарь полей из шага collectResult (например access_token, refresh_token, LastCode).
```

3. **Дефиниции потоков** — JSON в `ProcessEngine/Definitions/Flows/` (и при необходимости из файловой системы). Имена файлов: `{flow}.{Operation}.json` (например `license.Token.json`, `game.Register.json`). Подробное описание flow и шагов — в [FLOWS.md](Cross.Identity/FLOWS.md).

## Зависимости (NuGet)

- Cross.ErrorHandlers
- Cross.Headers
- Cross.Messaging
- Cross.PepperVault
- Konscious.Security.Cryptography.Argon2
- Microsoft.EntityFrameworkCore (+ InMemory, Relational)
- Microsoft.Extensions.Caching.Memory
- Microsoft.Extensions.Http
- Microsoft.IdentityModel.JsonWebTokens
- PhoneNumbersCore

## Сборка и тесты

```bash
dotnet build
dotnet test
```

## Тесты

### Категории (NUnit)

Константы — `Cross.Identity.Tests.Common.TestCategory`, атрибуты: `[Category(TestCategory.UNIT)]`, `[Category(TestCategory.INTEGRATION)]`, `[Category(TestCategory.FUNCTIONAL)]`.

| Категория | Назначение |
|-----------|------------|
| **UNIT** | Моки, один компонент, без InMemory EF |
| **INTEGRATION** | `EFTestsBase` (InMemory EF + реальные сервисы), `RunFlowCommandHandlerTestsBase` / `Identity/FlowTests` (сквозной process engine) |
| **FUNCTIONAL** | Зарезервировано (E2E / TestServer / внешние зависимости), пока не используется |

Примеры запуска:

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

### Именование методов

Соглашение **Given_When_Then**:

- **Given** — контекст/предусловия.
- **When** — действие.
- **Then** — ожидаемый результат.

Пример: `ExistingUser_RequestCode_SendsCodeAndReturnsLastCode`.

Структура: `Cross.Identity.Tests/Identity/` — FlowTests (integration), StepTests и StepFactoryTests (unit); `Services/` — unit или integration в зависимости от базового класса (`EFTestsBase` → integration).

## Дополнительно

- [RefreshToken.md](RefreshToken.md) — рекомендации по срокам жизни access/refresh токенов и ротации.
- [LICENSE.md](LICENSE.md) — лицензия.

## ToDo

- ~~[x] Организовать переход с System.IdentityModel.Tokens.Jwt на Microsoft.IdentityModel.JsonWebTokens~~
