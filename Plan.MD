# Custom Popup Migration Plan

## Overview
- Replace Mopups- and Shell-dependent popups with an in-house popup stack that plugs into the new `CustomShell`/`NavigationService` pipeline.
- Preserve existing MVVM patterns, async lifecycles, and API ergonomics while tightening control over navigation, transitions, and platform quirks.
- Deliver a reusable foundation that other UI components (e.g., toasts, lightweight banners) can build on without reintroducing third-party dependencies or relying on the deprecated `UiService`.

## Current State Assessment
### Legacy popup system (UiService + Mopups)
- `UiService` orchestrates alerts, prompts, popups, and navigation through Mopups `PopupPage` types; it exposes numerous overloads returning `Task`/`Task<T>` to integrate with MVVM workflows.
- `BasePopup` supplies shared UI (centered card, optional blurred background, background tap close, animations) and expects `BasePopupViewModel` derivatives with async lifecycle hooks (`OnAppearingAsync`, `OnDisappearingAsync`, `OnPop`).
- Hardware back and background tap route to `UiService.PopAsync`; Mopups manages stacking, transitions, and cross-platform quirks.
- Popups are consumed broadly (XmppService, SelectPhoneCode, Report/Block flows, etc.), and many services depend on `UiService` as the single façade for UI tasks.

### New navigation stack
- `CustomShell` exposes modal presentation via `modalOverlay`/`modalHost` and animated `ShowModal`/`HideTopModal` methods.
- `NavigationService` tracks page and modal stacks, serializes work via a queue, and integrates with `IShellPresenter` for visuals.
- No first-class concept yet for lightweight popups or transient notifications; Mopups remains to fill that gap, and `UiService` still exists primarily for legacy compatibility.

## Goals & Requirements
### Functional
- Provide `PushPopupAsync`/`PopPopupAsync` APIs mirroring existing `UiService` overloads (view/viewmodel factories, return values) while routing through a dedicated popup service.
- Support stacked popups with predictable LIFO behavior and deterministic completion tasks when popped.
- Allow declarative popup definitions in XAML using MVVM (no code-behind logic), with reusable base layout and theming support.
- Offer configurable transitions (at least fade + scale-in) and background dimming options (darkened overlay or transparent). Blurred screenshot backgrounds are **explicitly out of scope** going forward.
- Integrate toast/notification primitives alongside popups: lightweight, non-blocking overlays that auto-dismiss, can coexist with popup stack, and can queue multiple messages.

### Non-Functional
- Run all presentation changes on the main thread without blocking; reuse queueing patterns already in `NavigationService`.
- Remove the `UiService` dependency surface; introduce targeted services (`PopupService`, `ToastService`, etc.) registered in DI and exposed via `ServiceRef`.
- Maintain cross-platform parity (Android, iOS, Windows) without adding new native dependencies or platform-specific libraries.
- Ensure popup/toast APIs are testable and do not require UI thread during unit tests (mock `IShellPresenter`).

### Integration Constraints
- Must coexist with `NavigationService` queueing to avoid race conditions with page navigation.
- `CustomShell` remains the single presenter; popups cannot assume a `NavigationPage` host or Mopups behaviors.
- Toasts and multiple popups must share the same overlay surface without starving each other or blocking page interactions unexpectedly.

## Proposed Architecture
### Popup primitives
- Introduce `BasePopupView : ContentView` encapsulating shared layout (darkened backdrop, centered content container, optional close-on-backdrop) and simple animation hooks.
- Retain `BasePopupViewModel`/`ReturningPopupViewModel<T>` semantics but decouple from Mopups types; extend with `TaskCompletionSource` for close signals and optional metadata (transition, allowBackgroundDismiss).
- Define a `PopupOptions` DTO capturing dimming preference (dark/transparent), transition type, stacking behavior, and input blocking.

