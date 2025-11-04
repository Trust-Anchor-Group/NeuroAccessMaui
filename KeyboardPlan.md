# Keyboard Handling Improvements – Technical Plan

## Technical Requirements
- Provide a **default, platform-agnostic keyboard avoidance layer** inside `CustomShell` so any page displayed through the shell automatically adjusts when the on-screen keyboard appears or changes size.
- Preserve existing **safe-area logic** by combining reported keyboard height with existing safe-area insets; pages must still be able to opt out of the keyboard adjustment when they manage layout themselves.
- Ensure **consistent measurement units** across platforms by normalising keyboard height to device-independent units (DIPs) before broadcasting layout updates.
- Changes must not block UI thread responsiveness: animations and padding updates must run on the main thread but avoid synchronous waits.
- Support MAUI targets (Android, iOS, Windows) with clear extension points for platform-specific keyboard notifications.
- Avoid breaking current navigation, popup, or toast behaviour; popups must continue to block the background as designed.
- Respect localisation and theming resources; do not hard-code language strings or colours.
- Maintain testability by isolating keyboard state logic in a dedicated service or model that can be injected or mocked.

## Implementation Plan

1. **Keyboard Insets Service**
   - Create a `KeyboardInsetsService` (singleton) responsible for storing the latest safe-area and keyboard insets and exposing events or observables for updates.
   - Inject the service into `CustomShell`, popups, and any behaviours that need keyboard data. Use dependency inversion (service interface) for testability.

2. **Platform Event Normalisation**
   - Update each `PlatformSpecific` implementation to convert the detected keyboard height to DIPs before raising `KeyboardSizeMessage`.
   - Ensure `KeyboardSizeMessage` consistently represents the same unit across Android, iOS, and Windows. Document and verify conversions.

3. **CustomShell Integration**
   - Subscribe `CustomShell` to keyboard inset updates via the service or messenger on `OnAppearing` and unsubscribe on `OnDisappearing`.
   - Combine safe-area padding (from `SafeArea.ResolveInsetsFor`) with keyboard height, applying the result to the active content host and any overlay layers (popups, toast container).
   - Animate padding changes (e.g., `Animation` or `ThicknessAnimation`) to avoid abrupt jumps. Provide a short easing-based transition.
   - Respect an opt-out mechanism: add an attached property or interface (e.g., `KeyboardInsetMode` or `IKeyboardInsetAware`) that lets pages specify one of:
     - `Automatic` (default): shell applies padding.
     - `Manual`: shell skips adjustments and forwards raw keyboard height so the page can act.

4. **Popup and Toast Adjustments**
   - Update `BasePopupView` to listen for inset updates and adjust `PopupMargin` or internal translation unless the popup opts out. Ensure blocking behaviour is unaffected.
   - Confirm toast placements (`ToastPlacement.Top/Bottom`) remain visible with the new bottom inset.

5. **Scrollable Content Helpers**
   - Provide a reusable behaviour (e.g., `KeyboardAwareScrollBehavior`) that can scroll a `CollectionView` or `ScrollView` to keep the focused editor visible.
   - Default to enabled; pages can disable by setting the opt-out property on the behaviour or the page.

6. **Opt-Out Path**
   - Define an attached property `KeyboardInsets.Mode` (values: `Automatic`, `Manual`, `Ignore`) applied to pages or views.
   - `CustomShell` checks this property before applying padding. For `Manual`, it forwards keyboard height to `IKeyboardInsetAware` implementations.

7. **Lifecycle and Disposal**
   - Ensure subscriptions are registered in `OnAppearingAsync` / `OnDisappearingAsync` overrides to prevent memory leaks.
   - For popups and transient views, tie registration to activation/deactivation hooks already present in `BasePopupView` and `BaseContentPage`.

8. **Verification Steps**
   - Manual validation on Android and iOS (portrait and landscape) to confirm keyboard reveals no longer hide inputs and popups remain anchored.
   - Windows keyboard (if applicable) should not regress; verify no unexpected padding when no on-screen keyboard is present.
   - Review navigation transitions to ensure animations still execute smoothly with dynamic padding changes.

9. **Documentation & Follow-Up**
   - Update or create developer-facing notes (e.g., in project wiki) describing the keyboard handling behaviour and opt-out options.
   - Capture any platform nuances discovered during implementation for future maintenance.

