# Tasks

The user approved the requirements and decisions and asked for the full change.

- [x] R1-T1 Add shared scanner colors and use them in the QR scanner
  - Covers: R1
  - Files: NeuroAccessMaui/UI/ScannerColors.cs; NeuroAccessMaui/UI/Pages/Main/QR/ScanQrCodeViewModel.Camera.cs
  - Validate: Source review
  - Done when: Both scanners read feedback colors from one place
  - Completed: `ScannerColors` added; QR feedback colors now read from it.

- [x] R2-T2 Replace the scanner overlay drawable
  - Covers: R2, R4
  - Files: NeuroAccessMaui/UI/Pages/Kyc/KycDocumentMrzScannerPage.xaml.cs
  - Validate: Compile check in the scratch MAUI library
  - Done when: The guide frame, snap, scrim, band, sweep, and success check render from view-model state
  - Completed: `ScannerOverlayDrawable` compiled against Microsoft.Maui.Graphics 10.0.10 in a scratch library and rendered through Microsoft.Maui.Graphics.Skia for searching, reading, glare, and success states; corner weight and sweep visibility were tuned from the renders.

- [x] R1-T3 Rework the scanner view model presentation
  - Covers: R1, R3, R4, R5
  - Files: NeuroAccessMaui/UI/Pages/Kyc/KycDocumentMrzScannerViewModel.cs
  - Validate: Source review of hint keys, torch lifecycle, success pause, and cancel guard
  - Done when: Tone, icon, help, success, and torch state are exposed; OCR and validation behavior are unchanged
  - Completed: Tone color and icon replace the five hint colors; help after 15 s or ManualFallback; 700 ms success confirmation with haptic and announcement before the result is released; cancel during confirmation delivers the result; torch support and shutdown; fill scaling with crop-aware outline mapping. OCR and validation logic are unchanged. The app project was not built.

- [x] R1-T4 Rebuild the scanner page layout
  - Covers: R1, R3, R5
  - Files: NeuroAccessMaui/UI/Pages/Kyc/KycDocumentMrzScannerPage.xaml
  - Validate: Source review of bindings and styles
  - Done when: Header, guidance card, and validation sheet match the design
  - Completed: Every XAML binding resolves to a view-model member (scripted check).

- [x] R6-T5 Update flow copy and translate the scanner
  - Covers: R6, R7
  - Files: NeuroAccessMaui/Resources/Languages/AppResources{,.sv,.es,.pt}.resx; NeuroAccessMaui/Resources/Languages/AppResources.Designer.cs
  - Validate: XML parse and key parity across the four languages
  - Done when: All scanner and flow keys exist in en/sv/es/pt with the approved wording
  - Completed: 13 flow keys updated and 27 scanner keys added per language (sv/es/pt; English had 6 new keys). All four files parse without duplicates, and every used key exists in each language.
