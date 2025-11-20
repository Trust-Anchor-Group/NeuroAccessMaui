# ViewSwitcher Refactor Plan

## Purpose
- Deliver a resilient view navigation control that supports dynamic data, templating, caching, and custom transitions.
- Provide a predictable API for MVVM scenarios where SelectedIndex and SelectedItem stay synchronized.
- Enable testable, composable internals that separate selection logic, view creation, and animation orchestration.
- Improve accessibility, telemetry, and error handling to production readiness.

## Current Implementation Gaps
- SelectedIndex handling is brittle and lacks SelectedItem parity or validation, allowing out-of-range selections (`NeuroAccessMaui/UI/Controls/ViewSwitcher.cs:153`).
- UpdateContent is async void with manual state flags, risking uncaught exceptions, race conditions, and leaked transitions (`NeuroAccessMaui/UI/Controls/ViewSwitcher.cs:132`).
- Inline view disposal and template instantiation happen on every switch, causing unnecessary allocations and unsafe disposal (`NeuroAccessMaui/UI/Controls/ViewSwitcher.cs:215`).
- OnPropertyChanged triggers for multiple properties, making it hard to reason about updates and preventing collection change responsiveness (`NeuroAccessMaui/UI/Controls/ViewSwitcher.cs:98`).

## Refactor Objectives
- Define a clear contract for selection, navigation, and transition events with cancellation support.
- Provide consistent templating via DataTemplate or DataTemplateSelector, plus optional view factory delegates.
- Support collection change notifications, virtualization, and view caching to avoid re-creating views.
- Expose pluggable transition strategies with async cancellation and easy unit testing.
- Integrate with existing infrastructure such as PolicyRunner, ObservableTask, and ILifeCycleView where appropriate.

## Target Architecture
- ViewSwitcher (public control) focuses on dependency properties, command wiring, and orchestrating internal services.
- SelectionState tracks SelectedIndex, SelectedItem, SelectedStateKey, and SelectedIndexBehavior, raising SelectionChanging and SelectionChanged notifications while maintaining guard flags across properties.
- ViewFactory handles DataTemplate, DataTemplateSelector, Func<object, View>, inline/state view conversion, and caching keyed by item identity, index, or state key.
- TransitionCoordinator coordinates view replacement, uses IViewTransition, and manages CancellationTokenSource with PolicyRunner for resilience.
- VirtualizationManager defers view creation until needed and disposes or reuses views based on CacheViews and virtualization mode.
- LifecycleAdapter wires ILifeCycleView for views or view models to ensure appearing/disappearing notifications.

## Public API Surface
- **BindableProperty SelectedIndex**: int default -1; TwoWay; validates via SelectedIndexBehavior.
- **BindableProperty SelectedItem**: object default null; TwoWay; synchronized with SelectedIndex using guard flags.
- **BindableProperty ItemsSource**: IEnumerable; subscribes to INotifyCollectionChanged and clamps selection.
- **BindableProperty ItemTemplate**: DataTemplate; used when selector not provided.
- **BindableProperty ItemTemplateSelector**: DataTemplateSelector; falls back to ItemTemplate; supports Func<object, View> via BindableProperty or property wrapper.
- **BindableProperty StateViews**: ObservableCollection<ViewSwitcherStateView>; declarative inline entries that mirror StateContainer state declarations; supports x:FactoryMethod and view/viewmodel instantiation.
- **BindableProperty SelectedStateKey**: string default null; TwoWay; syncs with SelectedIndex/SelectedItem and drives state-key-based selection.
- **BindableProperty CacheViews**: bool default true; retains view instances per item or index.
- **BindableProperty VirtualizationMode**: enum { Immediate, Lazy, DisposeOnDeselect }; controls creation/disposal policy.
- **BindableProperty SelectedIndexBehavior**: enum { Clamp, Ignore, Wrap, Throw }; governs out-of-range handling.
- **BindableProperty Transition**: IViewTransition default CrossFadeTransition; configures animation strategy.
- **BindableProperty TransitionDuration**: TimeSpan or uint; still exposed for convenience; forwarded to transition if applicable.
- **BindableProperty TransitionEasing**: Easing; optional; forwarded to transition.
- **BindableProperty Animate**: bool; shortcuts to Transition = NoOp when false.
- **BindableProperty AutomationDescriptionTemplate**: string or Func<object, string>; informs Semantics.Description.
- **ICommand NextCommand/PreviousCommand**: support manual navigation; respect Wrap behavior; expose CanExecute based on selection state.
- **Event SelectionChanging**: provides old/new index and item with cancellation token or Cancel flag; runs before transition.
- **Event SelectionChanged**: fires after transition completes; supplies old/new data.
- **Task SwitchToAsync(int index, bool animate = true, CancellationToken token = default)**: public entry to trigger navigation programmatically.
- **Action<Exception> OnTransitionException**: optional hook for logging or fallback; defaults to PolicyRunner logging.

## Internal Components
- **SelectionState**: pure class encapsulating index/item/state-key sync, SelectedIndexBehavior, and wrap/clamp logic; exposes ObservableTask for async transitions.
- **ViewCache**: dictionary keyed by item reference, index, or state key (strategy selectable); respects CacheViews and virtualization; clears BindingContext when releasing a view.
- **DefaultViewFactory**: resolves DataTemplate, DataTemplateSelector, or Func<object, View>; integrates PolicyRunner for template instantiation policies.
- **TransitionCoordinator**: orchestrates old/new view swap, invokes IViewTransition.RunAsync, resolves state-key descriptors, and manages a CancellationTokenSource replaced per switch; uses ObservableTask to expose in-progress transition state.
- **LifecycleAdapter**: checks ILifeCycleView on view and BindingContext, forwarding appearing/disappearing events when transitions complete or cancel.
- **AsyncCommandAdapter**: reuses ObservableTask to surface NextCommand and PreviousCommand with automatic disable while transition is running.

