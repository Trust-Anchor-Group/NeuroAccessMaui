# Design

Approved decisions: plain "Scan your ID" wording for the flow entry and chip page header (instead of "Verify with chip"), and a guide frame that snaps to the detected document.

## Current system

- `KycDocumentMrzScannerPage` hosts `CameraView` (fit scaling), a `GraphicsView` overlay driven by a 33 ms page timer (`DocumentOutlineDrawable`), a hint card, two debug-only buttons, and a validation overlay built from raw `Button`s.
- `KycDocumentMrzScannerViewModel` analyzes preview frames, publishes guidance (`CurrentGuidanceState`), the detected outline in page coordinates, hint text and five hint colors, and returns a `TravelDocumentMrzResult` through `KycDocumentMrzScannerNavigationArgs.CompletionSource` before navigating back.
- The QR scanner uses `FilledImageButton` controls, white/green/amber feedback, torch support via `CameraView.GetAvailableCamerasAsync` and `ICameraController.SetTorchAsync`, and a haptic click on success.

## Scanner design (R1–R5)

### Shared scanner colors

A new static `ScannerColors` class (UI) holds `Neutral` (white), `Success` (`#5CE0A0`), `Attention` (`#FFD166`), and `Scrim`. The QR scanner's feedback colors switch to it so both camera screens stay in sync.

### Page layout

```text
CameraView (fill)
GraphicsView overlay (scrim + frame + band + success)
Header (top safe inset): back ImageButton · title · torch ImageButton
Guidance card (bottom safe inset): icon or spinner · title · detail
Validation sheet (bottom, themed): warning icon · title · detail · "Scan another document" · "Go back"
Debug buttons (DEBUG with OCR_DEBUG_ARTIFACTS_NATIVE_SHARE only)
```

- The camera switches to `CameraPreviewScaling.Fill`. `TryResolvePreviewContentBounds` computes cover bounds (content may extend past the viewport) so the detected outline still maps to the right on-screen position.
- The header matches the QR scanner: `FilledImageButton` with `Geometries.BackButtonPath` bound to `CancelCommand`, white title, and a torch `FilledImageButton` (`Geometries.CameraTorchButtonPath`) visible only when the rear camera supports a torch.
- The guidance card is a dark translucent rounded card with white text. The icon (or an activity indicator while reading) uses the tone color.
- The validation sheet uses app theme resources (`SurfaceElevation1WL`, `ContentPrimaryWL`, `TnPWarningContentWL`) and `FilledTextButton` / `OutlinedTextButton`.

### Overlay drawable

`ScannerOverlayDrawable` replaces `DocumentOutlineDrawable`:

- **Guide frame:** a document-shaped quad (aspect 1.46, between passport and ID-1), up to 88% of the width, centered slightly above the middle.
- **Snap:** the rendered corners ease toward the detected outline when present, otherwise toward the guide (about 30% per frame; instant with reduced motion).
- **Scrim:** 55% black everywhere outside the quad (even-odd fill).
- **Frame:** a thin quad outline plus round-capped corner brackets. White while searching, amber for glare or move closer, green when a document is held steady or captured.
- **Text band:** the bottom 22% of the quad, lightly highlighted. While searching, dashed lines hint at the machine-readable text. While reading, a sweep line moves across the band.
- **Success:** the quad fills with green, a disk pops in, and a check mark draws in over about 350 ms.

### View model changes

- The five hint color properties are replaced by `HintAccentColor` (tone color) and `HintIcon` (a geometry per hint), keeping `IsHintProgressVisible` for reading states.
- New keys: `Help` (shown for search states or `ManualFallback` after 15 seconds without success) and `Success` (sticky, highest priority).
- Torch: `CanUseTorch` resolved after the preview starts, `IsTorchOn`, `TorchDescription` (reuses the QR torch strings), `ToggleTorchCommand`. The torch is turned off on disappearing.
- Success: when a result is accepted, set `IsCaptureSucceeded`, show the success hint, give a haptic click, announce it, wait 700 ms, then complete the result and navigate back. Frame processing is already blocked by `resultReturned` and `isProcessingFrame`. `CancelAsync` completes a pending successful result instead of returning null.
- The validation overlay's primary action becomes "Scan another document"; the secondary "Go back" reuses the updated back-action key.
- A recoverable bad read gets its own detail text.

## Flow copy (R6)

Existing keys are updated in place, since their meaning is unchanged:

| Key | New English |
| --- | --- |
| `KycTravelDocumentTitle` | Scan your ID |
| `KycTravelDocumentSummaryTitle` | Verify with your ID |
| `KycTravelDocumentDescription` | Scan your passport or ID card and hold it to your phone. We fill in your details and add a secure chip check to your application. |
| `KycTravelDocumentSummaryMissing` | Not scanned yet. |
| `KycTravelDocumentSummaryMrzReady` | Document details saved. Read the chip to finish. |
| `KycTravelDocumentSummaryReadoutReady` | Chip read. It will be added to your application. |
| `KycTravelDocumentOpenButton` | Scan your ID |
| `KycTravelDocumentUnsureButton` | No, fill in manually |
| `KycTravelDocumentRescanButton` | Scan details again |
| `KycTravelDocumentManualFallbackButton` | Continue without chip |
| `KycTravelDocumentReadoutReady` | Your chip has already been read. |
| `KycTravelDocumentNfcUnavailable` | Chip reading is unavailable. You can continue without the chip. |
| `KycTravelDocumentNfcReservationFailed` | Could not prepare secure verification. Try again or continue without the chip. |

Scanner strings are rewritten according to the requirements table and translated into sv, es, and pt for the first time.

## Error handling

Camera permission denial and OCR paths are unchanged. Torch failures are logged and reset the toggle. Haptic and screen reader calls are best effort.

## Security and privacy

No parsed document data is displayed. The success pause happens before the result is released to the caller, so the chip flow never runs behind a still-visible scanner.

## Test strategy

- Compile check of the overlay drawable in the scratch MAUI library, plus source review of bindings.
- Device checks: Android and iOS; passport photo page and ID-card back; glare and dim light (torch); expired and unsupported documents; permission denied; tall and short screens; sv/es/pt.

## Alternatives considered

- A fixed frame only: calmer, but loses the live tracking users found reassuring. Rejected in favor of snapping.
- A document-type toggle (passport or ID card) to change the frame: adds a decision before scanning. The combined hint text covers both types instead.
