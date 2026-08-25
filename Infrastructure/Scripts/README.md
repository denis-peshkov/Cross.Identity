# Cross.Identity DbUp scripts (example)

Reference DDL for the `auth` schema. Scripts match the EF model (`IdentityContext`, `Entities/*EntityConfiguration.cs`).

The EF model is **provider-agnostic** (no SQL Server-specific column types). The host registers the database provider and applies the matching script folder.

> **This is a reference copy for the Cross.Identity repository.**
> In the `peshkov.biz` monorepo, the working scripts live in `compose/Identity/` and are linked into `Web.Api` via symlinks (`IdentitySymlinkToCompose.sh`). When changing the schema, update both locations or sync this copy manually.

**EF Core** is used for the runtime model (`DbContext`, entities, configurations).
**Schema and seed evolution** use **DbUp numbered SQL scripts**, not EF Code First Migrations (`Add-Migration` / `__EFMigrationsHistory` as the primary path).

| Approach | Role here |
|----------|-----------|
| EF Core | Mapping, queries, `SaveChanges`, optional greenfield model alignment |
| DbUp SQL folders | Apply DDL/DML to existing and new databases in a fixed layer order |

Agent conventions (same rules for Cursor): `.cursor/rules/102-backend-efcore.mdc`. Repo workflow: `.cursor/skills/db-scripts/SKILL.md`.

## Providers

```text
Infrastructure/Scripts/
├── SqlServer/     # T-SQL (original)
├── PostgreSQL/    # PostgreSQL 13+ (uses gen_random_uuid() in seed; built-in since PG 13)
└── MySQL/         # MySQL 8+ (Pomelo: schema → database `auth`)
```

PostgreSQL scripts require **PostgreSQL 13 or later**: `4_SeedData/4_01_auth_Providers.sql` calls `gen_random_uuid()` (available in core since 13; on older versions enable `pgcrypto` or replace UUID generation).

## Layer layout

Each provider folder uses the same DbUp layer layout:

```text
<Provider>/
├── 1_PreDeployment/   # incremental migrations for already deployed databases
├── 2_Initial/         # create schema/database and tables (greenfield)
├── 3_SeedLookup/      # lookup seed — preferred: table-var + MERGE upsert (edit VALUES area)
├── 4_SeedData/        # initial data — applied once per script per database; never re-run after that
└── 5_PostDeployment/  # follow-up data/structure updates (use this for later data changes)
```

Application order: `1` → `2` → `3` → `4` → `5`.

Point DbUp at the folder for your provider, e.g. `Infrastructure/Scripts/SqlServer`.

### `3_SeedLookup` — MERGE upsert (preferred)

Prefer a **table variable + `MERGE`**: put the desired lookup rows in a `VALUES` block (edit only between the markers), then upsert into the real table. Matched rows update, missing rows insert, rows absent from the source delete (`WHEN NOT MATCHED BY SOURCE`). **Intentional:** VALUES is the full managed set for that lookup (or the slice keyed in `ON` / target scope such as `SystemId`); do not leave orphan lookup rows. Do not flag delete-not-in-source as a bug.

SQL Server pattern (shortened):

```sql
BEGIN TRANSACTION

    DECLARE @Permissions AS TABLE
    (
        [SystemId]    INT           NOT NULL,
        [Code]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(300) NULL
    );

    INSERT INTO @Permissions ([SystemId], [Code], [Description])
    SELECT DISTINCT * FROM (VALUES
-- BEGIN OF AREA FOR EDIT >>>

           (1, 'Catalog.Products.Read', 'Ability to view Products')
         , (1, 'Catalog.Products.Write', 'Ability to modify Products')
         , (1, 'Access.Permissions.Read', 'Ability to view Permissions')

-- <<< END OF AREA FOR EDIT

    ) AS [src] ([SystemId], [Code], [Description]);

    -- upsert data
    MERGE [pol].[Permissions] [target]
    USING (SELECT DISTINCT [SystemId], [Code], [Description] FROM @Permissions) [source]
            ON   [target].[SystemId] = [source].[SystemId]
             AND [target].[Code]     = [source].[Code]
        WHEN MATCHED THEN
            UPDATE SET [target].[Description] = [source].[Description]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([SystemId], [Code], [Description])
            VALUES ([source].[SystemId], [source].[Code], [source].[Description])
        WHEN NOT MATCHED BY SOURCE THEN
            DELETE;

COMMIT TRANSACTION
GO
```

