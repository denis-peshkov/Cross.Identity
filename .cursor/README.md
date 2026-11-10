# Cursor project tooling

Portable layout used across Cross.* (and similar) repositories. Repo-specific bits live in skills/docs that name this package; rules with `globs` / `alwaysApply: false` stay inert when matching paths are absent.

| Path | Role |
|---|---|
| [`rules/`](rules/) | Cursor rules (`.mdc`) — global, backend, frontend, testing, output |
| [`skills/`](skills/) | Agent skills — release-plan, coderabbit, triage*, db-scripts, pr-message |
| [`triage/`](triage/) | GitHub triage scripts (`gh` wrapper, CI runners, reports under `triage/docs/`) |

Index of rules and skills: [`rules/README.md`](rules/README.md). Triage usage: [`triage/docs/README.md`](triage/docs/README.md).
