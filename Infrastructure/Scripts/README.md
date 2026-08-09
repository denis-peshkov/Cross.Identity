# Cross.Identity DbUp scripts (example)

Reference DDL for the `auth` schema. Scripts match the EF model (`IdentityContext`, `Entities/*EntityConfiguration.cs`).

The EF model is **provider-agnostic** (no SQL Server-specific column types). The host registers the database provider and applies the matching script folder.

> **This is a reference copy for the Cross.Identity repository.**
> In the `peshkov.biz` monorepo, the working scripts live in `compose/Identity/` and are linked into `Web.Api` via symlinks (`IdentitySymlinkToCompose.sh`). When changing the schema, update both locations or sync this copy manually.

## Providers

```text
Infrastructure/Scripts/
├── SqlServer/     # T-SQL (original)
├── PostgreSQL/    # PostgreSQL 13+ (uses gen_random_uuid() in seed; built-in since PG 13)
└── MySQL/         # MySQL 8+ (Pomelo: schema → database `auth`)
```

PostgreSQL scripts require **PostgreSQL 13 or later**: `4_SeedData/4_01_auth_Providers.sql` calls `gen_random_uuid()` (available in core since 13; on older versions enable `pgcrypto` or replace UUID generation).

Each provider folder uses the same DbUp layer layout:

```text
<Provider>/
├── 1_PreDeployment/   # incremental migrations for already deployed databases
├── 2_Initial/         # create auth schema/database and tables
├── 3_SeedLookup/      # lookup tables (idempotent data seeding migrations)
├── 4_SeedData/        # initial data (data seeding)
└── 5_PostDeployment/  # data/structure updates (if needed after main migration)
```

File naming:

```text
<FolderNumber>_<Layer>_<EntityName>[_<comment_if_required>]
```

Examples:

- `2_Initial/2_00_auth.sql` — create schema / database `auth`
- `2_Initial/2_01_auth_UsersAccounts.sql` — user accounts table
- `2_Initial/2_01_auth_ExternalLoginStates.sql` — OAuth state (multi-instance)
- `4_SeedData/4_01_auth_Providers.sql` — OAuth provider seed

`SqlServer/4_SeedData/4_01_AspNetUsers.sql` is SQL Server–only sample data (ASP.NET Identity); it is not ported.

## Application order (DbUp)

1. `1_PreDeployment`
2. `2_Initial`
3. `3_SeedLookup`
4. `4_SeedData`
5. `5_PostDeployment`

Point DbUp at the folder for your provider, e.g. `Infrastructure/Scripts/SqlServer`.

## Host registration (EF Core)

```csharp
// SQL Server
options.UseSqlServer(connectionString);

// PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL)
options.UseNpgsql(connectionString);

// MySQL (Pomelo.EntityFrameworkCore.MySql)
options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
```

`IdentityContext` attaches `ConcurrencyStampInterceptor` automatically. `AddCrossIdentity` does not register `DbContext` or database providers — the host owns that.

## Type mapping notes

| Concept | SQL Server | PostgreSQL | MySQL |
|---------|------------|------------|-------|
| Guid | `UNIQUEIDENTIFIER` | `uuid` | `CHAR(36)` (Pomelo default) |
| DateTime | `DATETIME2` | `timestamp` | `DATETIME(6)` |
| DateTimeOffset | `DATETIMEOFFSET` | `timestamptz` | `DATETIME(6)` (UTC) |
| bool | `BIT` | `boolean` | `TINYINT(1)` |
| byte[] hash | `BINARY(32)` | `bytea` | `BINARY(32)` |
| Identity | `IDENTITY` | `GENERATED … AS IDENTITY` | `AUTO_INCREMENT` |
| Unique nullable | filtered unique index | unique (NULLs distinct) | unique (multiple NULLs OK) |

## EF mapping

| Table | Script | Entity |
|---------|--------|--------|
| `auth.UsersAccounts` | `2_01_auth_UsersAccounts.sql` | `UserAccountEntity` |
| `auth.Providers` | `2_01_auth_Providers.sql` | `ProviderEntity` |
| `auth.UsersExternalLogins` | `2_01_auth_UsersExternalLogins.sql` | `UserExternalLoginEntity` |
| `auth.ExternalLoginStates` | `2_01_auth_ExternalLoginStates.sql` | `ExternalLoginStateEntity` |
| `auth.AccessTokens` | `2_01_auth_AccessTokens.sql` | `AccessTokenEntity` |
| `auth.RefreshTokens` | `2_01_auth_RefreshTokens.sql` | `RefreshTokenEntity` |
| `auth.EmailVerifications` | `2_01_auth_EmailVerifications.sql` | `EmailVerificationEntity` |
| `auth.PhoneVerifications` | `2_01_auth_PhoneVerifications.sql` | `PhoneVerificationEntity` |
| `auth.UsersCommunicationEndpoints` | `2_01_auth_UsersCommunicationEndpoints.sql` | `UserCommunicationEndpointEntity` |

When changing the schema, update the EF configuration and the corresponding SQL for **all three** providers.
