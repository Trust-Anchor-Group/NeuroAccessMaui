# AGENTS.md

Guidance for AI coding agents working in this repository. People should start with [README.md](README.md) and [docs/contributing.md](docs/contributing.md).

## Project

Neuro-Access is a .NET MAUI app by Trust Anchor Group for Android and iOS, plus Windows when built on Windows. It gives users digital identities, KYC, smart contracts, tokens and eDaler wallets on top of XMPP.

- C# `latest`. The app targets .NET 10. `NeuroAccess.Nfc` targets `net8.0` and `IdApp.Cv` targets `netstandard2.0`, so some newer APIs and language features are not available there.
- `global.json` sets the SDK version and only allows newer patches of it (`rollForward: latestPatch`).
- `Directory.Build.props` turns on nullable reference types for every project.
- `.editorconfig` holds the formatting, naming and analyzer rules, and `XamlStyler.config` holds the XAML formatting rules. Both are part of the code style below.

## Commands

Run these from the repository root.

```sh
# Style check on the C# files you changed. Fix everything it reports.
# It does not check naming, because those .editorconfig rules are only suggestions.
# Check names by hand against the Naming table under Code style.
dotnet format style NeuroAccessMaui.sln --verify-no-changes --include path/to/First.cs path/to/Second.cs

# Whitespace check (tabs, CRLF, final newline) on the files you changed.
dotnet format whitespace . --folder --verify-no-changes --include path/to/First.cs path/to/Second.cs

# Build the app for Android, to check that a change compiles.
dotnet build NeuroAccessMaui/NeuroAccessMaui.csproj -f net10.0-android

# Unit tests.
dotnet test NeuroAccess.Nfc.Test/NeuroAccess.Nfc.Test.csproj
dotnet test IdApp.Cv.Test/IdApp.Cv.Test.csproj
```

## Where things live

| Path | What it holds |
| --- | --- |
| `NeuroAccessMaui/Services/` | App services, reached through `ServiceRef` (for example `ServiceRef.NavigationService`, `ServiceRef.LogService`, `ServiceRef.Localizer`) |
| `NeuroAccessMaui/UI/Pages/<Area>/<Feature>/` | A page's XAML, its view model, navigation args and sub-views, side by side |
| `NeuroAccessMaui/UI/Controls/`, `NeuroAccessMaui/UI/Popups/` | Reusable controls and popups |
| `NeuroAccessMaui/UI/MVVM/` | Shared MVVM infrastructure, including `ObservableTask` and `[ObservableTaskCommand]` |
| `NeuroAccessMaui/Resources/Languages/` | `AppResources.resx` (English), its translations, and the checked-in `AppResources.Designer.cs` |
| `NeuroAccessMaui/Resources/Styles/` | Shared XAML styles |
| `NeuroAccessMaui/Platforms/` | Android, iOS and Windows specific code |
| `NeuroAccess.Nfc/`, `IdApp.Cv/` | Plain .NET libraries for NFC and image processing |
| `NeuroAccessMaui.Camera/`, `NeuroAccessMaui.OCR/` | Camera and OCR libraries |
| `NeuroAccessMaui.Generator/` | Source generator behind `[ObservableTaskCommand]` |
| `NeuroAccess.Nfc.Test/`, `IdApp.Cv.Test/` | MSTest projects |
| `docs/` | Developer docs |

Before working in an area, read the relevant parts of its guide in `docs/`: `architecture.md`, `services.md`, `navigation.md`, `animations.md`, `notifications.md`, `onboarding.md`, `kyc.md` or `branding.md`.

## Code style

These rules are mandatory for every line you write or change. Several of them differ from common .NET defaults (PascalCase locals and parameters, no `var`, no `_` on fields), so do not fall back on habits from other codebases. Where existing code breaks a rule, follow the rule in the lines you touch and leave untouched code as it is.

### Naming

| Symbol | Style | Example |
| --- | --- | --- |
| Classes, structs, enums, interfaces, methods, properties, events | PascalCase. Interfaces start with `I`. | `KycReference`, `IKycService`, `IsValid` |
| Local variables | PascalCase | `string UserName = "Alice";` |
| Parameters, including primary constructor parameters | PascalCase | `LoadUserAsync(string UserId)` |
| Exception variables | PascalCase, like other locals | `catch (Exception Ex)` |
| Private fields, including `private const` and `private static readonly` | camelCase with no `_` prefix | `currentUser`, `defaultEndpoint`, `requestTimeout` |
| Loop counters and math variables | Short lowercase names are allowed | `i`, `j`, `x`, `y`, `z` |
| Async methods, including `[RelayCommand]` methods | End in `Async`. The generated command drops the suffix, so `RefreshAsync` still gives `RefreshCommand`. | `LoadUserAsync` |

### Types and expressions

