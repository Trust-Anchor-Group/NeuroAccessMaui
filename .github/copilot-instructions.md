# AGENTS.MD

## Project Description

**NeuroAccessMaui** is a cross-platform application built with **.NET MAUI**, targeting Android, iOS and Windows
The project provides secure digital identity management, KYC (Know Your Customer) processing, and contract management features based on a distrubuted ledger technology.  
It leverages modern .NET technologies to deliver a robust, maintainable, and user-friendly experience.

This document defines **coding standards and behavior expectations** for both **developers and AI agents** contributing to the project.  
All agents must follow these conventions when generating, refactoring, or reviewing code.

---

## Coding Standards

### General Guidelines

- Use clear, descriptive names for variables, methods, and classes.  
- Write self-documenting code; use comments only where logic is complex or non-obvious.  
- Keep methods short and focused on a single responsibility.  
- Use `async`/`await` for asynchronous operations.  
  - Async methods must use the `Async` suffix (e.g., `LoadUserAsync`).  
- Avoid blocking calls on the UI thread.  
- Code must be **testable**, even if explicit tests are not written.  
- Ensure all **classes, structs, enums, and public functions/properties** are documented with **XML documentation comments (`///`)**.  
  - Each summary must clearly describe the purpose of the member.  
  - Parameters and return values must be properly annotated using `<param>` and `<returns>` tags.  

**Example:**

```csharp
/// <summary>
/// Loads user data asynchronously.
/// </summary>
/// <param name="userId">The unique identifier for the user.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public async Task LoadUserAsync(string userId) { ... }
```

---

### .NET & C# Conventions

#### Naming

- Use **PascalCase** for:
  - Classes/Structs/Enums
  - Methods
  - Properties
  - **Local variables**
  - Arguments
- Use **camelCase** for private fields (without an underscore prefix).  
- Only use **camelCase** for local variables in specific short-term cases, such as loop counters or mathematical variables (`x`, `y`, `z`, `i`, `j`, etc).  

#### Typing

- Prefer **explicitly typed variables** for all local variables and object instantiations.  
- Use `var` **only** for anonymous types or when the type is truly unnameable.  

#### Null Checking

- Use modern null checking patterns:  

  ```csharp
  if (obj is null)
  ```

  instead of `== null`.  

#### Instance Access

- Always use `this.` for instance members.  
- Use `base.` when explicitly referencing base class members.  

#### Example

```csharp
// Correct:
KycReference Reference = new KycReference();
string UserName = "Alice";
int Index = 0;

// Allowed (short loop/mathematical):
for (int i = 0; i < 10; i++) { ... }
double x = 1.0, y = 2.0;

// Not allowed:
// var reference = new KycReference();
// string userName = "Alice";
```

---

### MAUI / XAML Guidelines

- Use the **MVVM pattern** for UI logic separation.  
- Place all UI logic in **ViewModels**, not in code-behind.  
- Use **data binding** and **commands** for user interactions.  
- Use **ObservableObject** and **RelayCommand** from `CommunityToolkit.Mvvm`.  
- All UI updates must occur on the **main thread**:
  - Use `MainThread.BeginInvokeOnMainThread()` or `Dispatcher.Dispatch()` for UI changes.  
- ViewModels should inherit from a shared **BaseViewModel** for consistency.  
- Avoid inline event handlers in XAML; use Commands and Bindings instead.  
- Ensure all user-facing text supports localization.  

---

### File Organization

- Group related files into appropriate folders:
  - `Services`
  - `UI/Pages`
  - `UI/Controls`
  - `Models`
  - `ViewModels`
- Each file must be named after its primary class or component.  

**Example Directory Structure:**

```
/NeuroAccessMaui
  /Models
  /ViewModels
  /Services
  /UI
    /Pages
    /Controls
```

---

### Testing

- AI agents and developers **should not auto-generate or commit tests** unless explicitly requested.  
- Code must remain **testable** — logic should be modular, dependency-injected, and avoid static coupling.  
- Avoid dependencies that make testing difficult (e.g., static singletons or global state).  

---

### Building and Running

- **Do not build automatically.**  
  AI agents or tools must **not trigger builds** unless explicitly requested by the user or a CI/CD pipeline.  
- Builds and deployments should only be executed when explicitly initiated by a developer or automation process.  

---

## Documentation Requirements

- All **public** members (classes, structs, enums, properties, methods) must include XML documentation.  
- Internal or private members may include XML docs if they expose significant internal logic.  
- Summaries must be concise yet descriptive — aim for one or two clear sentences.  
- Use `<remarks>` tags when additional context or implementation notes are helpful.  
- Example format:

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
    /// <param name="id">The unique identifier.</param>
    public KycReference(string id)
    {
        this.Id = id;
    }
}
```

---

## Summary for AI Agents

When generating or editing code, AI agents must:

- Follow all naming, typing, and documentation conventions.  
- Respect the existing folder structure and MVVM architecture.  
- Include **XML documentation comments** for all classes, structs, enums, and public members.  
- Avoid creating, building, or running the project unless explicitly instructed.  
- Not add tests, documentation files, or dependencies without a direct request.  
- Ensure all generated code compiles cleanly and adheres to these conventions.  

---

**End of AGENTS.MD**
