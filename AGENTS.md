# AGENTS.md

## Project

NeuroAccessMaui is a cross-platform .NET MAUI application for Android, iOS, and Windows. It provides secure digital identity management, KYC processing, and contract management features backed by distributed ledger technology.

This file is the always-on steering guide for Codex users and other agents working in this repository. Follow it for every code, planning, review, and refactoring task.

## Codex Workflow

Use a plan-first workflow for non-trivial feature work, behavioral changes, architecture changes, security-sensitive changes, or broad refactors.

Plans live in:

```text
plans/<feature-slug>/
```

Use these files:

- `requirements.md`
- `design.md`
- `tasks.md`
- `decisions.md` when architectural or product decisions need to be recorded

Keep `plans/<feature-slug>/` as the source of truth for the work. Do not create parallel `.specs/` or `.kiro/` folders unless the user explicitly asks.

### Phase Order

Move through phases in this order:

1. Requirements
2. Design
3. Tasks
4. Implementation
5. Verification

For user-requested planning work, stop at the end of each planning phase and ask for approval before moving to the next phase. If the user explicitly asks to continue through multiple phases in one turn, still keep the phase sections separate and call out assumptions clearly.

For small, obvious fixes, a full plan is not required. Still read the relevant code first and keep the change scoped.

### Requirements Phase

Create or update `requirements.md`.

Include:

- Purpose or problem
- Goals
- Non-goals
- User stories or scenarios
- Requirements with stable IDs such as `R1`, `R2`, `R3`
- Acceptance criteria
- Edge cases
- Security and privacy considerations
- Compatibility and migration notes
- Assumptions
- Open questions

Acceptance criteria must be testable. Prefer:

```text
WHEN <event or condition>
THEN <expected behavior>
```

Do not design implementation details during this phase unless they are needed to clarify scope.

### Design Phase

Create or update `design.md` only after requirements are approved.

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

Designs must fit the existing .NET MAUI, MVVM, service, navigation, localization, and safe-area conventions in this repository.

### Tasks Phase

Create or update `tasks.md` only after design is approved.

Tasks must be small, ordered, and independently reviewable. Use checkboxes and stable IDs that trace back to requirements.

Use this shape:

```md
- [ ] R1-T1 Add focused task title
  - Covers: R1, R2
  - Files: NeuroAccessMaui/Services/ExampleService.cs
  - Validate: Source review, or an exact command if the user explicitly requested running validation
  - Done when: Concrete completion criteria
```

Do not add tests to the task list unless tests were explicitly requested or the user approved them during planning.

### Implementation Phase

Implement one task at a time unless the user explicitly asks for a larger batch.

Before editing code:

- Read `requirements.md`, `design.md`, and `tasks.md`.
- Identify the exact task being implemented.
- Check the worktree for existing changes and preserve user work.
- Do not silently expand scope.

During implementation:

- Follow the approved design.
- Keep changes minimal and maintainable.
- If the design is wrong or incomplete, stop and propose a design or task update.
- Do not add dependencies without explicit approval.
- Do not create tests, documentation files, or new plans unless explicitly requested by the user or required by the approved task.

After implementation:

- Update `tasks.md` only for the task actually completed.
- Add a short note under the task when useful.
- Run validation only when the user explicitly asked for it or the task specifically authorizes it. Otherwise, provide the exact validation command the user can run.
- Summarize changed files, validation status, and remaining risks.

### Review Phase

When asked to review a plan or code change, prioritize findings first:

- Bugs or behavioral regressions
- Security, privacy, or data-loss risks
- Missing requirements coverage
- Design contradictions
- Incomplete or unsafe task sequencing
- Missing validation that materially affects confidence

Keep summaries brief and secondary to findings.

### Codex Skills

Repo-local Codex skills live in:

```text
.agents/skills/
```

Use them for repeatable workflows:

- `$create-plan` for requirements, design, and task planning
- `$implement-plan-task` for implementing exactly one approved task
- `$review-plan` for reviewing plan completeness and consistency

If a skill conflicts with this file, this file wins.

## Coding Standards

### General Guidelines

- Use clear, descriptive names for variables, methods, and classes.
- Write self-documenting code; use comments only where logic is complex or non-obvious.
- Keep methods short and focused on a single responsibility.
- Use `async` and `await` for asynchronous operations.
- Async methods must use the `Async` suffix, such as `LoadUserAsync`.
- Avoid blocking calls on the UI thread.
- Keep code testable even when explicit tests are not written.
- Ensure all classes, structs, enums, and public functions/properties include XML documentation comments.
- Each XML documentation summary must clearly describe the purpose of the member.
- Parameters and return values must be documented with `<param>` and `<returns>` tags where applicable.

Example:

```csharp
/// <summary>
/// Loads user data asynchronously.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public async Task LoadUserAsync(string UserId)
{
}
```

### .NET And C# Conventions

Use PascalCase for:

- Classes, structs, and enums
- Methods
- Properties
- Local variables
- Arguments

Use camelCase without an underscore prefix for all private fields, including:

- Private instance fields
- `private const` fields
- `private static readonly` fields

