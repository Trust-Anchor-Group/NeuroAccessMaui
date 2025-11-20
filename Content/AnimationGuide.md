# Animation System Guide

The application ships with a reusable animation infrastructure designed to keep page, popup, toast, and future navigation-bar transitions consistent and easily configurable. This guide explains how the system is organised, how animations are resolved at runtime, and how you can add your own custom profiles.

---

## 1. Architecture Overview

| Component | Purpose |
|-----------|---------|
| `AnimationKey` | Strongly typed identifier for an animation profile (e.g. `AnimationKeys.Shell.PageCrossFade`). |
| `AnimationDescriptor` | Immutable description of an animation (duration, easing, factory). Separate descriptor flavours exist for view and transition animations. |
| `AnimationRegistry` | In-memory catalog that maps keys (optionally scoped per platform or theme) to descriptors and creates executors on demand. |
| `AnimationCoordinator` | Facade that consumers call to play animations. Handles main-thread dispatch, cancellation, telemetry events, and reduce-motion settings. |
| `AnimationContextProvider` / `AnimationContext` | Supplies runtime information (platform, theme, viewport size, keyboard height, motion preferences) to the animation executor. |
| `IMotionSettings` | Central store of user or platform motion preferences (reduce motion, duration multiplier). |
| Animation executors (`FadeAnimation`, `TranslateAnimation`, `CompositeViewAnimation`, `TransitionAnimation`, …) | Concrete classes that apply property changes to `VisualElement`s using MAUI animation primitives. |
| `AnimationRunner` | Helper that wraps MAUI animation APIs with consistent cancellation and cleanup behaviour. |

All of the reusable pieces live under `NeuroAccessMaui/Animations/`. The dependency injection setup lives in `NeuroAccessMaui/UI/PageAppExtension.cs` where the registry, coordinator, and motion settings are registered. Default profiles are seeded via `AnimationCatalog.RegisterDefaults`.

---

## 2. Animation Resolution Flow

1. **Consumer request** – A feature calls `IAnimationCoordinator.PlayAsync` or `PlayTransitionAsync`, providing an `AnimationKey` and the relevant `VisualElement`(s).
2. **Context build** – The coordinator asks `IAnimationContextProvider` to build a context snapshot (platform, theme, safe-area, viewport size, keyboard inset, reduce-motion flags).
3. **Registry lookup** – `AnimationRegistry` searches for a descriptor using hierarchical keys:
   1. `Key.Platform.Theme`
   2. `Key.Platform`
   3. `Key.Theme`
   4. `Key`
4. **Executor creation** – The descriptor creates an executor (`IViewAnimation` or `ITransitionAnimation`) tailored to the context.
5. **Execution** – The coordinator runs the animation through `AnimationRunner`, wiring up cancellation tokens, respecting reduce-motion settings, and raising telemetry events.

If a key is not registered, the coordinator emits a “Skipped” event and the caller should ensure final visual state is set manually.

---

## 3. Built-in Keys

`AnimationKeys` exposes strongly typed groups:

- `AnimationKeys.Shell.*` – Page transitions (`PageCrossFade`, `PageSlideLeft`, `PageSlideRight`) and popup/toast enter/exit profiles (`PopupShowScale`, `ToastHideSlideBottom`, …).
- `AnimationKeys.NavigationBars.*` – Default attach/detach animation for persistent navigation bars (`SwitchCrossFade`).
- `AnimationKeys.ViewSwitcher.*` – Default crossfade used by the tab view switcher.

Refer to `AnimationCatalog.RegisterDefaults` for the full list and default durations/easings.

---

## 4. Using the Coordinator

### Simple view animation

```csharp
public async Task ShowTooltipAsync(View tooltip, CancellationToken token)
{
    IAnimationCoordinator coordinator = ServiceHelper.GetService<IAnimationCoordinator>();
    tooltip.Opacity = 0;
    tooltip.IsVisible = true;

    await coordinator.PlayAsync(
        AnimationKeys.Shell.ToastShowFade,
        tooltip,
        Options: null,
        ContextOptions: null,
        Token: token);
}
```

### Transition animation (enter + exit views)

```csharp
public async Task SwapContentAsync(View incoming, View outgoing, CancellationToken token)
{
    IAnimationCoordinator coordinator = ServiceHelper.GetService<IAnimationCoordinator>();

    await coordinator.PlayTransitionAsync(
        AnimationKeys.Shell.PageCrossFade,
        incoming,
        outgoing,
        Options: new AnimationOptions { DurationOverride = TimeSpan.FromMilliseconds(200) },
        ContextOptions: null,
        Token: token);
}
```

The coordinator automatically honours reduce-motion preferences (animations collapse to an instant state change unless `ForceMotion` is `true`).

---

## 5. Registering Custom Animations

Use the registry to register new descriptors during application startup (e.g., in `AnimationCatalog.RegisterDefaults` or a dedicated extension method).