- Declare every local with its explicit type. Do not use `var`, except for anonymous types or a type that cannot be named. Target-typed `new()` is fine: `List<string> Names = new();`.
- Prefix every instance member with `this.`, for fields, properties, methods and events alike. Use `base.` when you mean the base class member.
- Check for null with `is null` and `is not null`, never `== null` or `!= null`.
- Await every task, or discard it on purpose with `_ = this.SaveAsync();`. An unawaited call is a build error here (CS4014).
- Use `async` and `await` for asynchronous work. Never block on a task with `.Result` or `.Wait()`.
- Nullable reference types are on. Fix nullable warnings instead of suppressing them.

### Layout

- Indent with tabs, use CRLF line endings, and end every file with a newline.
- Use block-scoped namespaces (`namespace X { ... }`), never file-scoped ones. The namespace matches the folder path.
- Put `using` directives at the top of the file, outside the namespace, with `System` namespaces first.
- Order modifiers as `.editorconfig` does: access modifier, `static`, `new`, `virtual`/`abstract`/`sealed`/`override`, `readonly`, `async`.
- Put one primary type in each file and name the file after it.
- Format XAML by `XamlStyler.config`: tabs, one attribute per line, and attributes in the order it defines.

### Documentation comments

- Every class, struct, record, enum and interface, and every public member, has a `/// <summary>` that says what it is for.
- Document each parameter with `<param>` and each non-void return value with `<returns>`. Primary constructor parameters are documented on the type.
- Document `[ObservableProperty]` fields. The summary is copied to the generated property.
- Document internal and private members when their logic is not obvious. Use `<remarks>` for extra context and `<see cref="..."/>` to point at other code.

### Comments and structure

- Use clear, descriptive names and keep methods short, with one responsibility each.
- Comment only where the logic is complex or the reason is not obvious.
- Comments must make sense from the code alone. Do not mention requirement or task IDs, plan documents, tickets or chat context.

### Example