On PostgreSQL / MySQL use the provider equivalent (`INSERT … ON CONFLICT` / `INSERT … ON DUPLICATE KEY` plus an explicit delete of rows not in the desired set), keeping the same “edit only the VALUES area” idea.

### `4_SeedData` — one-shot initial data

`4_SeedData` is **initial** seeding only. DbUp records each script by file name and **does not execute it again** after a successful apply on that database. Do not rely on re-running SeedData for updates. Ongoing data/structure fixes after the first install go in **`5_PostDeployment`** (new append-only scripts), not by re-invoking or rewriting SeedData.

`SqlServer/4_SeedData/4_01_AspNetUsers.sql` is SQL Server–only sample data (ASP.NET Identity); it is not ported.

## Naming conventions

- **Script file naming** — `<FolderNumber>_<Layer>_<EntityName>[_<comment_if_required>]`

| Part | Meaning |
|------|---------|
| `FolderNumber` | DbUp folder id (`1` … `5` → `1_PreDeployment` … `5_PostDeployment`) |
| `Layer` | **Dependency stage** within that folder (`00`, `01`, `02`, …). **Not** a per-file sequence. Several scripts may share the same `Layer` when they are **independent**. Bump `Layer` only when a script **depends on** work that must already have run in a **previous** stage. |
| `EntityName` / optional `_comment` | Short purpose / entity hint |

**Canonical bootstrap file** (all providers): exactly `1_00_Predeployment.sql` (casing as written — not `predeployment` / `PreDeployment`). Folder name stays `1_PreDeployment/`.

Examples:

```text
1_PreDeployment/1_00_Predeployment.sql
2_Initial/2_00_auth.sql
2_Initial/2_01_auth_UsersAccounts.sql
2_Initial/2_01_auth_ExternalLoginStates.sql   # same Layer 01 — independent
2_Initial/2_02_auth_ExternalLoginStates.sql   # Layer 02 — FK depends on prior stage
4_SeedData/4_01_auth_Providers.sql
```

- **Choosing `Layer`:**
  - **`2_Initial`** (and seed folders `3_*` / `4_*` when adding peer scripts): `Layer` is a **dependency stage** — several independent scripts may share the same `nn`; bump only when the new script depends on a prior stage.
  - **`1_PreDeployment`** and **`5_PostDeployment`** (append-only evolution): every **new** script file must use **`max(existing Layer in that folder) + 1`**. **Never gap-fill** a missing lower number after a higher one exists (e.g. if `1_07` is present, do **not** add `1_05` even if unused). Same after ship.
- **Comments** — optional `_comment` suffix when the purpose is unclear from the entity name alone.

## Append-only layers (mandatory)

Never edit, rename, or delete already-shipped scripts under:

- `*/1_PreDeployment/`
- `*/5_PostDeployment/`

DbUp tracks applied scripts by **file name**; changing an old file does not re-run it on databases that already applied it. Append a **new** script with the **delta only**.

**Numbering in `1_PreDeployment` / `5_PostDeployment`:** always **highest `Layer` + 1**. **No gap-filling.**

**Never:**

- Patch an old script to “fix” a deploy or to make it idempotent after shipping
- Gap-fill a lower `Layer` in `1_PreDeployment` / `5_PostDeployment` when a higher `Layer` already exists (shipped or not)
- Insert a `2_Initial` script that must run before an already-shipped dependency stage — bump `Layer` instead

For changes on existing databases, add a **new** script in **SqlServer**, **PostgreSQL**, and **MySQL** (same `FolderNumber` / `Layer` / intent). Prefer **idempotent** new scripts. Put only the delta in that file. Update `2_Initial` for greenfield installs separately.

