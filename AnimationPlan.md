# Animation Infrastructure Plan

## Current Pain Points
- `CustomShell` hard-codes fade and translate sequences for page swaps, popup presentation, and toast banners (`NeuroAccessMaui/Services/UI/CustomShell.cs:156-385`). Parameters are duplicated and updates require editing multiple call sites.
- `ViewSwitcher` introduces its own transition abstractions (`IViewTransition`, `ViewSwitcherTransitionCoordinator`) that are not reusable elsewhere.
- There is no shared catalog of animation profiles, so teams cannot reference common motion patterns from XAML or configuration.
- Manual animation wiring complicates testing, cancellation, and future visual refreshes (e.g., platform-specific easing or duration adjustments).

## Animation Goals
- Centralise animation definitions in an `Animations` module (folder) with reusable objects that encapsulate easing, duration, and targeted properties.
- Provide a registry-based lookup so shell navigation, popups, toasts, and future navigation bars request animations by key or descriptor.
- Support composite transitions (enter/exit pairs, staggered sequences) with cancellation and guaranteed final-state assignment.
- Allow configuration overrides per platform or theme without touching consumer code paths.
- Keep execution on the main thread and leverage existing MAUI primitives (`ViewExtensions`, `Animation`) while abstracting orchestration.

## Architectural Overview
- **Descriptors vs Executors**  
  - `AnimationDescriptor` records capture immutable configuration (key, base duration, easing, property targets, platform overrides).  
  - Executors (`IViewAnimation`, `ITransitionAnimation`) perform the work. Factories translate descriptors into executors at run time so configuration can come from code, resources, or future JSON.

- **Key Management**  
  - Keys use a lightweight value type (`AnimationKey`) to avoid free-form strings. A central static class exposes known keys (e.g., `AnimationKeys.Shell.PageCrossFade`).  
  - `AnimationRegistry` resolves descriptors via hierarchical lookup: `<Key>.<Platform>.<Theme>`, `<Key>.<Platform>`, then `<Key>`. Callers can also request `TryGet` variants for graceful fallback.

- **AnimationRegistry**  
  Singleton service exposing descriptor lookup and factory services:
  ```csharp
  IViewAnimation CreateViewAnimation(AnimationKey key, IAnimationContext context);
  ITransitionAnimation CreateTransition(AnimationKey key, IAnimationContext context);
  ```
  The registry seeds defaults during startup and allows DI overrides or runtime profile updates.

- **AnimationCoordinator**  
  Facade consumed by UI components (`CustomShell`, `ViewSwitcher`, `TabbedNavigationBar`). Responsibilities:
  - Dispatch execution to the main thread.
  - Coordinate cancellation via token-aware helpers (wrapping `ViewExtensions`/`Animation` APIs).
  - Emit telemetry events (start/stop/cancel/error) without enforcing a logging backend.
  - Honour motion settings (reduce-motion, duration overrides) provided through execution options.

- **Animation Definitions**  
  Located in `NeuroAccessMaui/Animations/`:
  - Primitive executors (`FadeAnimation`, `TranslateAnimation`, `ScaleAnimation`, `OpacityAnimation`).
  - `CompositeViewAnimation` with explicit composition mode (parallel/sequential) and optional per-child delays for staggered effects.
  - `TransitionAnimation` composing enter/exit/overlay (`IViewAnimation`) with minimal glue logic.
  - Descriptor builders (`FadeAnimationDescriptor`, `TransitionDescriptorBuilder`) simplifying strongly-typed configuration.

- **Integration Contracts**  
  - `IAnimationContext`: exposes platform, theme, safe-area, keyboard inset, and `ReduceMotion` flags sourced from OS + app settings.  
  - `AnimationOptions`: per-invocation overrides (duration tweak, force motion, completion callback).  
  - `IAnimationKeyProvider` (optional) for controls that derive animation keys dynamically from state.

