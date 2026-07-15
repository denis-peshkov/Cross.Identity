# Cross.Identity DbUp scripts (example)

A copy of SQL scripts for the `auth` schema and related seed data. Scripts match the EF model (`IdentityContext`, `Entities/*EntityConfiguration.cs`).

> **This is a reference copy for the Cross.Identity repository.**
> In the `peshkov.biz` monorepo, the working scripts live in `compose/Identity/` and are linked into `Web.Api` via symlinks (`IdentitySymlinkToCompose.sh`). When changing the schema, update both locations or sync this copy manually.

## Structure

```text
Infrastructure/Scripts/
├── 1_PreDeployment/   # incremental migrations for already deployed databases
├── 2_Initial/         # create auth schema and tables
├── 3_SeedLookup/      # lookup tables (idempotent data seeding migrations)
├── 4_SeedData/        # initial data (data seeding)
└── 5_PostDeployment/  # data/structure updates (if needed after main migration)
```

File naming:

```text
<FolderNumber>_<Layer>_<EntityName>[_<comment_if_required>]
```

Examples:

- `2_Initial/2_00_auth.sql` — `CREATE SCHEMA [auth]`
- `2_Initial/2_01_auth_UsersAccounts.sql` — user accounts table
- `2_Initial/2_01_auth_ExternalLoginStates.sql` — OAuth state (multi-instance)
- `4_SeedData/4_01_auth_Providers.sql` — OAuth provider seed

## Application order (DbUp)

1. `1_PreDeployment`
2. `2_Initial`
3. `3_SeedLookup`
4. `4_SeedData`
5. `5_PostDeployment`

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

When changing the schema, update the EF configuration and the corresponding SQL.
