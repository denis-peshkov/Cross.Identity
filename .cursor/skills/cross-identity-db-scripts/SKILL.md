---
name: cross-identity-db-scripts
description: >-
  Apply DbUp conventions (rule 102-backend-efcore) in Cross.Identity:
  Infrastructure/Scripts SqlServer/PostgreSQL/MySQL, auth schema, EF configs,
  docs/BREAKING. Use when changing Cross.Identity DDL or seed SQL.
---

# Cross.Identity — DbUp script locations

**DbUp rules** (layers, naming, append-only, idempotent new scripts, not EF Code First Migrations): `.cursor/rules/102-backend-efcore.mdc`.

This skill is only **where and how** those rules apply in this repository.

## When to use

- Changing `auth` DDL/seed under `Infrastructure/Scripts/`
- Syncing the same delta across **SqlServer**, **PostgreSQL**, and **MySQL**
- Updating `docs/BREAKING.md` with new script names for operators
- Aligning `Cross.Identity/Entities/*EntityConfiguration.cs` with scripts

## Layout in this repo

```text
Infrastructure/Scripts/
├── SqlServer/
├── PostgreSQL/
└── MySQL/
    ├── 1_PreDeployment/
    ├── 2_Initial/
    ├── 3_SeedLookup/       # lookup — preferred: table-var + MERGE (see Scripts/README.md)
    ├── 4_SeedData/         # initial data only — applied once; never re-run (later changes → 5_PostDeployment)
    └── 5_PostDeployment/
```

Reference copy notes: `Infrastructure/Scripts/README.md`. Runtime model: `IdentityContext` + entity configurations (provider-agnostic EF).

## Workflow (existing databases)

1. Find the **highest** `<Layer>_<nn>_…` number in the target layer for **each** provider folder.
2. Add a **new** idempotent script with the **next** number (delta only) — per `102-backend-efcore`.
3. Mirror intent in **all three** providers (syntax differs).
4. Update `2_Initial` for **greenfield** so new installs match the final model (do not rewrite shipped append-only scripts).
5. If the change is breaking for NuGet/host DBs, append `docs/BREAKING.md` with script names and order.

### New table when PreDeployment already has more than `1_00_*`

If `1_PreDeployment/` already has scripts **besides** `1_00_Predeployment.sql`, assume databases may already be initialized:

1. Add/update the table in **`2_Initial`** (greenfield).
2. Add a **new** `1_PreDeployment` script that creates the table if missing (existing DBs).
3. In that PreDeployment script, after a successful create, **insert the matching `2_Initial` script name** into `__MigrationsHistory` (see `102-backend-efcore` — Mark the matching `2_Initial` script as applied) so `2_Initial` does not fail with “table already exists”. Mirror the journal insert for PostgreSQL / MySQL journal tables/syntax.

### Example (Confirmed → Verified)

| Step | Script | Purpose |
|------|--------|---------|
| (existing, untouched) | `1_02_*EmailConfirmedUnique` | original filtered unique index |
| (existing, untouched) | `1_03_*PhoneNumberConfirmedUnique` | original filtered unique index |
| new | `1_07_*RenameConfirmedToVerified` | rename columns |
| new | `1_08_*EmailVerifiedUnique` | recreate email index on `EmailVerified` |
| new | `1_09_*PhoneNumberVerifiedUnique` | recreate phone index on `PhoneNumberVerified` |

## Checklist before finishing

- [ ] Followed `102-backend-efcore` (append-only + idempotent new scripts)
- [ ] No modified/deleted files under append-only layers except **new** numbered files
- [ ] New tables after init: both `2_Initial` **and** `1_PreDeployment`, plus `__MigrationsHistory` row for the `2_Initial` script name when PreDeployment creates the table on existing DBs
- [ ] SqlServer + PostgreSQL + MySQL in sync
- [ ] `docs/BREAKING.md` updated when operators must run new scripts
- [ ] EF `*EntityConfiguration.cs` matches final column/index names
