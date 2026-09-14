# Changelog

All notable changes to **Cross.Identity** are documented in this file.

Format: newest release first. English. Consumer-facing breaking changes also live in [`docs/BREAKING.md`](BREAKING.md).
Dates follow [GitHub Releases](https://github.com/denis-peshkov/Cross.Identity/releases) publish time (UTC).

---

## v2.5.1 — 14 Sep 2026

### CI / release process

- CI / GitHub Actions paths updated: `.github/workflows/dotnet.yml`.

### Versioning

- Versioning config updated: `GitVersion.yml`.

### Documentation

- Docs updated: `docs/RELEASE-PLAN-2.5.0.md`, `docs/RELEASE-PLAN-2.5.1.md`, `docs/RELEASE-PLAN-to-master.md`, `docs/TO-DO.md`.

### Repository tooling

- Repo tooling updated: `.cursor/skills/release-plan/SKILL.md`.

---

## v2.5.0 — 14 Sep 2026

### Library

- Stock `main.Register`: `Password` is optional; `CreateUserAsync` skips hashing blank/whitespace passwords (`PasswordPhc` may be null).

### Tests

- Registration flow / `UserService` coverage for register without password.

### Documentation

- `FLOWS.md` / README: optional Register password; `userAccountIdKey` / 18-flow sync; Notifications described as host config without library defaults.
- Contributor docs: release checklist / Change summary script are maintainer-only (`release-plan` skill); removed from PR template and contributor checklists.

### Repository tooling

- `.cursor/rules/401-markdown.mdc` — GFM table formatting (short separators, one-space cells).
- Release readiness checklist renamed to `docs/RELEASE-PLAN-to-master.md`; Change summary script → `release-plan-to-master.mjs`.

---

## v2.4.0 — 14 Sep 2026

### Library

- Host-supplied language context for notification templates (`HostSuppliedLanguageContext`); OTP / reset-password paths compose messages via `INotificationComposer` + `ISecurityNotifier`.
- `Authentication:Notifications` (`NotificationOptions`) — host-supplied brand placeholders; no built-in library defaults; template placeholder `{{supportEmail}}` (was `{{support}}` typo/alias).
- Embedded register / verify / confirm-email / password-changed templates for `en` / `ru` / `ro` (txt + html); stock flows use `verify` / `reset` / `password-changed`.
- `main.ChangeAccountEmail` flow / `IUserService.ChangeAccountEmailAsync` (host-authorized `UserAccountId`); audit `AccountEmailChanged`.
- License JWT: dotted product-type claims (e.g. `Cross.Identity`) parsed against underscore enum names.
- Package / solution icons renamed to `icon.png` / `icon.svg`.

### Tests

- Coverage for send-code / reset-password / ChangeAccountEmail / placeholders / license; new 2.4 tests use Given/When/Then naming; `NotificationOptions` asserts no built-in brand defaults.

### Documentation

- Version plan `2.4.0` / `TO-DO` backlog sync; `FLOWS.md` / README host-authorize notes for ChangeAccountEmail.

### CI / repo

- SonarCloud project key + name; email templates excluded from duplicate-code detection.
- GitHub issue / PR templates and rulesets aligned to Cross.Identity.
- Sample.Api: `Authentication:Notifications` in `appsettings.json`.

---

## v2.3.0 — 26 Aug 2026

### Library

- **Breaking:** `main.Logout` / `main.RefreshToken` take **`Jti`** (not compact refresh string); `main.LogoutAll` / `main.ChangePassword` take **`UserAccountId`**. Host resolves identity before `ExecuteAsync`.
- **Breaking:** removed unused compact-string APIs on `IJwtTokenService` (validate/revoke-by-string helpers). Kept Guid/`jti`-based rotate / logout paths.
- Stock steps: `RevokeSessionForLogoutAsync`, refresh-by-id rotation, `RevokeAllTokensForUserAsync`.

### Tests

- Logout / LogoutAll / RefreshToken / ChangePassword / VerifyToken flow and step tests updated for Jti / UserAccountId bags.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 2.2.0 to 2.3.0; `FLOWS.md` lifecycle bags.

See [v2.3.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.3.0) ([PR #20](https://github.com/denis-peshkov/Cross.Identity/pull/20)).

---

## v2.2.0 — 25 Aug 2026

### Library

- **Breaking:** user-scoped flows / services no longer take library `RefreshToken` session proof — host authorizes `UserAccountId` before `ExecuteAsync`.
- Affected: communication endpoints get-all / set-preferred; external login link / unlink / get-all. Token lifecycle flows unchanged.
- `EnsureRefreshTokenBelongsToUserAsync` remains an optional host helper (stock user-scoped steps do not call it).

### Tests

- Communication-endpoint and external-login flow / step tests updated for host-authorized `UserAccountId`.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 2.1.1 to 2.2.0; `FLOWS.md` user-scoped authorization.

See [v2.2.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.2.0) ([PR #19](https://github.com/denis-peshkov/Cross.Identity/pull/19)).

---

## v2.1.1 — 24 Aug 2026

### Library

- **Breaking:** `ConcurrencyStamp` rotation moved from `ConcurrencyStampInterceptor` / `OnConfiguring` into `IdentityContext.SaveChanges` / `SaveChangesAsync`; interceptor type **removed**.
- Pooled DbContext (`AddDbContextPool` / `AddPooledDbContextFactory`) supported — host must not re-add the removed interceptor.

### Tests

- IdentityContext pooling / concurrency stamp coverage.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 2.0.x to 2.1.1; bulk `ExecuteUpdate` / `ExecuteDelete` stamp contract clarified.

See [v2.1.1](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.1.1) ([PR #18](https://github.com/denis-peshkov/Cross.Identity/pull/18)).

---

## v2.0.0 — 23 Aug 2026

### Library

- **Breaking:** no ambient `HttpContext` / `IHttpContextAccessor` in the library — hosts pass `HostSuppliedClientContext` (trusted pipeline).
- **Breaking:** session binding (`Created*` on refresh rows), optional `SessionBindingCheckIp`, refresh idle timeout (`LastActivityAt` / `RefreshTokenIdleTimeout`).
- **Breaking:** revoke reason → `RevokedReason`; issue/revoke metadata in `auth.Audits`; filtered uniqueness on verified email/phone; OAuth link only when provider email verified.
- Delivery / OTP channel resolution model; stock flows and communication endpoints; Argon2id/PBKDF2 (+ pepper) password hashing.

### Tests

- Broad flow / step / infrastructure coverage for the 2.0 auth model.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 1.10.x to 2.0.0; `FLOWS.md` / `MIGRATION.md` consumer guidance.

### Versioning

- `GitVersion.yml` updated for the 2.x line.

See [v2.0.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v2.0.0) ([PR #16](https://github.com/denis-peshkov/Cross.Identity/pull/16)).

---

## v1.10.0 — 6 Aug 2026

### Library

- **Breaking:** `GetClaimValueAsync` → sync `GetClaimValue`; `GenerateIdTokenAsync` → sync `GenerateIdToken`.
- **Breaking:** `ValidateAccessTokenAsync` performs crypto validation (`ValidateTokenAsync`) before DB `jti` lookup; `CancellationToken` required on `IJwtTokenService` async APIs (no `= default`).
- `VerifyToken` AccessToken min length relaxed (see release / BREAKING).

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 1.9.x to 1.10.0; breaking details centralized for NuGet consumers.

See [v1.10.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.10.0) ([PR #15](https://github.com/denis-peshkov/Cross.Identity/pull/15)).

---

## v1.9.0 — 6 Aug 2026

### Library

- Introduced `VerifyToken` flow / operation with integration and unit tests.

See [v1.9.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.9.0) ([PR #13](https://github.com/denis-peshkov/Cross.Identity/pull/13)).

---

## v1.8.0 — 6 Aug 2026

### Library

- Added `ChangePassword` flow with current-password validation.

### Documentation

- Migration guide / PR template notes for `BREAKING:` title prefix.

See [v1.8.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.8.0) ([PR #12](https://github.com/denis-peshkov/Cross.Identity/pull/12)).

---

## v1.7.0 — 5 Aug 2026

### Library

- **Breaking:** external OAuth step types renamed `InitiateExternalLogin` / `CompleteExternalLogin` → `ExternalLoginInitiate` / `ExternalLoginComplete` (operations stay `ExternalLogin` / `ExternalLoginCallback`).

### Tests

- Given-When-Then test naming alignment across Identity test suite.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 1.6.x to 1.7.0.

See [v1.7.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.7.0) ([PR #11](https://github.com/denis-peshkov/Cross.Identity/pull/11)).

---

## v1.6.0 — 5 Aug 2026

### Library

- **Breaking:** removed `TokenByCode` — OTP exchange via `main.Token` with `{ Email|PhoneNumber, Code }`.

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From 1.5.x to 1.6.0.

See [v1.6.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.6.0) ([PR #10](https://github.com/denis-peshkov/Cross.Identity/pull/10)).

---

## v1.5.0 — 5 Aug 2026

### Library

- **Breaking:** built-in flow id `license` → `main`; operation `GetUser` → `GetUserId`; demo flows `game.*` / `shop.*` / `edoctors.*` removed.
- Ship main identity flows with logout, session revocation, and concurrency stamps ([PR #8](https://github.com/denis-peshkov/Cross.Identity/pull/8)).

### CI / release process

- Harden NuGet tag push auth; pin `undici` 6.28.0 in triage tooling ([PR #9](https://github.com/denis-peshkov/Cross.Identity/pull/9)).

### Documentation

- [`docs/BREAKING.md`](BREAKING.md) § From ≤1.4.x to 1.5.0.

See [v1.5.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.5.0).

---

## v1.4.0 — 17 Jul 2026

### Repository tooling

- Streamline GitHub issue templates (remove `type` attribute from bug / feature definitions).

See [v1.4.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.4.0).

---

## v1.3.0 — 17 Jul 2026

### Versioning

- `GitVersion.yml` branch filters adjusted for standard branches.

### CI / release process

- `dotnet.yml` / `branch-policy.yml` updates (`update-nuspec-action@v2`, expanded branch policies).

See [v1.3.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.3.0) ([PR #7](https://github.com/denis-peshkov/Cross.Identity/pull/7)).

---

## v1.2.0 — 17 Jul 2026

### Repository tooling

- Replace RTK wrapper with generic `gh-wrapper.sh` for GitHub CLI.
- Fix `Sample.Api` path formatting in `Cross.Identity.slnx`.

See [v1.2.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.2.0) ([PR #6](https://github.com/denis-peshkov/Cross.Identity/pull/6)).

---

## v1.1.0 — 17 Jul 2026

### Library

- User registration process and related identity flows ([PR #2](https://github.com/denis-peshkov/Cross.Identity/pull/2)).

### Repository tooling

- GitHub templates and Sample.Api REST client for E2E; merge `dev` → `master` ([PR #5](https://github.com/denis-peshkov/Cross.Identity/pull/5)).

See [v1.1.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.1.0).

---

## v1.0.0 — 17 Jul 2026

### Library

- Initial public NuGet release of **Cross.Identity** (identity flows, JWT services, sample host).

See [v1.0.0](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v1.0.0).

---
