# Migration guide (NuGet consumers)

Breaking changes for **Cross.Identity**, grouped by **from → to** package version. Apply every section in order when skipping releases (e.g. `1.4 → 1.7` = all three below).

| Upgrade path | Section |
|--------------|---------|
| `≤ 1.4.x` → `1.5.0+` | [From ≤1.4.x to 1.5.0](#from-14x-to-150) |
| `1.5.x` → `1.6.0+` | [From 1.5.x to 1.6.0](#from-15x-to-160) |
| `1.6.x` → `1.7.0+` | [From 1.6.x to 1.7.0](#from-16x-to-170) |

Flow contracts: [`Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md).

DB scripts: [`Infrastructure/Scripts/README.md`](../Infrastructure/Scripts/README.md).

Release notes mirror: [`Cross.Identity/config.nuspec`](../Cross.Identity/config.nuspec).

When shipping a new breaking change: add a **From X.Y.Z to A.B.C** section (newest first), sync `config.nuspec` `releaseNotes`, prefix the **PR title** with `BREAKING:`.

---

## From ≤1.4.x to 1.5.0

### Built-in flow rename: `license` → `main`

| Area | Was (≤1.4) | Now (1.5+) |
|------|------------|------------|
| Flow id | `ExecuteAsync(..., "license", operation, ...)` | `ExecuteAsync(..., "main", operation, ...)` |
| Definition files | `license.{Operation}.json` | `main.{Operation}.json` |
| Sample/API path | `/api/identity/license/{Operation}` | `/api/identity/main/{Operation}` |

Removed demo flows: `game.*`, `shop.*`, `edoctors.*`.

**Action:** rename flow id, override files, and client routes to `main`.

### Operation rename: `GetUser` → `GetUserId`

| Area | Was | Now |
|------|-----|-----|
| Enum / route | `GetUser` | `GetUserId` |
| Flow file | `license.GetUser.json` / `main.GetUser.json` | `main.GetUserId.json` |

**Action:** update clients, custom overrides, and hardcoded operation names.

### `collectResult` with a single field

| Was | Now |
|-----|-----|
| bare scalar (`"abc"`) | always an object (`{ "fieldName": "abc" }`) |

**Action:** adjust client deserialization.

### Public executor surface

`FlowExecutor` is **internal**. Use **`IFlowExecutor`** only.

### Licensing

JWT license validation runs on the first `IFlowExecutor.ExecuteAsync` call.

- `CrossIdentity:LicenseKey` in configuration, or
- `CrossIdentity__LicenseKey` environment variable

### Data model / schema (host-owned DB)

Reference DDL: `Infrastructure/Scripts/{SqlServer,PostgreSQL,MySQL}/`.

- Prefer `Email` over removed `NormalizedEmail`.
- `RefreshToken.AbsoluteExpiresAt` — add column and backfill.
- External logins: `UserExternalLogin` + provider seed (Google, Microsoft, GitHub, Apple).

### Dependencies

- JWT: `Microsoft.IdentityModel.JsonWebTokens`
- Messaging / pepper: `Cross.Messaging`, `Cross.PepperVault` (NuGet)
- Align `Cross.ErrorHandlers` / `Cross.Headers` if the app pins older versions

---

## From 1.5.x to 1.6.0

### Removed operation: `TokenByCode`

OTP exchange is handled by **`Token`** (`main.Token`) with `{ Email|Phone, Code }` (same payload shape as before).

| Area | Was (1.5) | Now (1.6+) |
|------|-----------|------------|
| Operation / route | `TokenByCode` | `Token` |
| Flow file | `main.TokenByCode.json` | removed — use `main.Token.json` |

**Action:** call `main` / `Token` instead of `TokenByCode`; drop custom `TokenByCode` overrides.

---

## From 1.6.x to 1.7.0

### External OAuth step type rename

Flow operations stay `ExternalLogin` / `ExternalLoginCallback`. Step **type** names in JSON and DI changed:

| Was (1.6) | Now (1.7+) |
|-----------|------------|
| `InitiateExternalLogin` | `ExternalLoginInitiate` |
| `CompleteExternalLogin` | `ExternalLoginComplete` |

**Action:** update custom flow overrides that reference the old step types; stock `main.ExternalLogin*.json` already use the new names.
