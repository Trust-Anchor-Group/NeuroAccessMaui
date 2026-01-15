# NFC

This document summarizes the NFC-related work implemented in NeuroAccessMaui so far.

## Goals

1. **NFC tag ingestion for app URIs**: Make NFC tags that contain the same URI format as QR codes behave like a QR scan (i.e., feed into the existing `App.OpenUrl*` routing pipeline).
2. **Passport (eMRTD) NFC scaffolding for KYC**: Add the building blocks required to read an ePassport chip in the KYC process.

## High-level architecture

### Shared flow (cross-platform)

- Platform NFC detection produces a cross-platform `NfcTag` object and forwards it as an application intent.
- The intent queue is processed by `IntentService`, which dispatches NFC events to the shared NFC handler.
- The shared handler (`INfcService` / `NfcService`) inspects the detected tag interfaces and performs one of:
  - NDEF parsing → URI extraction → either complete a user-initiated scan or navigate to the URI.
  - ISO-DEP (passport) → run the current BAC handshake scaffold (no DG reads yet).

Key components:

- `NeuroAccessMaui/Platforms/Android/MainActivity.cs`: Android NFC intent decoding and construction of `NfcTag`.
- `NeuroAccessMaui/Services/Intents/IntentService.cs`: Dispatches `Constants.IntentActions.NfcTagDiscovered` to `INfcService.TagDetected`.
- `NeuroAccessMaui/Services/Nfc/NfcService.cs`: Central shared NFC processing.

### User-initiated scanning (UI-driven)

Some platforms (notably iOS) require an **active NFC reader session** started by the app. To unify UX and avoid sprinkling platform NFC details across pages, the app uses explicit scan services:

- `INfcScanService`: user-initiated NDEF → URI scanning
- `IPassportNfcScanService`: user-initiated passport (ISO-DEP) scanning

These services allow UI pages to request a scan and await a result.

## Implemented features

### 1) NFC tags that contain URIs (same URI pipeline as QR)

**What works**

- The app can extract a URI from NFC NDEF content and route it through the existing URL/open pipeline.
- If a UI flow is actively waiting for an NFC URI (e.g., from the QR scan page), the NFC handler completes that pending scan instead of navigating immediately.

**Where it is implemented**

- `NeuroAccessMaui/Services/Nfc/NfcService.cs`
  - Handles NDEF payloads.
  - Extracts URIs from:
    - NDEF URI records (`INdefUriRecord`)
    - NDEF Well Known Text (`T`) records (text is treated as a URI if it parses as an absolute URI)
  - If the URI looks like an app scheme (`Constants.UriSchemes.GetScheme(...)`), it either:
    - Completes an active scan: `INfcScanService.TryHandleDetectedUri(...)`
    - Or (default) authenticates user and calls `App.OpenUrlAsync(...)`

- `NeuroAccessMaui/Services/Nfc/INfcScanService.cs`, `NeuroAccessMaui/Services/Nfc/NfcScanService.cs`
  - **Android (passive)**: `ScanUriAsync` waits for the next NFC intent-delivered tag to be processed and for `TryHandleDetectedUri(...)` to be called.
  - **iOS (active)**: `ScanUriAsync` starts a `NFCNDEFReaderSession` and extracts URIs from well-known NDEF records:
    - `U` (URI) record (prefix table decoding)
    - `T` (Text) record (content treated as URI if absolute)

**UI integration**

- `NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodeViewModel.cs`
  - Adds a `ScanNfcCommand`/`ScanNfc` flow to request NFC scanning and return a URI via the same result mechanism used for QR.
- `NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodePage.xaml`
  - Adds an NFC button (outlined text button) bound to `ScanNfcCommand`.

### 2) Passport (eMRTD) NFC scaffolding for KYC

This is currently **scaffolding**: it establishes transport + MRZ capture + a BAC handshake attempt, but does not yet read data groups.

#### 2.1 MRZ capture and storage from the KYC passport photo

- `NeuroAccessMaui/Services/Kyc/ViewModels/ObservableImageField.cs`
  - For image fields with `SpecialType == "passport"`:
    - Extracts an MRZ region using IdApp.Cv (`ExtractMrzRegion`).
    - OCRs the MRZ using `IOcrService` with language `ocrb`.
    - Normalizes and validates MRZ via `BasicAccessControl.ParseMrz`.
    - Stores the MRZ into runtime settings: `RuntimeSettings["NFC.LastMrz"]`.

#### 2.2 User entry point in KYC UI

- `NeuroAccessMaui/UI/Pages/Kyc/KycProcessPage.xaml`
  - Adds a passport-only NFC button (visible when `IsPassportField` is true) that binds to `ScanPassportNfcCommand`.

#### 2.3 BAC helper fixes and BAC attempt in shared NFC handler

- `NeuroAccess.Nfc/Extensions/BasicAccessControl.cs`
  - `GetChallenge(...)` now validates status words and returns the actual 8-byte challenge.
  - `ExternalAuthenticate(...)` now validates status words and returns response data excluding SW1/SW2.

