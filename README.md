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
- **Каналы** — email и SMS (отправка кодов через Cross.Notification).
- **Формы** — декларативное описание полей и правил валидации (equal, requiredIf, atLeastOneRequired и др.).

## Требования

- .NET 8.0

## Структура решения

```
Cross.Identity.slnx
├── Cross.Identity               # Основная библиотека (flow, JWT, сущности, сервисы)
├── Cross.Identity.Tests     # Тесты (NUnit, Moq, FluentAssertions)
├── Cross.Notification           # Отправка уведомлений: Email (MailKit), SMS (net7.0/net8.0)
├── Cross.PepperVault            # Хранение секретов (pepper и др.)
├── Cross.PepperVault.*          # Провайдеры: Env, FileJson, AwsSecretsJson, AzureKv*, GcpSecretManagerJson, HcvKv2Json
├── _nuget/                      # config.nuspec для публикации пакета Cross.Identity
├── .github/workflows/           # CI (dotnet.yml)
├── RefreshToken.md              # Рекомендации по срокам жизни и ротации refresh-токенов
├── LICENSE.md
└── README.md
```

## Структура проекта Cross.Identity

```
Cross.Identity/
├── FlowExecutor.cs, IFlowExecutor.cs   # Точка входа: выполнение flow по (flow, operation) и входному словарю
├── IdentityConstants.cs, ClaimConstants.cs
├── Entities/                           # EF Core-сущности и конфигурации
│   ├── UserAccountEntity, AccessTokenEntity, RefreshTokenEntity
│   ├── EmailVerificationEntity, PhoneVerificationEntity
│   ├── ProviderEntity, UserExternalLoginEntity
│   └── *Configuration
├── Infrastructure/
│   └── IdentityContext.cs               # DbContext
├── Services/                            # Доменные сервисы
│   ├── UserService, IUserService
│   ├── CodeService, ICodeService
│   ├── JwtTokenService, IJwtTokenService
│   └── Crypto/                          # PasswordHasher (Argon2), PhoneNormalizer
├── Dtos/                                # FlowResult, ResolveBy, NotificationMessage
├── Options/                             # AuthenticationOptions
├── Helpers/                             # CodeGeneratorHelper, JwtKeys
├── Extensions/                          # ServiceCollectionExtensions (AddCrossIdentity, провайдеры дефиниций)
└── ProcessEngine/
    ├── Core/                            # Ядро движка
    │   ├── Bag, BagKey, BagMapExtensions
    │   ├── IStep, IStepFactory, StepRegistry, StepResult
    │   ├── ProcessLoader, ProcessBuilder, ProcessExecutor
    │   ├── IRequestInput, RequestInput
    │   ├── Enums/                       # ChannelEnum, FlowOperationEnum, StepStatusEnum
    │   └── Forms/                       # Схемы форм и валидация
    │       ├── FormSchema, FieldDescriptor, FieldTypeEnum
    │       ├── ValidatorFactories/      # UnifiedFormValidatorFactory, правила (Equal, RequiredIf, AtLeastOneRequired, …)
    │       └── Providers/               # IFormSchemaProvider, InMemoryFormSchemaProvider
    ├── Steps/                           # Реализации шагов (CollectForm, CreateUser, SendCode, Token, …)
    ├── Factories/                       # Фабрики шагов (CollectFormStepFactory, TokenStepFactory, …)
    └── Definitions/
        ├── Flows/                       # JSON-файлы сценариев (license.Register, game.Token, shop.auth, …)
        ├── Templates/                   # Шаблоны писем (register.*.html/txt, verify.*, reset.*)
        ├── Providers/                   # IProcessDefinitionProvider, Composite, FileSystem, EmbeddedResource
        └── Helpers/                     # JsonHelpers
```

## Использование

1. **Подключение** в приложении (ASP.NET Core):

```csharp
services.AddCrossIdentity(configuration);
// Регистрирует: IFlowExecutor, StepRegistry, все IStepFactory, UserService, CodeService, JwtTokenService,
// провайдер дефиниций (файлы + embedded), формы и т.д.
```

2. **Выполнение сценария** — в контроллере или минимальном API передайте тело запроса как словарь и вызовите:

```csharp
var result = await _flowExecutor.ExecuteAsync(
    input: requestBodyAsDictionary,
    flow: "license",           // например: license, game, shop, edoctors
    operation: FlowOperationEnum.Token,
    cancellationToken);
// result.Data — объект с полями, заданными шагом collectResult (например access_token, refresh_token).
```

3. **Дефиниции потоков** — JSON в `ProcessEngine/Definitions/Flows/` (и при необходимости из файловой системы). Имена файлов: `{flow}.{Operation}.json` (например `license.Token.json`, `game.Register.json`). Подробное описание всех flow и шагов — в [ProcessEngine/Definitions/Flows/README.md](Cross.Identity/ProcessEngine/Definitions/Flows/README.md).

## Зависимости (NuGet)

- Cross.ErrorHandlers
- Cross.Headers
- Konscious.Security.Cryptography.Argon2
- Magick.NET.Core
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Relational
- Microsoft.Extensions.Http
- PhoneNumbersCore
- System.IdentityModel.Tokens.Jwt
- ProjectReference: Cross.Notification, Cross.PepperVault

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