### CustomShell presenter extensions
- Extend `CustomShell` with dedicated layers:
  - `popupOverlay`: covers the shell content with a semi-transparent dark background; manages pointer interception.
  - `popupHost`: hosts one or more popup containers (`PopupContainer`), supporting stacking and z-ordering.
  - `toastLayer`: hosts transient toast banners positioned (top/bottom) independently of the popup stack.
- Expand `IShellPresenter` to include `ShowPopup`, `HideTopPopup`, `ShowToast`, `HideToast`, `PopupBackgroundTapped`, and `PopupBackRequested` events.
- Background taps should trigger delegate events so `PopupService` can decide whether to close the current popup based on options.

### PopupService (new)
- Singleton `PopupService : IPopupService` responsible for:
  - Managing a stack of `PopupSession` objects (view/viewmodel/options/completion source).
  - Public async APIs mirroring legacy overloads but renamed to avoid confusion (`PushPopupAsync<TPage,...>` etc.).
  - Serializing operations through a dedicated main-thread queue (shared helper with `NavigationService` or a reusable `UiExecutionQueue`).
  - Hooking into `IShellPresenter.PopupBackgroundTapped` and navigation back handling to close top popup when permitted.
  - Applying lifecycle bridging: call `OnInitializeAsync`, `OnAppearingAsync`, `OnDisappearingAsync`, and `OnDisposeAsync` on both view and viewmodel.
  - Cleaning up resources (event handlers, disposable viewmodels) on pop completion.

### ToastService (new)
- `ToastService : IToastService` manages non-modal notifications:
  - Queue multiple toast requests with duration, priority, placement (top/bottom), and optional actions.
  - Auto-dismiss after timeout while ensuring only one toast animates at a time; support manual dismissal when tapped.
  - Integrate with `CustomShell` toast layer animations (slide/fade) and maintain separation from popup stack logic.

### NavigationService integration
- Update `NavigationService.HandleBackAsync` to consult `PopupService` first (close popups before modals/pages) and optionally coordinate with toasts (e.g., dismiss current toast on back if no popups).
- Ensure navigation queue and popup queue share synchronization to prevent orchestrated actions from interleaving unsafely (e.g., page navigation while popup animating out).

### Service refactoring
- Deprecate `UiService` entirely:
  - Extract alert/prompt functionality into `DialogService` (built on the new popup infrastructure or native dialogs).
  - Route existing callers to new services via DI updates and `ServiceRef` helpers.
  - Remove Mopups-specific code paths, types, and package references once all consumers migrate.
- Provide temporary adapter layer (`LegacyUiFacade`) only if necessary for incremental migration; plan for its removal before final release.

### UI composition & styling
- Provide default styles for popup cards (rounded corners, elevation, theme-aware colors) using MAUI resource dictionaries.
- Offer per-popup configuration for width/height, content alignment, and overlay darkness (e.g., `Dark`, `Light`, `None`).
- Ensure accessibility: focus trapping within popup, screen-reader announcements, and toast accessibility text.

### Transition engine
- Define `PopupTransition { None, Fade, Scale, SlideUp }` with animation helpers executed inside `CustomShell`.
- Support stacking animations (e.g., new popup scales in while dim layer already visible) and smooth dismissal when multiple popups are queued.
- Provide parallel transition set for toasts (`ToastTransition { SlideFromTop, SlideFromBottom, Fade }`).

## Migration Strategy
1. **Scaffold services**: implement `IPopupService`, `IToastService`, and `IDialogService`, register them in DI, and expose through `ServiceRef`.
2. **Extend presenter**: modify `CustomShell`/`IShellPresenter` to host popup/toast layers and expose new methods/events.
3. **Adapter phase**: introduce shims so existing Mopups-based popups can run through the new presenter while we incrementally port their XAML and viewmodels.
4. **Popup conversion**: port shared popup base (layout + VM) to the new system, then migrate feature-specific popups (Xmpp subscription, report/block, SelectPhoneCode, etc.).
5. **Toast rollout**: replace any ad-hoc toasts/snackbars with `ToastService`; ensure new service handles multi-toast queuing.
6. **Remove UiService**: after all consumers migrate, delete `UiService` and `IUiService`, purge Mopups package/usings, and drop related platform workarounds.
7. **Validation**: run manual flows on Android/iOS/Windows covering stacked popups, toast interactions, back-button behavior, and navigation interactions; update documentation and onboarding guides.

