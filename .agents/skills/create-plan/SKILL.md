---
name: create-plan
description: Create or update a NeuroAccessMaui plan-first workflow under plans/<feature-slug>/ with requirements.md, design.md, tasks.md, and optional decisions.md. Use when the user asks to plan a feature, bugfix, refactor, architecture change, KYC flow change, MAUI workflow change, or wants a Kiro-style Requirements -> Design -> Tasks workflow before implementation.
---

# Create Plan

Create plans that let Codex users move deliberately from requirements to design to tasks before code changes begin.

## Ground Rules

- Use `plans/<feature-slug>/`.
- Keep existing plans in `plans/`; do not create `.specs/` or `.kiro/`.
- Preserve AGENTS.md conventions, especially the rule against running builds/tests unless explicitly instructed.
- Ask for approval at phase boundaries unless the user explicitly requested multiple phases in one turn.
- Do not implement code while creating a plan.

## Feature Slug

Choose a short lowercase slug from the feature name:

- Use letters, digits, and hyphens.
- Prefer product or behavior wording over implementation wording.
- Reuse an existing plan folder when the request clearly continues that plan.

## Requirements Phase

Create or update:

```text
plans/<feature-slug>/requirements.md
```

Include:

- Purpose
- Goals
- Non-goals
- User stories or scenarios
- Requirements with IDs such as `R1`, `R2`, `R3`
- Acceptance criteria
- Edge cases
- Security and privacy considerations
- Compatibility and migration notes
- Assumptions
- Open questions

Use testable acceptance criteria:

```text
WHEN <event or condition>
THEN <expected behavior>
```

Do not choose implementation details unless needed to clarify scope.

## Design Phase

Create or update this file only after requirements are approved:

```text
plans/<feature-slug>/design.md
```

Include:

- Current system analysis
- Proposed architecture
- Data model changes
- API, service, and interface changes
- UI and UX changes when relevant
- Error handling
- Security and privacy considerations
- Migration and backwards compatibility
- Test strategy
- Alternatives considered

Tie the design back to requirement IDs. If discovery shows the requirements are wrong, update requirements first.

## Tasks Phase

Create or update this file only after design is approved:

```text
plans/<feature-slug>/tasks.md
```

Use small, ordered, reviewable tasks:

```md
- [ ] R1-T1 Add focused task title
  - Covers: R1, R2
  - Files: NeuroAccessMaui/Services/ExampleService.cs
  - Validate: Source review, or an exact command if validation was approved
  - Done when: Concrete completion criteria
```

Tasks should avoid hidden bundles. If one task affects unrelated areas, split it.

## Decisions

Create `decisions.md` only when there are meaningful product, architecture, migration, or security decisions that future Codex users should preserve.