## Data and State Flow
- SelectedIndex/SelectedItem/SelectedStateKey setters call into SelectionState; invalid inputs adjusted per SelectedIndexBehavior before raising SelectionChanging and keeping the triad synchronized without recursion.
- SelectionChanging obtains a CancellationTokenSource; external handlers can cancel before work begins.
- TransitionCoordinator cancels any in-flight transition using the stored token; new transition awaits PolicyRunner.ExecuteAsync to honor retry or fallback policies.
- On transition success, CurrentView is updated, view cache notified, and SelectionChanged raised; AutomationId and Semantics refreshed to reflect SelectedItem.

## Transition Execution
- IViewTransition defines `Task RunAsync(View oldView, View newView, TransitionRequest context, CancellationToken token)`.
- Built-in transitions include NoOp, CrossFade (Task.WhenAll fade), Slide, and custom injection (e.g., hooking to platform-specific animations).
- TransitionRequest carries duration, easing, host layout, and whether control is loaded; transitions skip animations when not yet loaded.
- TransitionCoordinator ensures new view is added before removing old view to avoid blank states; uses simultaneous fade when possible; handles state-key lookups so declarative StateViews participate in the same pipeline.

## Collection and Virtualization Handling
- ItemsSource changes detach/attach INotifyCollectionChanged; SelectedIndex adjusted on add/remove/reset to maintain valid selection.
- Inline `Views` property upgraded to ObservableCollection<View> with CollectionChanged subscription to refresh automatically.
- `StateViews` property exposes ObservableCollection<ViewSwitcherStateView>; map builds Key → descriptor and updates selection when entries change.
- VirtualizationMode controls whether views are created immediately, lazily on first selection (including StateViews), or disposed when not visible (unless cached).
- ViewCache invalidates entries when ItemsSource resets or StateViews are replaced; optionally exposes `Func<object, object>` key selector for custom identity.

## Threading and Asynchrony
- Switch logic exposed as async Task returning transitions; UpdateContent replacement becomes `Task SwitchAsync(SelectionRequest request, CancellationToken token)`.
- CancellationTokenSource replaced atomically via Interlocked.Exchange; prior token canceled to avoid overlapping transitions.
- ObservableTask used to expose `CurrentTransition` for diagnostics and to keep commands in sync.
- PolicyRunner wraps transition execution to ensure exceptions surface through OnTransitionException and telemetry.

## Accessibility, Telemetry, and Diagnostics
- Semantics.Description updated using AutomationDescriptionTemplate or SelectedItem.ToString; AutomationId derived from control’s x:Name.
- SelectionChanged raises UI Automation events via `SemanticScreenReader.Announce`.
- TransitionCoordinator logs start/end/cancel events with structured data through existing logging infrastructure.
- Diagnostics flag enables trace logging with minimal closures to avoid allocations.

## Usage Examples
```xml
<controls:ViewSwitcher x:Name="Wizard"
                       ItemsSource="{Binding Steps}"
                       SelectedItem="{Binding CurrentStep, Mode=TwoWay}"
                       ItemTemplateSelector="{StaticResource StepTemplateSelector}"
                       CacheViews="True"
                       SelectedIndexBehavior="Clamp"
                       Transition="{StaticResource SlideTransition}"
                       AutomationDescriptionTemplate="Step: {0}" />
```

```csharp
await this.Wizard.SwitchToAsync(targetIndex, animate: true, this.cts.Token);
```

```xml
<controls:ViewSwitcher x:Name="EnrollmentFlow"
                       SelectedStateKey="{Binding CurrentState}"
                       CacheViews="False"
                       Animate="True">
    <controls:ViewSwitcher.StateViews>
        <controls:ViewSwitcherStateView StateKey="Loading"
                                        ViewType="{x:Type views:LoadingView}"
                                        x:FactoryMethod="Create" />
        <controls:ViewSwitcherStateView StateKey="GetStarted">
            <views:GetStartedView />
        </controls:ViewSwitcherStateView>
        <controls:ViewSwitcherStateView StateKey="NameEntry"
                                        ViewModelType="{x:Type viewModels:NameEntryViewModel}" />
    </controls:ViewSwitcher.StateViews>
</controls:ViewSwitcher>
```

## Implementation Roadmap
1. Introduce foundational types (SelectedIndexBehavior, ViewSwitcherSelectionChangedEventArgs, ViewSwitcherStateView, IViewTransition, transitions).
2. Build SelectionState, ViewCache, DefaultViewFactory, and TransitionCoordinator with unit-testable logic (including state-key lookup tables).
3. Refactor ViewSwitcher to delegate to new components, wire dependency properties, commands, and events.
4. Integrate collection change tracking, virtualization, and caching policies; ensure ILifeCycleView invocation pipeline.
5. Add accessibility, diagnostics, and PolicyRunner/ObservableTask hooks; document usage and migration guidance.

## Open Questions
- Should SelectedItem set to a non-existing object inject it into cache or fall back to nearest match?
- Do we expose PolicyRunner configuration on the control or rely on a global singleton?
- How aggressively should we dispose views when CacheViews is false for templates that pool platform resources?
- Are there platform-specific transitions we must ship out-of-box or rely on app-specific implementations?
- Does virtualization need paging support for very large data sets or is per-item lazy creation sufficient for current requirements?
- Should StateView entries support view-model-only declarations or DI service resolution when ViewType not supplied?