## Limitations & Mitigations
- **Overlay dimming only**: without blurred screenshots, some legacy UX expectations change. *Mitigation*: provide adjustable opacity and optional backdrop imagery to preserve depth perception.
- **Transition performance**: custom animations may stutter on low-end devices. *Mitigation*: keep animations lightweight (opacity/scale only) and allow opting out per popup/toast.
- **Thread marshaling**: incorrect main-thread handling can crash the app. *Mitigation*: centralize dispatch logic and add debug assertions when operations execute off-thread.
- **Back-button conflicts**: simultaneous modal/page pop and popup/toast dismissal can cause double handling. *Mitigation*: clearly define precedence (toast → popup → modal → page) and guard via `PopupService.IsHandlingBack` flags.
- **Manual lifecycle cleanup**: removing Mopups requires explicit disposal. *Mitigation*: encapsulate cleanup in `PopupSession.DisposeAsync()` and enforce via unit tests.
- **Testing gap**: presenter-level UI remains hard to automate. *Mitigation*: add unit tests for service queues/options and create a manual regression checklist for QA.

## Open Questions
- Should toasts pause when a popup is active or continue to display concurrently? (Impacts toast layer z-order and focus rules.)
- Do we need additional overlay types (bottom sheets, inline banners) beyond popups/toasts, and can they reuse the same infrastructure?
- Are there scenarios where multiple popups should be visible simultaneously (e.g., nested dialogs), or should stacking enforce single-visible policy with queueing?
- How do we expose configuration (animations, overlay opacity) to feature teams—via resource dictionaries, options objects, or both?

## Implementation Tickets

### POP-001 – Extend CustomShell Presenter Layers
- **Objective**: Introduce popup and toast overlays, background dimming, and presenter events required by the new services.
- **Scope**:
  - Add `popupOverlay`, `popupHost`, and `toastLayer` elements to `CustomShell`.
  - Update `IShellPresenter` interface with new popup/toast methods and events.
  - Implement animations for popup and toast transitions.
- **Dependencies**: Existing `CustomShell` implementation, `NavigationService`.
- **Acceptance Criteria**: `CustomShell` can display placeholder popup/toast content via presenter APIs; background dimming toggles correctly; no regressions to page navigation.

### POP-002 – Implement PopupService & IPopupService Contract
- **Objective**: Provide a new service managing popup stack, lifecycle, and back/overlay interactions.
- **Scope**:
  - Define `IPopupService` interface with async push/pop overloads, options, and stack inspection APIs.
  - Implement `PopupService` with internal queueing, lifecycle management, and presenter integration.
  - Handle background tap/back button events according to popup options.
- **Dependencies**: POP-001 presenter extensions, existing viewmodel base classes.
- **Acceptance Criteria**: Service unit tests (or mocks) demonstrate stacking, completion, and lifecycle calls; manual smoke test shows popups opening/closing through the presenter.

### POP-003 – Rebuild Base Popup Components
- **Objective**: Replace Mopups-based `BasePopup` with MAUI `ContentView` equivalents compatible with `PopupService`.
- **Scope**:
  - Create `BasePopupView`, `PopupContainer`, and updated resource templates.
  - Update `BasePopupViewModel`/`ReturningPopupViewModel<T>` to remove Mopups dependencies and expose new options (transition, dismiss behaviors).
  - Ensure accessibility (focus trap, screen reader announcements) and theming support.
- **Dependencies**: POP-002 for service integration.
- **Acceptance Criteria**: Converted base popup renders via demo page; lifecycle hooks fire; background dimming configurable.