### Example: “Bounce-In” popup animation

```csharp
private static void RegisterCustomAnimations(IAnimationRegistry registry)
{
    TimeSpan duration = TimeSpan.FromMilliseconds(250);
    Easing easing = Easing.SpringOut;

    registry.Register(new ViewAnimationDescriptor(
        new AnimationKey("Popups.BounceIn"),
        duration,
        easing,
        _ => new CompositeViewAnimation(
            AnimationCompositionMode.Parallel,
            new List<IViewAnimation>
            {
                new FadeAnimation(duration, easing, 0, 1),
                new ScaleAnimation(duration, easing, 0.7, 1)
            })));
}
```

> **Tip:** If your animation depends on context (e.g., viewport size), capture it in the factory delegate:
>
> ```csharp
> context => new TranslateAnimation(duration, easing, context.ViewportHeight * 0.4, null, 0, 0);
> ```

### Scoping by platform/theme

Use overloads on `AnimationRegistry.Register`:

```csharp
registry.Register(descriptor, Platform: "Android");
registry.Register(descriptor, Platform: "iOS", Theme: "Dark");
```

Scoping supports the hierarchical lookup described earlier.

---

## 6. Creating a Custom Executor

Sometimes primitives like `FadeAnimation` or `TranslateAnimation` are not enough. To build a fully custom animation:

1. Derive from `TimedViewAnimation` (for single-view effects) or implement `IViewAnimation` directly.
2. Use `AnimationRunner.RunAsync` for consistent cancellation behaviour.
3. Honour `ReduceMotion` unless the caller sets `ForceMotion`.

```csharp
public sealed class RotateAnimation : TimedViewAnimation
{
    private readonly double startRotation;
    private readonly double endRotation;

    public RotateAnimation(TimeSpan duration, Easing easing, double startRotation, double endRotation)
        : base(duration, easing)
    {
        this.startRotation = startRotation;
        this.endRotation = endRotation;
    }

    public override async Task RunAsync(VisualElement target, IAnimationContext context, AnimationOptions? options, CancellationToken token)
    {
        if (context.ReduceMotion && options?.ForceMotion != true)
        {
            target.Rotation = this.endRotation;
            return;
        }

        target.Rotation = this.startRotation;
        uint duration = this.ResolveDurationMilliseconds(context, options);

        await AnimationRunner.RunAsync(target, _ => target.RotateTo(this.endRotation, duration, this.Easing), token)
            .ConfigureAwait(false);
    }
}
```

Register it via the registry just like any other descriptor.

---

## 7. Motion Settings & Accessibility

`IMotionSettings` stores the global reduce-motion flag and duration multiplier. The default implementation (`MotionSettings`) raises `MotionSettingsChanged` whenever values change. Consumers can react by re-resolving contexts or re-running animations with updated settings.

Example: responding to an accessibility toggle

```csharp
IMotionSettings motionSettings = ServiceHelper.GetService<IMotionSettings>();
motionSettings.Update(reduceMotion: true, durationScale: 1.0);
```

Animations automatically read these settings the next time they execute.

---

## 8. Telemetry & Diagnostics

The coordinator exposes an `AnimationEvent` stream:

```csharp
coordinator.AnimationEvent += (_, e) =>
{
    ServiceRef.LogService.LogDebug(
        $"Animation {e.Key} {e.Stage} (Elapsed={e.Elapsed}, Cancelled={e.WasCancelled})");
};
```

Events are raised for `Started`, `Completed`, `Cancelled`, `Skipped`, and `Failed`. Use them to feed telemetry dashboards or debug tricky animation timing issues.

For manual exploration, build a developer “animation gallery” page that lists registered keys and lets you preview them—this dramatically speeds up tuning.

---

## 9. Testing Notes

- Executors are isolated classes, so you can unit test them by supplying mock `VisualElement`s and stub contexts. Use the zero-duration pattern to avoid asynchronous timing flakiness.
- To validate registry behaviour, create a test registry instance, register descriptors, and assert `TryCreateViewAnimation` returns the expected executor type.
- When writing custom animations that interact with layout or safe-area, simulate different context values (e.g., viewport width/height) to ensure behaviour is predictable.

---

## 10. Quick Checklist for New Animations

1. **Define a key** – Add a constant to `AnimationKeys` (optional but recommended).
2. **Implement an executor** – Use the existing primitives or write a new `IViewAnimation`.
3. **Register a descriptor** – Update `AnimationCatalog` or your feature-specific registration code.
4. **Consume via the coordinator** – Call `IAnimationCoordinator.PlayAsync/PlayTransitionAsync` with the new key.
5. **Test reduced motion** – Ensure animations degrade gracefully when `ReduceMotion` is `true`.
6. **Add diagnostics** – Leverage `AnimationEvent` if you need runtime insight.
