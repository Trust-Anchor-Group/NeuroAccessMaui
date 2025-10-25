# Project Description

NeuroAccessMaui is a cross-platform application built with .NET MAUI, targeting Android and other platforms. The project provides secure digital identity management, KYC (Know Your Customer) processing, and contract management features. It leverages modern .NET technologies to deliver a robust, maintainable, and user-friendly experience.

# Coding Standards

## General Guidelines

* Use clear, descriptive names for variables, methods, and classes.
* Write self-documenting code; use comments where necessary for complex logic.
* Keep methods short and focused on a single responsibility.
* Prefer async/await for asynchronous operations.
* Avoid blocking calls on the UI thread.

## .NET & C# Conventions

* Use PascalCase for class, method, property, and local variable names. Only use camelCase for local variables in specific cases (e.g., loop counters, mathematical variables, x, y, z, i, j, etc).
* Use camelCase for private fields, without an underscore prefix.
* Use modern null checking patterns (e.g., `if (obj is null)`).
* Explicitly refer to `this.` for instance members and `base` for base class members.
* Always use **explicitly typed variables** for all local variables and object instantiations. Use `var` only for anonymous types or when the type is truly unnameable.
* **Example:**

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

## MAUI/XAML Guidelines

* Use MVVM pattern for UI logic separation.
* Place UI logic in ViewModels, not in code-behind.
* Use data binding and commands for UI interactions.
* Always ensure that actions that result in UI changes are performed on the main thread (using MainThread).
* Use ObservableObject and RelayCommand from CommunityToolkit.Mvvm for ViewModel implementation.

## File Organization

* Group related files into appropriate folders (e.g., Services, UI/Pages, UI/Controls).
* Name files to match their main class or function.

## Testing

* Don't write tests, but ensure the code is testable.

## Building and Running

* Never build the project if the user has not requested it.