- `NeuroAccessMaui/Services/Nfc/NfcService.cs`
  - When an `IIsoDepInterface` (ISO 14443-4) is detected:
    - Reads MRZ from `RuntimeSettings["NFC.LastMrz"]`.
    - Runs the BAC handshake attempt:
      - `GetChallenge()` → `CalcChallengeResponse()` → `ExternalAuthenticate()`
    - Writes status to runtime settings:
      - `RuntimeSettings["NFC.Passport.BacOk"] = true/false`
      - `RuntimeSettings["NFC.Passport.LastBacAt"] = <UTC ISO 8601>` on success

### 3) iOS ISO-DEP / APDU bridge (CoreNFC → shared `IIsoDepInterface`)

To make the shared BAC helpers usable on iOS, we added an iOS implementation of the ISO-DEP/APDU transport:

- `NeuroAccessMaui/Platforms/iOS/Nfc/IosIsoDepInterface.cs`
  - Implements `IIsoDepInterface` using `NFCISO7816Tag.SendCommandApdu(...)`.
  - Returns response bytes with `SW1/SW2` appended to match the existing Android shape.

- `NeuroAccessMaui/Platforms/iOS/Nfc/IosIsoDepTag.cs`
  - Wraps CoreNFC `NFCISO7816Tag` as an `INfcTag` exposing one `IIsoDepInterface`.

### 4) User-initiated passport scan service (KYC button)

- `NeuroAccessMaui/Services/Nfc/IPassportNfcScanService.cs`
- `NeuroAccessMaui/Services/Nfc/PassportNfcScanService.cs`

Behavior:

- **iOS**:
  - Starts an `NFCTagReaderSession` for ISO 14443 tags.
  - Connects to the tag and requires `Iso7816Compatible`.
  - Wraps the CoreNFC tag using `IosIsoDepTag`, then calls `INfcService.TagDetected(...)`.
  - Returns `true/false` based on `RuntimeSettings["NFC.Passport.BacOk"]`.

- **Android / non-iOS**:
  - Uses a “passive wait” model:
    - Sets `RuntimeSettings["NFC.Passport.ScanRequested"] = true`.
    - Clears previous passport scan result keys.
    - Waits (up to 30 seconds, cancelable) for `RuntimeSettings["NFC.Passport.BacOk"]` to be set by the standard Android NFC intent pipeline.
  - `NfcService` is updated to set `BacOk = false` if a scan is requested but MRZ is missing/invalid, so the wait completes deterministically.

## Dependency injection / registration changes

The following services are registered in DI during app startup:

- `NeuroAccessMaui/UI/PageAppExtension.cs`
  - `IOcrService` (used for MRZ OCR)
  - `INfcService`
  - `INfcScanService`
  - `IPassportNfcScanService`

## Runtime settings keys used (debugging + orchestration)

These values are stored in `Waher.Runtime.Settings.RuntimeSettings`:

- `NFC.LastMrz` (string)
  - The last MRZ captured (typically from the KYC passport photo field).
- `NFC.Passport.ScanRequested` (bool)
  - Used to indicate that a user-initiated passport scan is currently active (helps Android passive waiting complete correctly).
- `NFC.Passport.BacOk` (bool)
  - Result of the last BAC attempt.
- `NFC.Passport.LastBacAt` (string, ISO 8601 UTC)
  - Timestamp written when BAC succeeds.

## Current limitations / known gaps

- Passport NFC currently performs **only BAC scaffolding**. It does not yet:
  - Select the eMRTD application AID
  - Establish secure messaging
  - Read EF.COM or data groups (DG1/DG2/etc.)
  - Map extracted data back into KYC fields

- UX prompts are currently generic (`AppResources.NFC`). Platform-specific instructions like “Hold your passport near the top of the phone” are not yet localized/implemented.

- iOS requires user-initiated sessions for NFC reads; the architecture supports this, but background/passive iOS NFC reads are not expected.

## Future work

### ePassport reading (core feature completion)

- Implement full eMRTD session flow after BAC:
  - SELECT eMRTD AID
  - READ EF.COM
  - READ DG1 (MRZ / identity text)
  - READ DG2 (face image)
  - Optional: DG11/DG12 depending on document
- Implement **secure messaging** for protected file reads.
- Consider supporting **PACE** (many modern passports prefer PACE over BAC).

### KYC integration

- Map DG1/DG2 content into the KYC flow:
  - Pre-fill name, birthdate, nationality, document number
  - Compare DG2 face image vs provided photo (if applicable)
  - Add explicit user consent and clear UI states for “NFC verified”

### UX + localization

- Add localized prompt strings:
  - “Hold your passport near the top of your phone”
  - “Keep the phone still until reading completes”
  - Improved error messages (MRZ missing, BAC failed, unsupported tag)

### Reliability and diagnostics

- Improve result reporting from `IPassportNfcScanService` to include failure reasons (instead of bool only).
- Add metrics/logging around:
  - Tag type detected
  - BAC success/failure rates
  - Time-to-scan

### Security

- Review storage semantics for `NFC.LastMrz` (lifetime, clearing policy, and whether it should be persisted beyond the KYC session).
- Ensure NFC-derived KYC updates are gated by user authentication/consent and consistent with the app’s security model.
