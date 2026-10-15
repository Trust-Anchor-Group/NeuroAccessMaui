# MRZ scan experience and chip-flow actions

Status: Approved. The user chose plain "Scan your ID" wording and a guide frame that snaps to the detected document.

## Purpose

The document-details scanner (`KycDocumentMrzScannerPage`) is the first hands-on step of the chip flow, but it feels like a different app: it uses its own brown, amber, sky-blue and red palette, native MAUI buttons, a text "Back" pill, and a letterboxed camera. It gives no clear target before a document is detected, no satisfying confirmation when it succeeds, and it is only available in English. The actions and button text around it, from the application form through the chip page, also leave users guessing what each choice does.

## Current behavior (findings)

Scanner:

- The camera uses `CameraPreviewScaling.Fit`, leaving black bars above and below the preview on tall phones.
- Before detection, only a faint sweeping line is drawn. The hint says "Place the machine-readable lines inside the highlighted area", but no area is highlighted until a document is found.
- After detection, the detected document outline is filled and bracketed, but the area outside it is barely dimmed (12% black).
- The hint card, validation overlay and debug buttons use hard-coded colors (`#DD1E1712`, `#E610202A`, `#F87171`, and others) that match neither the app theme nor the redesigned QR scanner.
- Success returns to the previous page immediately: no visual confirmation, no haptic.
- The expired and unsupported-document overlay uses raw `Button` controls with red styling, and "Try again" and "Back" do not say what happens.
- There is no torch control, and no extra help when scanning keeps failing.
- Copy uses jargon ("machine-readable lines", "document lines") and never tells ID-card users to scan the back side.
- `KycDocumentMrzScanner*` and `KycDocumentScan*` strings exist only in English.

Flow actions:

| Where | Current | Problem |
| --- | --- | --- |
| Application form, chip card | "Use document chip"; description mentions "NFC.xml" | Technical jargon; the button does not say what starts |
| Application form, chip status | "MRZ saved. Tap the document with your phone…" | Jargon ("MRZ"); contradicts "hold" wording elsewhere |
| Chip page header | "Personal ID" | Does not describe the task |
| Chip intro | "Yes, scan document" / "No" | "No" does not say it skips the chip and returns to the form |
| Scanner header | "Back" text pill | Inconsistent with icon buttons used on other scanners |
| Scanner overlay | "Try again" / "Back" for an expired or unsupported document | Trying again with the same expired document cannot succeed |
| Chip failure | "Try again" / "Scan document details again" / "Continue manually" | Long rescan label; "Continue manually" does not say the chip is skipped |

## Goals

- Make the scanner look and behave like the app's other camera experience (the QR scanner) and the redesigned chip page.
- Show where to place the document before anything is detected, and confirm success in a satisfying way.
- Make every action say what it does, with consistent wording across the flow.
- Provide the scanner in every language the chip flow supports.

## Non-goals

- No changes to OCR, preview analysis, frame stability, validation rules, or MRZ evidence handling.
- No manual MRZ entry.
- No changes to the chip-reading page's visual (`NfcScanVisual`) or its crash investigation.
- No new NuGet dependencies.

## Requirements

### R1: Consistent scanner chrome

WHEN the scanner opens,
THEN the camera fills the screen, the header uses the same icon back button and title treatment as the QR scanner, and all colors come from a small scanner palette shared with the QR scanner (white neutral, green success, amber attention).

WHEN a torch is available,
THEN a torch toggle is shown and turns off when the scanner closes.

### R2: Visible target

WHEN no document is detected,
THEN a document-shaped frame with a highlighted band at the bottom shows where the page and its text lines should go, and the area outside the frame is dimmed.

WHEN a document is detected,
THEN the frame follows the detected outline and changes to the success color while the user holds still.

### R3: Clear, calm guidance

WHEN the scanner gives guidance (find, move closer, reduce glare, hold still, could not read),
THEN one short title and one plain-language sentence are shown in a card styled like the rest of the app, with an icon for the current guidance.

