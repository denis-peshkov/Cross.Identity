# PR Review Comment Template — Cross.Identity

Comments in **English**.

```markdown
## Review

**Scope**: Security (auth/JWT/tokens), .NET quality, process engine flows, test coverage

### Summary

{1-2 sentences — main takeaway.}

### Critical Issues 🔴

{- `Cross.Identity/Path/File.cs:42` — problem, impact, suggested fix.}

{If none: "None found."}

### Important Issues 🟠

{Significant issues with file:line citations.}

{If none: "None found."}

### Suggestions 🟡

{Nice-to-haves. Omit section if none.}

### What's Good ✅

{At least one specific positive point.}

---
*Automated review via [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) Cursor `/pr-triage`*
```

## Severity

- 🔴 Critical: security (token leak, auth bypass), data loss, broken auth flow, missing tests for security fix
- 🟠 Important: error handling gaps, breaking public API without docs, missing flow tests
- 🟡 Suggestion: naming, DRY, documentation

## Cross.Identity checks (mention when relevant)

- No logging of passwords/tokens/codes (see `105-backend-security.mdc`)
- JWT/OAuth correctness (`104-backend-auth.mdc`)
- Flow JSON + `FLOWS.md` updated for new auth flows
- `Cross.Identity.Tests` coverage for new behavior
- `Nullable enable`, `Async` suffix, `.editorconfig`

**Tone**: professional, constructive. 200–400 words.