## Prefer idempotent new scripts

New files in the append-only layers should be **idempotent where practical** (partial apply / re-run / restored DB):

- Schema: create index/constraint only if missing; add column only if missing; backfill with `WHERE col IS NULL` (or equivalent).
- Seeds: for **`3_SeedLookup`**, prefer table-var + `MERGE` (upsert + delete not in source); otherwise insert only when missing (`WHERE NOT EXISTS` / `ON CONFLICT DO NOTHING` / `INSERT IGNORE` — provider-appropriate).

Destructive renames/drops may remain one-way — document for operators and keep the delta minimal.

## Greenfield (`2_Initial`)

`2_Initial` **may be edited in place** so new installs match the current EF model. Do **not** rewrite history in append-only layers to match greenfield — append PreDeployment/PostDeployment scripts for existing databases instead.

## New table after the database is already initialized

**Heuristic:** treat the database / product as already initialized when `1_PreDeployment/` contains scripts **other than** the bootstrap `1_00_Predeployment.sql` (or equivalent). Then a **new table** must be added in **both** places:

1. **`2_Initial`** — greenfield create (so new installs get the table).
2. **`1_PreDeployment`** — new numbered script that creates the same table on **existing** databases (idempotent: create only if missing).

If you only update `2_Initial`, already-deployed DBs never get the table. If you only add PreDeployment, greenfield may create the table twice (PreDeployment then `2_Initial`) and fail with “object already exists”.

### Mark the matching `2_Initial` script as applied

After creating the table in the PreDeployment script on an existing DB, **journal the corresponding `2_Initial` file name** in `__MigrationsHistory` (or the host’s DbUp journal table) so layer `2_Initial` **skips** that script and does not try to create the table again.

SQL Server example (adjust schema/journal/script name to match the host):

```sql
-- … create auth.ExternalLoginStates if not exists (idempotent) …

IF OBJECT_ID(N'auth.ExternalLoginStates') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM [dbo].[__MigrationsHistory]
    WHERE [ScriptName] = N'2_01_auth_ExternalLoginStates.sql'
)
BEGIN
    INSERT INTO [dbo].[__MigrationsHistory] ([ScriptName], [Applied])
    VALUES (N'2_01_auth_ExternalLoginStates.sql', SYSDATETIME());
END
GO
```

Notes:

- `[ScriptName]` must match **exactly** what DbUp stores for that `2_Initial` script (often the file name; confirm against existing journal rows).
- Use the provider’s equivalent checks/inserts on PostgreSQL / MySQL.
- Prefer doing create + journal insert in the **same** PreDeployment script so a partial failure does not leave the table without a journal row (or the reverse).

## Host registration (EF Core)

```csharp
// SQL Server
options.UseSqlServer(connectionString);

// PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL)
options.UseNpgsql(connectionString);

// MySQL (Pomelo.EntityFrameworkCore.MySql)
options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
```

`IdentityContext` rotates `ConcurrencyStamp` in `SaveChanges` (no interceptor registration required). `AddCrossIdentity` does not register `DbContext` or database providers — the host owns that.

## Type mapping notes

| Concept | SQL Server | PostgreSQL | MySQL |
|---------|------------|------------|-------|
| Guid | `UNIQUEIDENTIFIER` | `uuid` | `CHAR(36)` (Pomelo default) |
| DateTime | `DATETIME2` | `timestamp` | `DATETIME(6)` |
| DateTimeOffset | `DATETIMEOFFSET` | `timestamptz` | `DATETIME(6)` (UTC) |
| bool | `BIT` | `boolean` | `TINYINT(1)` |
| byte[] hash | `BINARY(32)` | `bytea` | `BINARY(32)` |
| Identity | `IDENTITY` | `GENERATED … AS IDENTITY` | `AUTO_INCREMENT` |
| Unique nullable | filtered unique index | unique WHERE verified | expression unique index |

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
| `auth.Audits` | `2_01_auth_Audits.sql` | `AuditEntity` |

When changing the schema, update the EF configuration and the corresponding SQL for **all three** providers.
