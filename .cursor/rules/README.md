# Project Rules (Cursor Rules)

## File structure

### 000-099: Global rules
- `000-global.mdc` - Global project rules
- `001-team-workflow.mdc` - Branches, PR targets (`dev` vs owner-only `master` / `release/*` / `hotfix/*`)
- `002-multi-repo.mdc` - Multi-repository workflow rules
- `003-triage.mdc` - GitHub triage (issues/PRs, skills, CI)

### Cursor triage scripts

- `.cursor/triage/` - `gh` wrapper, CI runners (`collect-data.sh`, `post-pr-triage.mjs`), reports in `docs/`

### Cursor skills (project)

- `.cursor/skills/release-plan/` - Version plans, `TO-DO.md`, `BREAKING.md`; scripts: `resolve-target-version.sh`, `scaffold-breaking-section.sh`, `collect-release-delta.sh`, `release-plan-summary.mjs`
- `.cursor/skills/coderabbit/` - CodeRabbit CLI review → current version plan
- `.cursor/skills/db-scripts/` - This repo’s DbUp paths (`Infrastructure/Scripts/` multi-provider, `auth`, BREAKING); conventions in `102-backend-efcore`
- `.cursor/skills/triage/` - issue + PR triage orchestrator
- `.cursor/skills/issue-triage/` - GitHub issues
- `.cursor/skills/pr-triage/` - GitHub PRs

### 100-199: Backend (.NET)
- `100-backend-dotnet-general.mdc` - General .NET backend rules
- `101-backend-architecture.mdc` - Backend architecture
- `102-backend-efcore.mdc` - EF Core + DbUp SQL migrations (not Code First Migrations): layers, naming, append-only, idempotent scripts
- `103-backend-api-style.mdc` - API style
- `104-backend-auth.mdc` - Authentication and authorization
- `105-backend-security.mdc` - Backend security (validation, sanitization, CORS, HTTPS)
- `106-backend-formatting-and-style.mdc` - Backend code formatting and style
- `107-backend-observability.mdc` - Backend observability

### 200-299: Frontend (Angular)
- `200-frontend-angular-general.mdc` - General Angular rules
- `201-frontend-rxjs-only.mdc` - RxJS rules
- `202-frontend-state-stores-signals.mdc` - State management: Stores and Signals
- `203-frontend-http.mdc` - HTTP rules
- `204-frontend-ui-tailwind.mdc` - UI and Tailwind CSS rules
- `205-frontend-i18n.mdc` - Internationalization rules
- `206-frontend-forms-ugc.mdc` - Forms rules
- `207-frontend-formatting-and-style.mdc` - Frontend code formatting and style
- `208-frontend-angular-routing.mdc` - Angular routing
- `209-frontend-guards.mdc` - Guards (route protection)
- `210-frontend-error-handling.mdc` - Error handling

### 300-399: Testing
- `300-testing-dotnet.mdc` - .NET testing
- `301-testing-angular.mdc` - Angular testing

### 400-499: Output Format
- `400-output-format.mdc` - Output formatting rules

## Usage

All rules are written in English and are applied automatically when working on the project through Cursor IDE.
