# PR Review Comment Template

Comments in **English**. Resolve `{repository_name}` / `{repository_url}` from `gh repo view`.

```markdown
## Review

**Scope**: Security (secrets/auth/licensing), .NET quality, pipeline/registration, test coverage

### Summary

{1-2 sentences — main takeaway.}

### Critical Issues 🔴

{- `path/from/this-pr.cs:42` — problem, impact, suggested fix.}

{If none: "None found."}

### Important Issues 🟠

{Significant issues with file:line citations.}

{If none: "None found."}

### Suggestions 🟡

{Nice-to-haves. Omit section if none.}

### What's Good ✅

{At least one specific positive point.}

---
*Automated review via [{repository_name}]({repository_url}) Cursor `/triage-pr`*
```

## Severity

- 🔴 Critical: security (secret leak, auth/licensing bypass), data loss, broken registration/pipeline, missing tests for security fix
- 🟠 Important: error handling gaps, breaking public API without docs, missing behavior tests
- 🟡 Suggestion: naming, DRY, documentation

## Checks (mention when relevant)

- No logging of secrets / license keys (see `references/dotnet-checklist.md`)
- Auth/licensing / pipeline security
- `docs/BREAKING.md` / README updated for public API changes
- `*Tests*/` coverage for new behavior
- `Nullable enable`, `Async` suffix, `.editorconfig`

**Tone**: professional, constructive. 200–400 words.
