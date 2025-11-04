# Persistent Navigation Bars – Technical Plan

## Current State Assessment
- `CustomShell` already reserves `topBar` and `navBar` slots (`NeuroAccessMaui/Services/UI/CustomShell.cs:62`) and exposes `UpdateBars` to swap their `ContentView` contents when a page implementing `IBarContentProvider` becomes active.
- Visibility is controlled through the `NavigationBars` attached properties, but there is no built-in mechanism for pages to declare *which* bar to use, nor a persistence model for reusing an existing instance across navigation.
- `BottomBar` (`NeuroAccessMaui/UI/Controls/BottomBar.cs`) implements a three-tab layout with command bindings, but it is internal, not easily templated, and tightly coupled to a fixed icon/label count.
- `NavigationService` orchestrates page transitions and delegates to `IShellPresenter.UpdateBars`, so any solution must operate within this pipeline without blocking the queued navigation tasks or breaking the keyboard/safe-area adjustments `CustomShell` already handles.

## Technical Requirements
- Allow every `BaseContentPage` to declare (in XAML) the identifier of the navigation bar it wants, without imperatively creating controls in code-behind.
- Support multiple bar shapes (bottom tab bars, segmented headers, contextual action bars) with reusable styling and MVVM-friendly bindings.
- Enable persistence of bar instances across compatible pages so animations, selection state, and badge counts survive navigation where desired.
- Provide smooth animated transitions when swapping bars (fade/slide/morph) while staying on the main thread and avoiding heavy layout churn.
- Integrate with existing safe-area/keyboard inset logic so bars respect padding adjustments handled by `CustomShell`.
- Keep the architecture testable: ensure descriptor resolution and page-to-bar negotiation can be mocked without rendering the full visual tree.
- Maintain localisation/theming by sourcing text and styles from resources rather than hard-coded strings.

## Architectural Options
- **Option A – DataTemplate Registry**  
  Register navigation-bar `DataTemplate` instances in a resource dictionary keyed by name. Pages declare `shell:NavigationBars.NavigationBarKey="MainTabs"` and the shell instantiates the template when entering. Pros: simple XAML story, designer-friendly. Cons: template instantiation produces a new bar each time; persistence requires additional caching logic.
- **Option B – Descriptor Service + Dependency Injection**  
  Define `NavigationBarDescriptor` objects (control factory, desired persistence mode, animation profile). Register them in DI. Pages declare a descriptor key; the shell asks a `INavigationBarRegistry` to provide the descriptor. Pros: centralised lifecycle control, easy to cache instances or supply platform variants. Cons: more ceremony, developers must register descriptors in code.
- **Option C – Page-Provided Provider Interface**  
  Continue with `IBarContentProvider`, but move instantiation into page-level partial classes or view models. Pros: minimal shell changes. Cons: encourages imperative code-behind, duplicates layout logic, and makes reuse/persistence harder.

## Recommended Direction
- Combine **Option A** for declarative XAML with **Option B** for lifecycle control: pages set an attached property with a descriptor key; the shell resolves it through a registry that decides whether to reuse an existing control or instantiate from a `DataTemplate`.
- Introduce a new `NavigationBarHost` abstraction managing stateful instances and exposing async `SetBarAsync` with animation hooks. The host will live inside `CustomShell` and drive transitions for `navBar`.
- Standardise bar definitions through `NavigationBarDescriptor` (key, `DataTemplate`/factory, persistence policy, animation profile, optional view-model binding strategy).

## Implementation Plan
Assumes the shared animation infrastructure described in `AnimationPlan.md` is available (registry, coordinator, reusable profiles).

1. **Navigation Bar Contracts**
   - Create `NavigationBarDescriptor`, `NavigationBarPersistence` enum, and `INavigationBarRegistry`.
   - Extend `NavigationBars` with attached property `NavigationBarKey` and optional `NavigationBarStateBinding` for declarative state hand-off (e.g., selected tab index).

2. **Registry & Configuration**
   - Implement `NavigationBarRegistry` resolving descriptors from resource dictionaries and DI registrations; support lazy creation and caching for persistent bars.
   - Provide helper markup extension `<bars:NavigationBarResource Key="..."/>` to streamline registration in XAML resource dictionaries when needed.

3. **Control Abstractions**
   - Build a reusable `TabbedNavigationBar : ContentView` accepting an `IReadOnlyList<NavigationTabItem>` plus `SelectedIndex`, command bindings, and badge/notification hooks.
   - Factor existing `BottomBar` logic into this control (or wrap it) to preserve visual styling while making item count/configuration flexible.

