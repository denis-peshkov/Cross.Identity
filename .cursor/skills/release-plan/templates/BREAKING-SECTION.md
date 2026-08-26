# BREAKING section snippet (maintainers)

Rules, scripts, workflow: skill [`release-plan`](../SKILL.md) → **`docs/BREAKING.md`**.

| Placeholder | Meaning |
|-------------|---------|
| `{{REPOSITORY_LINK}}` | GitHub repo base, e.g. `https://github.com/org/repo` |
| `{{PR_NUMBER}}` | PR id when known (omit `([PR #…](…)).` until then) |

```markdown
---
## From {{FROM_VERSION}} to {{TO_VERSION}}

Release: [v{{TO_VERSION}}]({{REPOSITORY_LINK}}/releases/tag/v{{TO_VERSION}}) ([PR #{{PR_NUMBER}}]({{REPOSITORY_LINK}}/pull/{{PR_NUMBER}})).

{{BODY}}
```

Prefer `scaffold-breaking-section.sh` — fills `{{REPOSITORY_LINK}}` from `git remote` + resolved version.