## Core Interfaces
```csharp
public readonly record struct AnimationKey(string Value)
{
    public override string ToString() => this.Value;
}

public sealed class AnimationOptions
{
    public TimeSpan? DurationOverride { get; init; }
    public bool ForceMotion { get; init; }
    public Action? Completed { get; init; }
}

public interface IViewAnimation
{
    Task RunAsync(
        VisualElement target,
        IAnimationContext context,
        AnimationOptions? options,
        CancellationToken token);
}

public interface ITransitionAnimation
{
    Task RunAsync(
        VisualElement? entering,
        VisualElement? exiting,
        IAnimationContext context,
        AnimationOptions? options,
        CancellationToken token);
}

public interface IAnimationRegistry
{
    bool TryCreateViewAnimation(AnimationKey key, IAnimationContext context, out IViewAnimation animation);
    bool TryCreateTransition(AnimationKey key, IAnimationContext context, out ITransitionAnimation animation);
}
```
Executors rely on shared helpers to manage cancellation and final-state assignment.

## Implementation Steps
1. **Foundational Abstractions**
   - Introduce descriptor models, key struct, registry interfaces, and option/context contracts.
   - Implement the registry with hierarchical lookup and factory wiring, including DI registration extensions.

2. **Baseline Animation Library**
   - Port existing shell animations into descriptors/executors (`Shell.PageCrossFade`, `Shell.PageSlideHorizontal`, `Shell.PopupFadeScale`, `Shell.ToastSlideTop`).
   - Provide primitive animations and `CompositeViewAnimation` with explicit `AnimationCompositionMode` (Parallel/Sequential) plus stagger support.

3. **CustomShell Refactor**
   - Introduce local adapter methods (`PlayPageTransitionAsync`, `PlayPopupAnimationAsync`) that resolve keys (`AnimationKeys.Shell.*`) via the coordinator.
   - Replace inline `FadeTo`/`TranslateTo` blocks with adapter calls; leverage shared cancellation helpers to integrate with navigation queue.
   - Defer ViewSwitcher migration until shell flows are stable.

4. **ViewSwitcher Alignment**
   - Update `ViewSwitcherTransitionCoordinator` to consume `ITransitionAnimation` from the registry while preserving legacy extensibility.
   - Mark old transition APIs obsolete once the new path is validated.

5. **Navigation Bar / Tab Integration**
   - Extend `NavigationBarDescriptor` with `AnimationKey` properties for attach/detach transitions and share execution through the coordinator.

6. **Platform Overrides**
   - Document the lookup order (`Key.Platform.Theme -> Key.Platform -> Key`) and implement it inside the registry.
   - Provide DI samples for injecting platform/theme-specific descriptors without touching consumers.

7. **Diagnostics & Testing**
   - Add `AnimationEvent` notifications (Start/Completed/Cancelled/Failed) to the coordinator; leave integration to telemetry services optional.
   - Supply test doubles and zero-duration executors so orchestration logic can be unit tested without rendering.

8. **Accessibility & Motion Settings**
   - Implement `IMotionSettings` providing `ReduceMotion`, global duration multipliers, and developer overrides.
   - Animate executors must respect `ReduceMotion` by skipping motion or shortening duration unless `ForceMotion` is set.

## Integration Considerations
- Use shared helper methods (`AnimationRunner.RunAsync`) to wrap MAUI animations with cancellation tokens and ensure final-state assignment.
- `AnimationCoordinator` dispatches to the main thread when invoked off-thread and short-circuits to instant completion when `ReduceMotion` is true and `ForceMotion` is not set.
- `IAnimationContext` supplies safe-area, keyboard inset, and platform identifiers so animations can align with shell padding without duplicating logic.
- Provide a sample `AnimationsGalleryPage` for developers/designers to preview keys with different motion settings and platforms.

## Migration Strategy
1. Implement foundational abstractions and seed default animations.
2. Refactor `CustomShell` page transitions; validate navigation still queues correctly.
3. Migrate popup and toast animations; ensure `PopupVisualState` overlays still apply.
4. Update `ViewSwitcher` and new navigation bar host to request animations.
5. Remove legacy helper methods once all call sites use the coordinator.

## Open Questions
- Should animations be configurable at runtime (settings toggle for reduced motion) and if so, do we need dynamic registry updates?
- Can timeline/keyframe-based animations be layered on later by adding dedicated descriptor/executor types without altering existing contracts?
- What tooling or preview support do designers need beyond the proposed gallery page?
- What telemetry schema (key, target type, duration, cancel state) best supports monitoring animation performance on lower-end devices?