4. **Shell Host Integration**
   - Add `NavigationBarHost` component inside `CustomShell` to manage current descriptor, active control instance, and animation pipeline.
   - Update `CustomShell.UpdateBars` to resolve the descriptor key from the active page (attached property or interface) and call `NavigationBarHost.SetBarAsync`.
   - Ensure safe-area and keyboard insets continue to run through `SetViewPadding`, and cache per-bar padding if persistence is enabled.

5. **Navigation Service Wiring**
   - When `NavigationService` activates a page, propagate navigation arguments (selected tab, etc.) to the bar via binding context or a shared `INavigationBarContext`.
   - Expose extension points so view models can update the bar state using a mediator (`INavigationBarCoordinator`).

6. **Animation & UX**
   - Resolve animation profiles via `AnimationRegistry` keys (e.g., `NavigationBars.SwitchCrossFade`) and execute them through `AnimationCoordinator` to ensure consistent behavior with `CustomShell` and `ViewSwitcher`.
   - Allow descriptors to specify animation keys per region plus optional overrides for reduced-motion scenarios; default to a subtle crossfade paired with slide derived from shared profiles.

7. **Developer Ergonomics**
   - Document how to register bars (DI vs resource dictionary), how to declare them on pages, and how to share state via binding.
   - Provide starter descriptors for common scenarios (main tabs, wizard steps) and sample page XAML demonstrating attached property usage.

## Technical Considerations
- Instance lifetime: persistent bars should detach from visual tree without disposing their view models; ensure bindings are updated when bar re-attaches.
- Threading: descriptor resolution occurs during navigation queue execution; UI changes must be dispatched to the main thread.
- Accessibility: ensure tab bars expose proper semantic descriptions (`SemanticProperties.Description`) and keyboard navigation (Windows) remains viable.
- Reduced motion: honour any global animation settings by resolving alternate profiles from the shared animation registry when `AnimationCoordinator` indicates motion should be minimized.
- Testing: registry and host should be injectable; add unit tests for descriptor resolution and persistence rules (without creating actual views).
- Performance: cache measurement results for persistent bars to reduce layout thrashing during repeated attachments.

## Open Questions
- How should page-specific state (e.g., which tab is selected) flow into a persistent bar—through bound view models, attached properties, or a shared coordinator service?
- Do we need multiple concurrent bar regions (e.g., top + bottom) with independent descriptors, or is the initial scope limited to the bottom bar?
- Should bar descriptors be reloadable at runtime (e.g., feature flags, theming), and if so, how do we invalidate cached instances safely?
- What animation profile feels appropriate across Android, iOS, and Windows—do we need platform-specific defaults?
- Are there scenarios where a page needs to *opt out* of any bar (full-screen flows), and if so, should we standardise a `NavigationBars.NavigationBarKey="None"` convention?

## Goal XAML Examples
- **Descriptor Registration (AppShell or global resources)**
  ```xaml
  <ResourceDictionary
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:bars="clr-namespace:NeuroAccessMaui.Services.UI.NavigationBars"
      xmlns:controls="clr-namespace:NeuroAccessMaui.UI.Controls">
      <bars:NavigationBarDescriptor
          x:Key="MainTabsDescriptor"
          Persistence="SharedInstance"
          Region="Bottom"
          TransitionProfile="{bars:NavigationBarTransition FadeSlideUp}">
          <bars:NavigationBarDescriptor.ContentTemplate>
              <DataTemplate>
                  <controls:TabbedNavigationBar
                      ItemsSource="{Binding Tabs}"
                      SelectedIndex="{Binding SelectedIndex, Mode=TwoWay}"
                      TabSelectedCommand="{Binding TabSelectedCommand}" />
              </DataTemplate>
          </bars:NavigationBarDescriptor.ContentTemplate>
      </bars:NavigationBarDescriptor>
  </ResourceDictionary>
  ```

- **Page Declaration**
  ```xaml
  <views:DashboardPage
      x:Class="NeuroAccessMaui.UI.Pages.DashboardPage"
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:vm="clr-namespace:NeuroAccessMaui.UI.ViewModels"
      xmlns:shell="clr-namespace:NeuroAccessMaui.Services.UI"
      shell:NavigationBars.NavigationBarKey="MainTabsDescriptor"
      shell:NavigationBars.NavigationBarState="{Binding NavigationBarState}">
      <views:DashboardPage.BindingContext>
          <vm:DashboardViewModel />
      </views:DashboardPage.BindingContext>
      <!-- Page content -->
  </views:DashboardPage>
  ```

- **Type-Based Shortcut (alternative)**
  ```xaml
  <views:SettingsPage
      ...
      shell:NavigationBars.NavigationBarType="{x:Type bars:SettingsNavigationBar}"
      shell:NavigationBars.NavigationBarPersistence="PerPage">
      <!-- Content -->
  </views:SettingsPage>
  ```