WHEN scanning has not succeeded after a while,
THEN extra tips are shown (light, glare, keeping the phone parallel).

The initial guidance explains that passports are scanned on the photo page and ID cards on the back.

### R4: Satisfying success

WHEN the document details are captured,
THEN the frame turns green, a check mark appears, the phone gives a short haptic tick, and the scanner closes after a brief pause.

WHEN reduced motion is on,
THEN the same confirmation is shown without animation.

### R5: Clear recovery for unusable documents

WHEN the document is expired or not supported,
THEN a themed panel explains why, offers "Scan another document" as the primary action and "Go back" as the secondary action, and uses the app's button styles.

### R6: Action and copy review across the flow

WHEN any action in the chip flow is shown,
THEN its label states the outcome, according to the proposed copy below.

### R7: Localization

WHEN the scanner or any changed copy is shown,
THEN English, Swedish, Spanish, and Portuguese text is available.

## Proposed copy (English)

Scanner:

| State | Title | Detail |
| --- | --- | --- |
| Header | Scan your document | |
| Find | Fit the document in the frame | Passport: photo page. ID card: back side. |
| Move closer | Move closer | Fill the frame with the document. |
| Glare | Reduce glare | Tilt the document slightly or move away from bright light. |
| Hold still / reading | Hold still | Reading the text at the bottom… |
| Could not read | Let's try again | Keep the document flat and the bottom lines sharp. |
| Help after a while | Having trouble? | Use good light, avoid glare and hold the phone parallel to the document. |
| Success | Got it | Document details captured. |
| Expired | This document has expired | Expired documents can't be used. Scan a valid passport or ID card. |
| Unsupported | Document not supported | Scan a passport or national ID card. |
| Overlay actions | Scan another document / Go back | |
| Torch | Turn on light / Turn off light (accessibility) | |

Flow actions:

| Where | Proposed |
| --- | --- |
| Application form chip card title | Verify with your ID chip |
| Application form chip card description | Scan your passport or ID card and hold it to your phone. This fills in your details and adds a secure chip reading to your application. |
| Application form chip button | Verify with chip |
| Application form status, details saved | Document details saved. Read the chip to finish. |
| Application form status, readout ready | Chip read. It will be added to your application. |
| Chip page header | Verify with chip |
| Chip intro actions | Yes, scan document / No, fill in manually |
| Chip failure actions | Try again / Scan details again / Continue without chip |
| Chip cancelled and resume actions | Try again or Read chip / Continue without chip |
| Chip success action | Continue |

## Edge cases

- Camera permission denied, or the camera failing to start.
- Tall and short screens, landscape tablets, and large fonts in the guidance card.
- A document detected partly outside the visible (cropped) preview after switching to fill scaling.
- Rapidly alternating guidance states (the existing minimum hint durations stay).
- The success pause interrupted by leaving the page (the result is still delivered once).
- Reopening the scanner from the chip page to rescan.

## Security and privacy

No document data is shown on the scanner beyond the camera preview. Success feedback does not reveal parsed fields.

## Compatibility and migration

No persisted data or navigation contract changes. The scanner still returns a `TravelDocumentMrzResult` through the existing completion source. Existing resource keys are updated in place where the meaning is unchanged; new keys are added otherwise. The debug-only artifact buttons stay available under their existing build flags.

## Assumptions

- The QR scanner's feedback colors (`#5CE0A0` success, `#FFD166` attention, white neutral) are the reference for camera screens.
- Swedish, Spanish (informal "tú"), and Brazilian Portuguese match the existing chip-flow translations.

## Open questions

1. Is "Verify with chip" acceptable as the chip page header and application-form button, replacing "Personal ID" and "Use document chip"?
2. Should the scanner keep drawing the detected document outline, or show only the fixed guide frame? The proposal keeps both: the guide snaps to the detected outline.
