# DbUp-скрипты Cross.Identity (пример)

Копия SQL-скриптов для схемы `auth` и связанных seed-данных. Скрипты соответствуют EF-модели (`IdentityContext`, `Entities/*EntityConfiguration.cs`).

> **Это справочная копия для репозитория Cross.Identity.**
> В монорепозитории `peshkov.biz` рабочие скрипты лежат в `compose/Identity/` и подключаются в `Web.Api` через симлинки (`IdentitySymlinkToCompose.sh`). При изменении схемы обновляйте оба места или синхронизируйте копию вручную.

## Структура

```text
Infrastructure/Scripts/
├── 1_PreDeployment/   # инкрементальные миграции для уже развёрнутых БД
├── 2_Initial/         # создание схемы auth и таблиц
├── 3_SeedLookup/      # лукап таблицы (засеивание данными иденпотентные миграции)
├── 4_SeedData/        # начальные данные (засевание данными)
└── 5_PostDeployment/  # обновление данных/структуры (если необходимо после основной миграции)
```

Именование файлов:

```text
<FolderNumber>_<Layer>_<EntityName>[_<comment_if_required>]
```

Примеры:

- `2_Initial/2_00_auth.sql` — `CREATE SCHEMA [auth]`
- `2_Initial/2_01_auth_UsersAccounts.sql` — таблица учётных записей
- `2_Initial/2_01_auth_ExternalLoginStates.sql` — OAuth state (multi-instance)
- `4_SeedData/4_01_auth_Providers.sql` — seed OAuth-провайдеров

## Порядок применения (DbUp)

1. `1_PreDeployment`
2. `2_Initial`
3. `3_SeedLookup`
4. `4_SeedData`
5. `5_PostDeployment`

## Соответствие EF

| Таблица | Скрипт | Entity |
|---------|--------|--------|
| `auth.UsersAccounts` | `2_01_auth_UsersAccounts.sql` | `UserAccountEntity` |
| `auth.Providers` | `2_01_auth_Providers.sql` | `ProviderEntity` |
| `auth.UsersExternalLogins` | `2_01_auth_UsersExternalLogins.sql` | `UserExternalLoginEntity` |
| `auth.ExternalLoginStates` | `2_01_auth_ExternalLoginStates.sql` | `ExternalLoginStateEntity` |
| `auth.AccessTokens` | `2_01_auth_AccessTokens.sql` | `AccessTokenEntity` |
| `auth.RefreshTokens` | `2_01_auth_RefreshTokens.sql` | `RefreshTokenEntity` |
| `auth.EmailVerifications` | `2_01_auth_EmailVerifications.sql` | `EmailVerificationEntity` |
| `auth.PhoneVerifications` | `2_01_auth_PhoneVerifications.sql` | `PhoneVerificationEntity` |

При изменении схемы обновляйте EF configuration и соответствующий SQL.
