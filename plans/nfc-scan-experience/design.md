# Design

## Current system

- `KycTravelDocumentPage.xaml` stacks an intro block, an NFC block with two illustration variants, a success block, an Android-only progress panel (three `Ellipse` dots, status text, spinner), an iOS-only panel, and a warning panel. Visibility is driven by many view-model booleans.
- `KycTravelDocumentViewModel` owns the flow (`KycTravelDocumentFlowState`), session guards, reservation, readout, and save. `UpdateChipProgressAsync` maps `TravelDocumentsState` to a status text and a coarse `NfcProgressStage` (0-2), and pushes the native iOS alert for every chip event.
- `TravelDocumentsClient` raises `StateChanged` for detection, authentication (PACE or BAC steps), file selection, every binary read chunk, certificate validation, and idle. The readout service forwards these via `TravelDocumentReadoutRequest.ProgressCallback`.
- SkiaSharp 4 is used by `SvgView` and `ProgressBar`. In SkiaSharp 4, `SKPath` mutation methods are obsolete in favor of `SKPathBuilder`.
- `IMotionSettings.ReduceMotion` is the app's reduced-motion signal. `HapticFeedback` is used by the QR scanner.

## Proposed architecture

### Presentation states (R2)

A new enum `NfcScanVisualState` (UI/Controls) describes the hero scene: `Intro`, `Preparing`, `Searching`, `Paused`, `Reading`, `Success`, `Failure`. The view model derives it:

| Condition | Visual state |
| --- | --- |
| Readout available or flow `Success` | `Success` |
| Intro, MRZ capture, or error without MRZ | `Intro` |
| Error with MRZ and user cancelled | `Paused` |
| Error with MRZ | `Failure` |
| `NfcReading` | `Reading` |
| `NfcReady` and busy | `Searching` |
| Busy otherwise (reservation) | `Preparing` |
| Otherwise (saved MRZ, not started) | `Paused` |

### Hero control: `NfcScanVisual` (R2-R5, R7)

An `SKCanvasView` subclass in UI/Controls with bindable properties:

- `State` (`NfcScanVisualState`), `Progress` (0-1), `IsPassport`, `AntennaPlacement` (`NfcAntennaPlacement.BackCenter` or `TopEdge`), `SuccessRevealDelay` (ms).
- Theme colors: `AccentColor`, `OnAccentColor`, `ContentColor`, `TrackColor`, `SurfaceColor`, `WarningColor`, bound with `DynamicResource`.

Behavior:

- Draws in a 240x240 design box scaled to fit, using `NfcScanVisualPainter` (pure drawing, one method per scene) and a cached ICAO chip symbol parsed from the existing `icao_chip.svg` path data.
- An `IDispatcherTimer` (about 60 fps) runs only while something moves: looping scenes, crossfades, one-shot animations, or progress easing. It stops on `Unloaded` and when static.
- Scene changes crossfade (about 300 ms). Scenes that continue each other (Reading to Success or Failure, Searching to Paused) switch without a crossfade.
- Displayed progress eases toward the bound target; resets snap immediately. Completing a ring segment flashes it; new data pulses a faint inner ring, throttled.
- Success plays only when entering from `Reading`; otherwise the final frame is shown. Failure shakes only when entering from an active state.
- Reduced motion renders settled, static frames with no timer.
- `SuccessRevealDelay` lets iOS hold the full ring while the native sheet dismisses before revealing the check mark.

Scenes:

- Intro: passport cover with the ICAO symbol and a pulsing highlight ring.
- Preparing: chip symbol with an indeterminate spinner arc.
- Searching: document (passport or ID card) with the phone gliding in and settling, gentle drift ("move slowly"), and NFC ripples from the antenna point. The phone and document are auto-fit to the box for both antenna placements.
- Paused: the Searching layout at rest, without motion.
- Reading: four-segment ring with a glowing head, breathing chip symbol, data pulses.
- Success: gaps close, the disk fills, a check mark draws, and a burst of ring and particles plays.
- Failure: warning-colored ring showing reached progress, exclamation disk, and a decaying shake.

### Step indicator: `GuideStepper` and `GuideStep` (R1)

A C# `ContentView` in UI/Controls. `GuideStep` is a `BindableObject` with a `Label`, so XAML can use `{l:Localize}`. The stepper renders N columns: a numbered circle, a check (`Geometries.SuccessCheckmarkPath`) when complete, connecting half-lines, and a label. `CurrentStep` (int) marks earlier steps complete and the current one active; `CurrentStep >= N` completes all. State changes pop the affected indicator unless reduced motion is on.

