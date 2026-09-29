# NFC scan experience

Status: User requested a full overhaul of the NFC scanning UI/UX with freedom to refactor views and pages and to add SVG, Lottie, or other animations. User authorized direct implementation; phases are recorded separately below and in `design.md` and `tasks.md`.

## Purpose

The travel-document chip flow (`KycTravelDocumentPage`) works, but it feels busy and static: a fixed illustration, three small dots, a spinner, and several stacked text panels compete for attention. Users hold the phone against a document, often unable to see the screen, and get little tactile or visual confirmation that anything is happening. The goal is a flow that is simple to follow, clearly shows what to do next, and feels satisfying when it works.

## Goals

- One clear instruction and one hero visual per moment of the flow.
- Show how to hold the phone and document with an animated illustration that matches the document type and platform.
- Show live chip-reading progress that advances only when the chip actually responds.
- Celebrate success, and make failures calm, specific, and recoverable.
- Keep the user oriented across the whole flow: scan details, read chip, done.
- Fit the existing design language (colors, typography, buttons, theme support).

## Non-goals

- No changes to the NFC protocol, readout service behavior, evidence storage, preview reservation, telemetry semantics, or navigation contracts.
- No change to iOS native NFC sheet messages (the in-page UI behind the sheet may change).
- No changes to the MRZ camera scanner page.
- No new NuGet dependencies.
- No new tests unless requested.

## User scenarios

1. A user opens the chip flow, sees the chip symbol highlighted on a document, and answers whether their document has it.
2. After scanning the document details with the camera, the user sees that step completed and an animation showing how to place the phone on their passport or ID card.
3. The chip is found: the phone gives a short haptic tick, the illustration becomes a progress ring, and the text says to hold still.
4. The ring fills as data is read; the status names what is happening (secure connection, reading, checking security).
5. On success the ring closes into a check mark with a short celebration and haptic confirmation, then the user continues.
6. On failure the ring turns to a warning state with a specific message and clear recovery actions.
7. A user who cannot find the chip after a while receives extra placement help.
8. A user reopening the flow with saved document details can start reading the chip directly.

## Requirements

### R1: Guided step overview

WHEN the chip flow page is shown,
THEN a compact three-step indicator (scan details, read chip, done) shows completed, active, and upcoming steps.

WHEN the document details are captured and the NFC step is shown,
THEN the first step is shown as completed and the second as active.

WHEN the readout succeeds or a saved readout exists,
THEN all steps are shown as completed.

### R2: Single hero visual per state

WHEN the page is in the intro, preparing, waiting-for-chip, paused, reading, success, or failure state,
THEN exactly one hero visual represents that state and transitions smoothly from the previous state.

WHEN the theme changes between light and dark,
THEN the hero visual uses the current theme colors.

### R3: Placement guidance

WHEN waiting for the chip,
THEN an animated illustration shows the phone settling onto the document with NFC waves, using a passport or ID-card shape based on the scanned document type, and the antenna position appropriate to the platform (back center on Android, top edge on iOS).

WHEN waiting for the chip,
THEN a short tip explains how to find the chip.

WHEN waiting for the chip longer than a short delay without detection,
THEN the tip expands with document-specific help (remove phone case, try other positions).

### R4: Live reading progress

WHEN the chip is detected,
THEN the visual switches to a segmented progress ring (detect, secure connection, read data, verify), the title asks the user to hold still, and the status names the current activity.

WHEN chip data arrives,
THEN the ring advances in proportion to real chip events and never reaches completion before the readout is saved.

WHEN a new attempt starts,
THEN progress resets to empty.

### R5: Success feedback

WHEN the readout is saved successfully during this visit,
THEN the ring completes and resolves into a check mark with a short celebratory animation.

WHEN the page opens with an existing readout,
THEN the success state is shown without replaying the celebration.

### R6: Failure and recovery

WHEN a readout attempt fails,
THEN a warning visual, a clear failure title, and the existing specific failure message are shown with retry, rescan (where relevant), and manual fallback actions.

WHEN the user cancels the scan,
THEN a neutral cancelled state is shown with a retry action and without warning styling.

WHEN saved document details exist but no attempt has started,
THEN a primary action starts reading the chip (not "Try again").

### R7: Haptics and accessibility

WHEN on Android the chip is detected, the readout succeeds, or the readout fails,
THEN the device gives distinct haptic feedback where supported. iOS relies on its native sheet feedback.

WHEN a screen reader is active,
THEN chip detection, success, and failure are announced, and decorative visuals are excluded from the accessibility tree.

WHEN reduced motion is enabled,
THEN visuals show static, understandable frames without looping or celebratory motion.

### R8: Localization

WHEN any new or changed user-facing text appears,
THEN English, Swedish, Spanish, and Portuguese resources are available, consistent with existing tone.

## Edge cases

- Chip detected, then connection lost mid-read; retry after failure; PACE retry on iOS (new native session with the same flow).
- Preview reservation failure before the chip session starts.
- Leaving the page while scanning (session stop, animations stop).
- Page reopened with saved MRZ, with saved readout, or with insufficient saved MRZ.
- Small screens, large fonts, long translations, landscape tablets.
- Very fast chips (progress events in rapid succession) and slow chips (long pauses during certificate validation).

## Security and privacy

- No document data (names, numbers, images) is displayed in the new visuals or messages.
- Success is shown only after the existing successful save path.

## Compatibility and migration

- No persisted data, service interface, or navigation changes.
- Existing resource keys remain; new keys are added. Unused view-model properties specific to the old layout are removed.

## Assumptions

- A procedurally drawn SkiaSharp visual (already a dependency) is preferred over hand-authored Lottie JSON because it follows theme colors, adapts to document type and platform, and can react to live progress.
- Portuguese follows the existing Brazilian Portuguese resources; Spanish uses the informal "tú" form as existing resources do.
- The project is not built or run by the agent; the user validates on devices.

## Open questions

None blocking. Copy can be refined after device review.