The names below are illustrative. The shape is what matters.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc.References
{
 /// <summary>
 /// View model for the page that lists the user's KYC references.
 /// </summary>
 public partial class ReferencesViewModel : BaseViewModel
 {
  private const int maxReferences = 50;
  private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(30);
  private readonly string? userId;

  /// <summary>
  /// Creates the view model for the KYC references page.
  /// </summary>
  /// <param name="Args">Navigation arguments, if any.</param>
  public ReferencesViewModel(ReferencesNavigationArgs? Args)
  {
   this.userId = Args?.UserId;
  }

  /// <summary>
  /// Number of references that are still valid.
  /// </summary>
  [ObservableProperty]
  private int validCount;

  /// <summary>
  /// Reloads the references and counts the valid ones.
  /// </summary>
  [RelayCommand]
  private async Task RefreshAsync()
  {
   if (this.userId is null)
    return;

   try
   {
    KycReference[] References = await ServiceRef.KycService.LoadReferencesAsync(this.userId, requestTimeout);
    int Count = 0;

    for (int i = 0; i < References.Length && i < maxReferences; i++)
    {
     if (References[i].IsValid)
      Count++;
    }

    this.ValidCount = Count;
    await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)]);
   }
   catch (Exception Ex)
   {
    ServiceRef.LogService.LogException(Ex);
    await ServiceRef.UiService.DisplayException(Ex);
   }
  }
 }
}
```

## App patterns

- **MVVM.** Logic and state live in view models that derive from `BaseViewModel`. Pages derive from `BaseContentPage` and section views from `BaseContentView`. Code-behind holds only purely visual work, such as animations.
- **Bindings.** Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm, and bind commands in XAML instead of adding event handlers.
- **Navigation.** Change screens with `ServiceRef.NavigationService`, which is backed by `CustomShell`. Never push MAUI pages directly. Show popups with `ServiceRef.PopupService` and toasts with `ServiceRef.ToastService`. See `docs/navigation.md`.
- **Localization.** All user-facing text is localized: `ServiceRef.Localizer[nameof(AppResources.Key)]` in C# and `{l:Localize Key}` in XAML. For every new or changed key, update `AppResources.resx` (English), `AppResources.sv.resx`, `AppResources.es.resx` and `AppResources.pt.resx` by hand so the four keep the same keys, and add the matching property to `AppResources.Designer.cs`, which only Visual Studio regenerates. The other eight languages (da, de, fi, fr, no, ro, ru, sr) are filled in by `Translation/TranslateResources.bat`; leave them to it.
- **Main thread.** Update bound properties and UI on the main thread, with `MainThread.BeginInvokeOnMainThread()` or `Dispatcher.Dispatch()`, whenever the code can run off it, such as in service events, timers and `Task.Run`. Use `MainThread.InvokeOnMainThreadAsync()` when the caller must wait for the UI update.
- **Errors.** Catch, log with `ServiceRef.LogService.LogException(Ex)`, and show with `ServiceRef.UiService.DisplayException(Ex)` when the user should know.
- **Safe areas and keyboard.** Never hard-code safe-area margins. Use the `SafeArea` attached properties on pages, or bind to `SafeInsetsExtension` when a literal `Thickness` is needed. Keyboard insets are applied automatically; opt out with `KeyboardInsets.Mode="Manual"` or `IKeyboardInsetAware` only when a view must manage that padding itself. Popups and toasts already adjust to inset changes, so override that only with a clear reason.
- **Testability.** Inject dependencies where you can, avoid new static state, and keep plain .NET logic apart from platform code.

## UI and product rules

- **Separators.** Never use the middle dot (·, U+00B7) as a separator in user-facing text. Use separate labels, a line break or a comma.
- **Buttons and tap targets.** Keep padding and margins modest. Every tappable element keeps a hit area of at least 48x48, set with `MinimumHeightRequest` and `MinimumWidthRequest` rather than extra padding. Never give a control a fixed height that clips its text; it must grow with large system text sizes.
- **Maps.** Maps and geo features use OpenStreetMap through Mapsui, as the controls in `NeuroAccessMaui/UI/Controls/Geo/` do. Do not add Google Maps, Apple Maps or other map SDKs.
- **Contracts.** Contract screens are generic and built from the template's data: roles, parts and parameters. Never special-case a particular contract or template. The Trust provider role is normally added automatically, so do not ask the user to pick it.

## Tests

Add unit tests wherever they help verify a change. They do not need asking first. When you add them:

- Use MSTest (the `MSTest` package) in `NeuroAccess.Nfc.Test` or `IdApp.Cv.Test`, which target `net10.0` without MAUI. A source file with no MAUI dependencies can be linked in with `<Compile Include="..." Link="..." />`.
- Test classes have `[TestClass]`, end in `Tests`, and cover one logical area each.
- Test methods have `[TestMethod]`, are `public void` or `public async Task`, and are named `Test_NN_Description`, with a two-digit number in the class. Insert between existing tests with a letter, as in `Test_03a_...`. Example: `Test_01_BuildStableKeyName_DatabaseKey_NormalizesPath`.
- Mocks go in a `Mocks/` folder, start with `Mock`, and are simple in-memory or dictionary-backed implementations. Avoid mocking frameworks unless the complexity demands one.
- Helpers are `private static` methods in the test class that uses them, not shared helper classes. Duplicate small helpers when that keeps tests self-contained.

## Git and pull requests

- Branch from `dev` and open PRs against `dev` (Git Flow, see `docs/contributing.md`).
- Fill in `.github/pull_request_template.md`, including the Changelog section with one-line entries under Added, Changed, Fixed or Removed. A workflow copies them into `CHANGELOG.md` when the PR merges.
- Do not edit `CHANGELOG.md` by hand; that workflow keeps it up to date.

## Boundaries

Always:

- Read the code around a change before editing it, and match its structure and patterns. Where it breaks the code style rules, the rules still win in the lines you write.
- Keep changes to what the task needs.
- Preserve other uncommitted work. The working tree often holds several tasks in progress, so never revert, reset, stash, check out or reformat files you were not asked to change.
- Ignore `.worktrees/`. It holds checkouts of other branches; never edit files there unless your task is in that worktree.
- Build the app, run the checks, add and run unit tests, and run the app on a device or emulator whenever that helps verify a change. None of these need asking first.

Ask first:

- Adding or upgrading packages, or changing a `.csproj`, `Directory.Build.props`, `global.json`, `.editorconfig` or `XamlStyler.config`. Linking a source file into a test project is fine without asking.
- Adding Markdown docs (XML doc comments are always required, see Code style), plan or design files, or new projects.
- Committing, pushing or opening a PR.

Never:

- Silence a style or analyzer rule to get code through, whether with `#pragma warning disable`, `[SuppressMessage]` or an `.editorconfig` override. Fix the code.
- Commit secrets or the Firebase config files (`google-services.json`, `GoogleService-Info.plist`). They are git-ignored on purpose.
- Edit anything under `bin/` or `obj/`.

## Before you finish

1. Go through your diff against the code style rules: naming, explicit types, `this.`, `is null`, `Async` suffixes, tabs and CRLF, block-scoped namespaces, and XML docs on every new type and public member.
2. Run the style and whitespace checks on the files you changed and fix what they report. If you could not run them, say so.
3. If you changed C# or XAML in the app, build it for Android and fix any errors. If you changed a library that has tests, run them.
4. Make sure every new or changed user-facing string is in `AppResources.resx`, `AppResources.sv.resx`, `AppResources.es.resx` and `AppResources.pt.resx`, and has its property in `AppResources.Designer.cs`.
5. Make sure no unrelated files changed.
6. Report what you changed, what you ran and its result, and anything left open.