### Progress model: `TravelDocumentReadProgress` (R4)

A plain .NET class in Services/TravelDocuments (no MAUI dependencies) that consumes `TravelDocumentsState` values:

- Stages (`TravelDocumentReadStage`): `Detect`, `Secure`, `Read`, `Verify`; four equal ring segments.
- `Detected` completes Detect. Events before and during authentication fill Secure asymptotically. Authentication-specific states (PACE and BAC steps) mark authentication as started; the first file or read event after that begins Read, which fills asymptotically with read chunks. `Idle` begins Verify at half. `Complete()` sets 1.0 on save.
- Progress is monotonic within an attempt and never reaches 1.0 before `Complete()`.

### View model changes

- Replace `NfcProgressStage` and old-layout properties with `ScanVisualState`, `ChipReadProgress`, `IsPassportDocument`, `GuideStep`, `NfcHeadlineText`, `ShowNfcMessage`, `ShowPlacementTip`, `PlacementTipText`, `ShowIntroError`, `ShowStartNfcAction`, and `IsScanCancelled`.
- Derived presentation properties are raised from one `NotifyPresentationChanged` method called by the toolkit's `On<Property>Changed` hooks, replacing long `NotifyPropertyChangedFor` lists.
- `UpdateChipProgressAsync` feeds the tracker, sets status text by stage (adding "Opening a secure connection…"), and updates the iOS native alert only when its message key changes.
- A delayed task per session expands placement help after 12 seconds of searching, guarded by session ID and state.
- Haptics (Android only): click on first detection, long press on success, double click on failure. Screen reader announcements on detection, success, and failure.
- Cancellation shows the neutral cancelled title and description; rescan is hidden for cancellation.
- Resumed state with saved MRZ shows "Document details saved…" and a "Read chip" action.

### Page layout

```text
Header: close button, page title
GuideStepper: Scan details, Read chip, Done
Scroll: NfcScanVisual (fixed height), message block (title, status, tip, intro error)
Footer: intro question with Yes/No, or Read chip, Try again, Rescan, Continue; Continue manually as text button
```

The message block fades and slides in on each visual-state change (code-behind, reduced-motion aware). Safe areas and keyboard insets are unchanged.

## Data model changes

None persisted. Transient view-model state only.

## API, service, and interface changes

- New public controls and enums in UI/Controls. New progress tracker in Services/TravelDocuments.
- No service interface or readout contract changes.

## UI and UX copy

New keys (en, sv, es, pt): `KycTravelDocumentStepScan`, `KycTravelDocumentStepChip`, `KycTravelDocumentReadChipButton`, `KycTravelDocumentNfcPreparingTitle`, `KycTravelDocumentNfcSecuring`, `KycTravelDocumentNfcErrorTitle`, `KycTravelDocumentNfcCancelledTitle`, `KycTravelDocumentNfcCancelledDescription`, `KycTravelDocumentNfcHelpPassport`, `KycTravelDocumentNfcHelpIdCard`. Keys used from XAML get `AppResources.Designer.cs` accessors, which `LocalizeExtension` requires.

## Error handling

Failure classification, cancellation, retry, PACE retry, reservation cleanup, and save logic are unchanged. The visual only reflects state. Haptic and screen reader calls are wrapped in try/catch. Timer callbacks tolerate a missing dispatcher.

## Security and privacy

No document data is rendered. Success is derived from the existing saved readout.

## Migration and backwards compatibility

No migration. iOS native alerts keep their existing text; only redundant repeat updates are skipped. This design supersedes the Android three-dot indicator and compact illustration from `plans/android-nfc-progress`; its approved copy, including the authentication failure message, is kept.

## Test strategy

Source review of bindings, state derivation, progress monotonicity, guards, and resources. The progress tracker is plain .NET and can be unit tested later if requested. Device checks: Android and iOS, passport and ID card, light and dark, reduced motion, large fonts, sv/es/pt, cancel, timeout, connection loss, and reopen with saved MRZ or readout.

## Alternatives considered

- Hand-authored Lottie files: cannot follow theme colors or live progress, need per-theme and per-document variants, and cannot be previewed here. Rejected in favor of a procedural Skia scene.
- MAUI shapes with `StrokeDashOffset` animation: inconsistent across platforms for arcs and lacks blur and path trimming.
- Percentage text: implies precision the chip protocol cannot provide; the segmented ring communicates stage and motion without numbers.
