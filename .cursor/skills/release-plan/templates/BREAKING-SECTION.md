# BREAKING section snippet (maintainers)

Rules, scripts, workflow: skill [`release-plan`](../SKILL.md) → **`docs/BREAKING.md`**.

```markdown
---
## From {{FROM_VERSION}} to {{TO_VERSION}}

Release: [v{{TO_VERSION}}](https://github.com/denis-peshkov/Cross.Identity/releases/tag/v{{TO_VERSION}}) ([PR #N](https://github.com/denis-peshkov/Cross.Identity/pull/N)).

{{BODY}}
```

Prefer `scaffold-breaking-section.sh` over hand-copying this snippet.
