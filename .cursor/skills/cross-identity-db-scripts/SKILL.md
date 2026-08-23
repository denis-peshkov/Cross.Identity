---
name: cross-identity-db-scripts
description: >-
  Cross.Identity DbUp SQL under Infrastructure/Scripts. Use when changing auth
  schema, adding PreDeployment migrations, syncing SqlServer/PostgreSQL/MySQL,
  or updating docs/BREAKING for DDL.
---

# Cross.Identity DbUp scripts

Reference: `Infrastructure/Scripts/README.md`. EF model lives in `Cross.Identity/Entities/*EntityConfiguration.cs`.

## When to use

- Any change to `auth` schema for **already deployed** databases (`1_PreDeployment`)
- Greenfield table DDL (`2_Initial`) or seed scripts
- BREAKING notes that mention migration file names

## PreDeployment immutability (mandatory)

**`*/1_PreDeployment` scripts are never edited, renamed, or deleted** once they exist in the repo (including on a release branch). DbUp tracks applied scripts by **file name**; changing an old file does not re-run it on databases that already applied the original version.

When schema must change on existing databases:

1. Find the **highest** `<Layer>_<nn>_…` number in `1_PreDeployment/` for **each** provider folder (`SqlServer`, `PostgreSQL`, `MySQL`).
2. Add a **new** script with the **next** number (`1_07`, then `1_08`, …) and the **delta only** (rename column, drop/recreate index, add column, etc.).
3. Keep the same base name pattern: `<FolderNumber>_<Layer>_<EntityName>[_<comment>]`.
4. Mirror the change in **all three** provider folders (syntax differs; intent must match).
5. Update `2_Initial` (and seed if needed) for **greenfield** installs — that layer may reflect the final state; do **not** “fix history” by rewriting old PreDeployment files.

**Never:**

- Rename `1_02_*` → `1_02_*` with different SQL
- Patch column names inside an already-shipped PreDeployment script
- Skip sequence numbers to force sort order (`1_011`, `1_11` while `1_07` is free)
- Add “run before 1_02” hacks — use the next number after the latest PreDeployment script

**Example (Confirmed → Verified):**

| Step | Script | Purpose |
|------|--------|---------|
| (existing, untouched) | `1_02_*EmailConfirmedUnique` | original filtered unique index |
| (existing, untouched) | `1_03_*PhoneNumberConfirmedUnique` | original filtered unique index |
| new | `1_07_*RenameConfirmedToVerified` | rename columns |
| new | `1_08_*EmailVerifiedUnique` | recreate email index on `EmailVerified` |
| new | `1_09_*PhoneNumberVerifiedUnique` | recreate phone index on `PhoneNumberVerified` |

## Other layers

| Layer | May edit in place? | Notes |
|-------|-------------------|--------|
| `2_Initial` | Yes (greenfield) | Must match EF; no history on new installs |
| `3_SeedLookup`, `4_SeedData`, `5_PostDeployment` | Prefer idempotent new scripts | Follow team conventions for seeds |

## Checklist before finishing

- [ ] No modified/deleted files under `*/1_PreDeployment/` except **new** numbered files
- [ ] SqlServer + PostgreSQL + MySQL folders in sync
- [ ] `docs/BREAKING.md` lists new script names and order for operators
- [ ] EF `*EntityConfiguration.cs` matches final column/index names
