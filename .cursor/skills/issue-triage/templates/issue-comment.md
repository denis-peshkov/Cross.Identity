# Issue Comment Templates — Cross.Identity

GitHub comments — **in English**.

---

## Template 1 — Acknowledgment + Request Info

```markdown
## Issue Triage

**Category**: {Bug | Feature | Enhancement | Question}
**Priority**: {P0 | P1 | P2 | P3}
**Effort estimate**: {XS | S | M | L | XL}

### Assessment

{1-2 sentences about the issue and why it matters for an identity/auth library.}

### Missing Information

To move forward, we need:

- **NuGet version** or git commit (`Cross.Identity` package version)
- **Target framework** (e.g. net8.0)
- **Flow name** if auth-related (see `FLOWS.md`, e.g. `main.Token`)
- **Reproduction steps** (no real passwords, tokens, or PII)

### Next Steps

{What happens after the info is provided.}

---
*Triaged via [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) Cursor `/issue-triage`*
```

---

## Template 2 — Duplicate

```markdown
## Duplicate Issue

This issue covers the same problem as #{original_number}: **{original_title}**.

### Overlap

{Explain overlap in 1-2 sentences.}

If your scenario differs materially, please reopen with that context. Otherwise, follow the original issue.

---
*Triaged via [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) Cursor `/issue-triage`*
```

---

## Template 3 — Close (Stale)

```markdown
## Closing: No Activity

This issue has been open for {N} days without activity. We're closing it to keep the backlog actionable.

If still relevant, reopen with your current NuGet version, target framework, and reproduction steps.

---
*Triaged via [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) Cursor `/issue-triage`*
```

---

## Template 4 — Close (Out of Scope)

```markdown
## Closing: Out of Scope

After review, this request falls outside Cross.Identity's current scope as an identity/auth library.

### Rationale

{Specific reason — e.g. app-specific UI, unrelated infrastructure.}

### Alternatives

{If applicable: extension points, Sample.Api, or separate package.}

---
*Triaged via [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) Cursor `/issue-triage`*
```
