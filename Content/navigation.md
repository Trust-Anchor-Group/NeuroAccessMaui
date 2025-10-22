# Navigation & Presenter Layer

This guide explains how Neuro-Access manages navigation, popups, and global UI chrome via a custom `IShellPresenter`. It is intended for developers adding new pages or popups, or integrating with navigation flows from services and view models.

## Table of Contents

- [Overview](#overview)
- [Core Building Blocks](#core-building-blocks)
- [Navigation Workflow](#navigation-workflow)
- [Popup Lifecycle](#popup-lifecycle)
- [Back Handling](#back-handling)
- [Common Usage Examples](#common-usage-examples)
- [Troubleshooting Tips](#troubleshooting-tips)

## Overview

Instead of using the default MAUI Shell navigation, Neuro-Access employs a custom presenter (`CustomShell`) driven by service-layer abstractions. The goals are:

- Serialize navigation and popup operations to avoid overlapping transitions.
- Provide a unified overlay for popups and toasts that can be styled consistently.
- Keep navigation logic testable and independent of concrete UI classes.
- Centralize back handling (hardware, software, and background taps) across platforms.

The presenter exposes just enough surface for services to instruct it what to render, while the services enforce application-specific semantics (blocking popups, lifecycle hooks, logging, etc.).

## Core Building Blocks

| Component | Responsibility | Key Notes |
|-----------|----------------|-----------|
| `NavigationService` | Owns the logical stack of `BaseContentPage` instances, queues navigation requests, and invokes lifecycle hooks. | Located under `Services/UI/NavigationService.cs`. Exposes async APIs such as `GoToAsync`, `SetRootAsync`, `GoBackAsync`. |
| `PopupService` | Manages a stack of popup sessions, resolves views/view models from DI, and coordinates overlay transitions via the presenter. | See `Services/UI/Popups/PopupService.cs` and `PopupOptions`. |
| `CustomShell` | Implements `IShellPresenter` and hosts the actual UI layers (pages, top/bottom bars, popup overlay, toast layer). | Delivered via MAUI page injection. Subscribes to presenter events for background and back taps. |
| `IShellPresenter` | Contract between services and the visual host. | Methods: `ShowScreen`, `ShowPopup`, `HidePopup`, `ShowToast`, `HideToast`. Also exposes events for background/back interactions. |
| `BaseContentPage` | Base class for navigable pages exposing async lifecycle hooks used by the services. | Located in `UI/Pages`. |
| `BasePopupView` / `BasicPopup` | Popup base views. `BasePopupView` is a full-screen surface; `BasicPopup` adds the centered card container used by most dialogs. | Custom popups inherit from `BasePopup` (which now derives from `BasicPopup`). |

## Navigation Workflow

1. **Request** – A caller (view model, service, or UI) resolves `INavigationService` (commonly via `ServiceRef.NavigationService`) and calls one of the async methods (`GoToAsync`, `SetRootAsync`, etc.). Optional navigation arguments are supplied via strongly-typed argument classes.
2. **Queue** – `NavigationService` enqueues the work on a `ConcurrentQueue<Func<Task>>`. A dispatcher loop (`ProcessQueueAsync`) runs on the main thread to process one navigation request at a time.
3. **Resolve** – The service resolves the target page (and optionally view model) using MAUI routing/DI, invokes `OnInitializeAsync` if required, and pushes it onto the internal stack.
4. **Render** – `NavigationService` calls `IShellPresenter.ShowScreen` with the target view. `CustomShell` performs the configured transition (fade, swipe, etc.), swaps active page slots, and updates global bars.
5. **Lifecycle** – After the transition the service dispatches `OnAppearingAsync` on the page (and binding context if applicable). When navigating away, `OnDisappearingAsync` and disposal hooks are invoked.
6. **Back Stack** – Back navigation consults the stored arguments to decide how many levels to pop, removes pages from the stack, and replays the steps above for the new top page.

### Registering Pages

Pages should be registered with MAUI routing in `MauiProgram` (or appropriate DI registration) so that `NavigationService` can instantiate them. Ensure the page derives from `BaseContentPage` to participate in the extended lifecycle.

## Popup Lifecycle

1. **Push** – Call one of `PopupService.PushAsync` overloads. The service resolves the view (and optional view model), ensures both are initialized, and pushes a new `PopupSession` onto its stack.
2. **Presenter Sync** – The service constructs a `PopupVisualState` (overlay opacity, blocking status, background tap rules) and calls `IShellPresenter.ShowPopup`. `CustomShell` animates the popup and adjusts the overlay.
3. **Lifecycle Hooks** – `OnAppearingAsync` is invoked for both view and view model. If the pop up implements `ILifeCycleView`, `OnInitializeAsync`/`OnDisposeAsync` are respected.
4. **Dismissal** – Dismissing occurs via `PopupService.PopAsync`, background tap, back button, or view-model command. The service pops the session, invokes `OnDisappearingAsync` and disposal hooks, and notifies the presenter (`HidePopup`) with the next overlay state (if any).
5. **Blocking Semantics** – When `PopupOptions.IsBlocking` is true, non-blocking popups are dismissed before showing the new one. Background taps and back button requests are ignored unless explicitly allowed in options.

## Back Handling

Hardware back handling flows through `CustomShell.OnBackButtonPressed`:

1. If a popup exists, the shell raises `PopupBackRequested`; `PopupService` checks whether the top popup allows back dismissal and pops if appropriate.
2. If no popup is open but a toast is active, the shell hides it.
3. Otherwise the shell asks `NavigationService` if it can handle back (`WouldHandleBack`). The service then checks page/view-model handlers (`IBackButtonHandler` implementations) and either consumes the event or navigates back.
4. If the stack cannot go back (root page) the event bubbles to the base implementation.

This approach keeps platform-specific back behavior consistent and ensures blocking popups remain modal until explicitly dismissed.

## Common Usage Examples

### Navigate to a Route

```csharp
public async Task OpenSettingsAsync()
{
    await ServiceRef.NavigationService.GoToAsync("settings");
}
```

### Show a Blocking Popup with View Model

```csharp
PopupOptions options = PopupOptions.CreateModal(overlayOpacity: 0.8, closeOnBackButton: false);
await ServiceRef.PopupService.PushAsync<MyModalPopup, MyModalViewModel>(options);
```

### Dismiss the Top Popup

```csharp
await ServiceRef.PopupService.PopAsync();
```

### Hook into Popup Stack Changes

```csharp
ServiceRef.PopupService.PopupStackChanged += (_, _) =>
{
    bool hasPopups = ServiceRef.PopupService.HasOpenPopups;
    this.IsBlurred = hasPopups;
};
```

## Troubleshooting Tips

- **Navigation requests overlap**: Ensure all page transitions go through `NavigationService`. Avoid calling `ShowScreen` directly on the presenter.
- **Popup stuck on screen**: Verify the popup calls `ServiceRef.PopupService.PopAsync()` (or emits a command that does). Check that `PopupOptions` allow back/background dismissal if expected.
- **Back button ignored**: Confirm the top page or view model does not always return `true` from `IBackButtonHandler.OnBackButtonPressedAsync`. For popups, confirm `CloseOnBackButton` is set in `PopupOptions`.
- **Overlay opacity incorrect**: A popup may have set a custom opacity that persists for the next popup. Ensure each popup specifies the desired `OverlayOpacity` in its options.

For deeper architectural context read [Content/architecture.md](architecture.md) and for service APIs see [Content/services.md](services.md).