Use camelCase for local variables only in short-term loop or mathematical cases, such as `x`, `y`, `z`, `i`, and `j`.

Prefer explicitly typed variables for all local variables and object instantiations. Use `var` only for anonymous types or when the type is truly unnameable.

Use modern null checks:

```csharp
if (Value is null)
{
}
```

Always use `this.` for instance members. Use `base.` when explicitly referencing base class members.

Example:

```csharp
KycReference Reference = new KycReference();
string UserName = "Alice";
int Index = 0;

private const string defaultEndpoint = "https://api.example.com";
private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(30);
private string currentUser;

for (int i = 0; i < 10; i++)
{
}
```

### MAUI And XAML Guidelines

- Use the MVVM pattern for UI logic separation.
- Place UI logic in ViewModels, not in code-behind.
- Use data binding and commands for user interactions.
- Use `ObservableObject` and `RelayCommand` from `CommunityToolkit.Mvvm`.
- ViewModels should inherit from the shared `BaseViewModel`.
- Avoid inline event handlers in XAML; use commands and bindings.
- Purely visual animations may be implemented in code-behind when needed for reliability or simplicity, as long as business logic and state remain in the ViewModel.
- Ensure all user-facing text supports localization.
- All UI updates must occur on the main thread by using `MainThread.BeginInvokeOnMainThread()` or `Dispatcher.Dispatch()`.

### Navigation And Insets

- Use `NavigationService`, backed by `CustomShell`, from view models to change screens.
- Avoid directly pushing MAUI navigation pages.
- Pages should derive from `BaseContentPage`.
- Section views should derive from `BaseContentView`.
- Keyboard insets are opt-out. Set `KeyboardInsets.Mode="Manual"` or implement `IKeyboardInsetAware` only when a view must manage that padding itself.
- Never hard-code safe-area margins.
- Use `SafeArea` attached properties on pages or bind to `SafeInsetsExtension` in XAML when a literal `Thickness` is needed.
- Popups and toasts already adjust to inset updates. Override that behavior only with a clear reason.

### File Organization

Group related files into appropriate folders, including:

- `Services`
- `UI/Pages`
- `UI/Controls`
- `Models`
- `ViewModels`

Each file must be named after its primary class or component.

## Testing

Do not auto-generate or commit tests unless explicitly requested.

Code must remain testable:

- Use dependency injection where appropriate.
- Keep logic modular.
- Avoid static coupling and global state.
- Separate platform-dependent code from plain .NET logic when possible.

Test project rules, when tests are explicitly requested:

- Framework: MSTest via the `MSTest` NuGet package.
- Test projects: `NeuroAccess.Nfc.Test` and `NeuroAccessMaui.Test`.
- Target framework: `net10.0` with no MAUI TFMs.
- Test projects may include source files under test through `<Compile Include="..." Link="..." />` when the source has no MAUI platform dependencies.

Test method rules:

- Use `[TestMethod]`.
- Use `public void` or `public async Task`.
- Follow `Test_N_Description`, where `N` is a two-digit sequence within the test class.
- Use suffixes such as `Test_03a_...` when inserting between existing tests.

Example:

```csharp
[TestMethod]
public void Test_01_BuildStableKeyName_DatabaseKey_NormalizesPath()
{
}
```

Test class rules:

- Use `[TestClass]`.
- End class names with `Tests`.
- Use one test class per logical area.

Mock rules:

- Place mocks in a `Mocks/` subfolder within the test project.
- Prefix mock names with `Mock`.
- Prefer simple dictionary-backed or in-memory implementations.
- Avoid mocking frameworks unless complexity demands them.

Test helper rules:

- Do not create separate helper classes for test utilities.
- Keep helper methods as `private static` methods inside the test class that uses them.
- Duplicate small helper logic across test classes when that improves locality.

Comment rules:

- Do not reference requirement IDs, task IDs, or spec document numbers in code comments.
- Comments should describe what and why in terms future developers can understand without external documents.

## Documentation Requirements

- All public members must include XML documentation.
- Internal or private members may include XML docs if they expose significant internal logic.
- Summaries should be concise and descriptive.
- Use `<remarks>` when additional context or implementation notes are helpful.

Example:

```csharp
/// <summary>
/// Represents a user identity record for KYC processing.
/// </summary>
/// <remarks>
/// Instances of this class are immutable once created.
/// </remarks>
public class KycReference
{
    /// <summary>
    /// Gets the unique identifier for this reference.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KycReference"/> class.
    /// </summary>
    /// <param name="Id">The unique identifier.</param>
    public KycReference(string Id)
    {
        this.Id = Id;
    }
}
```

## Agent Summary

When generating or editing code:

- Follow the plan-first workflow for non-trivial work.
- Follow all naming, typing, and documentation conventions.
- Respect the existing folder structure and MVVM architecture.
- Include XML documentation comments for all classes, structs, enums, and public members.
- Preserve user changes in the worktree.
- Avoid creating, building, or running the project unless explicitly instructed.
- Do not add tests, documentation files, or dependencies without a direct request or approved plan task.
- Ensure generated code is intended to compile cleanly under these conventions.
