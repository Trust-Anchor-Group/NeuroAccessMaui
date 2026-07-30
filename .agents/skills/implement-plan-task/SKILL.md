---
name: implement-plan-task
description: Implement exactly one approved task from plans/<feature-slug>/tasks.md while preserving traceability to requirements.md and design.md. Use when the user asks Codex to implement a specific planned task, the next unchecked task, or a bounded slice from a NeuroAccessMaui plan.
---

# Implement Plan Task

Implement one planned slice at a time without drifting from the approved requirements and design.

## Before Editing

Read these files for the target plan:

- `plans/<feature-slug>/requirements.md`
- `plans/<feature-slug>/design.md`
- `plans/<feature-slug>/tasks.md`
- `plans/<feature-slug>/decisions.md` when present

Then:

- Identify the exact unchecked task to implement.
- Check `git status --short` and preserve unrelated user changes.
- Inspect the likely affected files before changing them.
- Confirm whether the task authorizes validation commands. AGENTS.md says not to build, run, or test unless explicitly instructed.

## Implementation

- Implement only the selected task.
- Follow the approved design and requirement IDs listed under `Covers`.
- Keep changes scoped to the listed files unless discovery shows another file is necessary.
- Follow NeuroAccessMaui C# conventions: explicit local types, PascalCase local variables and arguments, camelCase private fields, `this.` for instance members, XML docs for public members.
- Keep MAUI UI logic in ViewModels, use bindings and commands, and preserve localization requirements.
- Do not add dependencies, tests, documentation files, or new plan files unless the task or user explicitly asks.

If the design is wrong, stop and propose a plan update instead of silently implementing a different design.

## Task Update

After implementation, update `tasks.md` only for the selected task:

- Mark the checkbox complete only when the task is actually done.
- Add a concise note if there is useful implementation or validation context.
- Leave other unchecked tasks untouched.

## Validation

Run only validation that was explicitly requested or authorized by the task. If validation is not authorized, do not run it; report the exact command the user can run.

When validation fails:

- Investigate whether the failure is caused by the current task.
- Fix task-related failures when feasible.
- Report unrelated or pre-existing failures without masking them.

## Final Response

Summarize:

- The task implemented
- Files changed
- Validation run or skipped
- Remaining risks or follow-up tasks
