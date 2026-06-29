---
name: review-plan
description: Review a NeuroAccessMaui plan under plans/<feature-slug>/ for requirement clarity, design consistency, task quality, traceability, safety, and implementation readiness. Use before implementation, before approving requirements/design/tasks, or when the user asks for a planning review.
---

# Review Plan

Review plan files as a code-review style gate before implementation begins.

## Inputs

Read the relevant files:

- `plans/<feature-slug>/requirements.md`
- `plans/<feature-slug>/design.md`
- `plans/<feature-slug>/tasks.md`
- `plans/<feature-slug>/decisions.md` when present

If a phase file is missing, review only the phases that exist and call out the missing file as a blocker when it prevents approval.

## Review Priorities

Lead with findings, ordered by severity.

Check requirements for:

- Clear purpose, goals, and non-goals
- Stable requirement IDs
- Testable acceptance criteria
- Security and privacy coverage
- Compatibility and migration coverage
- Open questions that would block design or implementation

Check design for:

- Coverage of every requirement
- Consistency with existing .NET MAUI, MVVM, navigation, inset, localization, and service patterns
- Clear interfaces, model changes, and error handling
- Security-sensitive behavior and data handling
- Backwards compatibility
- Alternatives considered when tradeoffs matter

Check tasks for:

- One-task-at-a-time implementability
- Traceability through `Covers`
- Realistic file scope
- Validation guidance that respects AGENTS.md
- Done criteria that can be checked
- Missing ordering dependencies

## Output Shape

Use this order:

1. Findings
2. Open questions
3. Readiness verdict
4. Brief summary

For each finding, include the file path and line number when available.

Use severity labels:

- `High` for blockers, unsafe behavior, missing core requirements, or design contradictions.
- `Medium` for gaps that may cause rework or incomplete implementation.
- `Low` for clarity, sequencing, or polish issues.

If there are no findings, say so clearly and mention residual risk.
