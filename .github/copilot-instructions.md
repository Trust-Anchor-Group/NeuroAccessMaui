# GitHub AI Instructions

Use the repository root `AGENTS.md` as the source of truth for project rules, coding conventions, and the Codex plan-first workflow.

For non-trivial work, follow the plan workflow in `AGENTS.md`:

1. Requirements in `plans/<feature-slug>/requirements.md`
2. Design in `plans/<feature-slug>/design.md`
3. Tasks in `plans/<feature-slug>/tasks.md`
4. One-task-at-a-time implementation
5. Explicit verification

Do not build, run, add tests, add dependencies, or expand scope unless the user explicitly requests it or an approved plan task authorizes it.

Repo-local Codex skills live under `.agents/skills/`:

- `$create-plan`
- `$implement-plan-task`
- `$review-plan`
