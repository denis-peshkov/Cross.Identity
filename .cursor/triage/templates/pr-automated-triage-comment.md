# PR Automated Triage Comment Template

Agent fills JSON fields; `format-pr-comment.mjs` renders this layout.

```markdown
> <img src="https://raw.githubusercontent.com/denis-peshkov/Cross.Identity/master/IdentityServer.png" width="48" height="48" alt="Cross.Identity"> **Cross.Identity** · Automated triage by AI

## 🔍 Automated Triage

| | |
|---|---|
| ✨ **Category** | `{category}` |
| {priority_emoji} **Priority** | `{priority}` |
| 🎯 **Confidence** | {confidence}% |

### Summary

{summary}

{maintainer_hint_block}

<details>
<summary>📁 Relevant files</summary>

{relevant_files_list}

</details>

{security_block}

---
*Triaged automatically by [Cross.Identity](https://github.com/denis-peshkov/Cross.Identity) · [Cursor](https://cursor.com)* · This is an automated analysis, not a human review.
<!-- cross-identity-triage -->
```

## JSON schema (agent output)

```json
{
  "category": "feature|bug|enhancement|security|docs|chore|question",
  "priority": "critical|high|medium|low",
  "confidence": 85,
  "summary": "2-4 sentences in English.",
  "maintainerHint": "Optional one-line hint, e.g. simple fix / needs security review",
  "relevantFiles": ["Cross.Identity/Services/JwtTokenService.cs"],
  "securityNotes": "Optional; omit if N/A"
}
```

## Category / priority rules (Cross.Identity)

- **security** + **critical/high** for JWT, OAuth, token leak, auth bypass
- **bug** for broken flows/tests
- **feature** for new flows/capabilities
- **enhancement** for refactors/perf without behavior change