### POP-004 – ToastService & Notification Layer
- **Objective**: Deliver transient notification infrastructure decoupled from popups.
- **Scope**:
  - Define `IToastService` with queueing, duration, placement, and action support.
  - Implement service using `CustomShell` toast layer; provide default toast styles and animations.
  - Add cancellation/back-dismiss support and ensure compatibility with popup presence.
- **Dependencies**: POP-001 for toast host, shared queue helper if needed.
- **Acceptance Criteria**: Multiple toasts queue and display sequentially; manual tests confirm dismissal on timeout and optional tap action.

### POP-005 – DialogService & Alert/Prompt Replacement
- **Objective**: Migrate alert/prompt flows off `UiService` onto new infrastructure (popups or native dialogs).
- **Scope**:
  - Define `IDialogService` covering alerts, prompts, confirmations.
  - Implement using `PopupService` templates or platform dialogs, maintaining existing localization.
  - Update call sites currently using `UiService.DisplayAlert/DisplayPrompt`.
- **Dependencies**: POP-002/003 for popup templates; localization resources.
- **Acceptance Criteria**: Alerts/prompts operate via new service with identical UX and translations; automated tests updated if applicable.

### POP-006 – Navigation Back-Handling Integration
- **Objective**: Align `NavigationService` with popup/toast precedence and ensure orderly back-button handling.
- **Scope**:
  - Update `NavigationService.HandleBackAsync` to delegate to `PopupService`/`ToastService` before modal/page logic.
  - Ensure queue coordination between navigation, popups, and toasts.
  - Add logging/diagnostics for back handling outcomes.
- **Dependencies**: POP-002, POP-004.
- **Acceptance Criteria**: Back button closes toasts first, then popups, then navigates; no race conditions observed during manual stress tests.

### POP-007 – Popup Migration (Feature Modules)
- **Objective**: Convert all Mopups-based popups to new system.
- **Scope**:
  - Migrate shared templates and individual popups (Xmpp, SelectPhoneCode, ReportType, etc.) to `BasePopupView`.
  - Update viewmodels to rely on `PopupService` APIs.
  - Remove Mopups namespaces and package references from migrated files.
- **Dependencies**: POP-002/003 complete; POP-005 for dialogs.
- **Acceptance Criteria**: All popups function via new service in staging build; no Mopups usage remains in feature folders.

### POP-008 – UiService Decommissioning & Cleanup
- **Objective**: Fully remove `UiService`, `IUiService`, and Mopups dependency.
- **Scope**:
  - Remove service registrations, helper methods, and platform-specific workarounds (e.g., iOS AppDelegate comments).
  - Delete Mopups package reference and unused base classes.
  - Update documentation and release notes.
- **Dependencies**: POP-002 through POP-007 complete.
- **Acceptance Criteria**: Solution builds without Mopups; DI graph contains new services only; documentation reflects new architecture.

### POP-009 – QA & Regression Validation
- **Objective**: Validate new popup/toast system across platforms.
- **Scope**:
  - Create manual QA checklist (Android/iOS/Windows) covering stacked popups, toasts, back button, orientation changes, and accessibility.
  - Execute tests or coordinate with QA team; file follow-up bugs if issues found.
  - Capture performance benchmarks (animation smoothness) for reference.
- **Dependencies**: Prior tickets (POP-001…POP-008) complete.
- **Acceptance Criteria**: QA sign-off with documented results; no critical regressions outstanding.

### POP-010 – Documentation & Developer Enablement
- **Objective**: Provide guidance for feature teams adopting the new popup/toast services.
- **Scope**:
  - Update internal docs (e.g., `AGENTS.md`, onboarding guides) with usage patterns, style references, and migration tips.
  - Supply sample code snippets and troubleshooting notes.
  - Host knowledge-sharing session or record walkthrough if needed.
- **Dependencies**: Core services (POP-001…POP-004) ready.
- **Acceptance Criteria**: Docs published; developers can build new popups/toasts without referencing legacy services.
