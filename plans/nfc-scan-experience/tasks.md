# Tasks

The user requested the overhaul as one piece of work. Tasks are ordered so each can be reviewed independently.

- [x] R4-T1 Add chip read progress tracker
  - Covers: R4
  - Files: NeuroAccessMaui/Services/TravelDocuments/TravelDocumentReadProgress.cs; NeuroAccessMaui/Services/TravelDocuments/TravelDocumentReadStage.cs
  - Validate: Source review of stage transitions and monotonic progress
  - Done when: Tracker maps chip states to four stages with asymptotic fill, resets per attempt, and completes only on demand
  - Completed: Compiled in an isolated scratch project and fed a simulated PACE readout (EF.CardAccess, PACE, EF.COM, EF.SOD, certificate, DG1, DG2 with 90 chunks, DG11, idle). Progress stayed monotonic: 0.25 on detection, about 0.42 after PACE, 0.50 to 0.72 while reading, 0.875 at idle, and 1.0 only after `Complete()`.

- [x] R2-T2 Add NFC scan hero control
  - Covers: R2, R3, R4, R5, R6, R7
  - Files: NeuroAccessMaui/UI/Controls/NfcScanVisual.cs; NeuroAccessMaui/UI/Controls/NfcScanVisualPainter.cs; NeuroAccessMaui/UI/Controls/NfcScanVisualState.cs; NeuroAccessMaui/UI/Controls/NfcAntennaPlacement.cs
  - Validate: Source review against SkiaSharp 4 API (non-obsolete path building), timer lifecycle, reduced motion
  - Done when: All seven scenes render with theme colors, transitions, and progress easing, and the timer stops when static or unloaded
  - Completed: Compiled with zero warnings against Microsoft.Maui.Controls 10.0.10, SkiaSharp 4.151.1, and SkiaSharp.Views.Maui.Controls 3.119.2 in a scratch library. Every scene was rendered to PNG contact sheets in light and dark palettes and reviewed; placement proportions, entrance direction, and intro spacing were tuned from that review.

- [x] R1-T3 Add guide step indicator control
  - Covers: R1, R7
  - Files: NeuroAccessMaui/UI/Controls/GuideStepper.cs; NeuroAccessMaui/UI/Controls/GuideStep.cs
  - Validate: Source review of layout and state styling
  - Done when: The stepper renders localized steps with active, complete, and upcoming styles
  - Completed: Compiled with the hero control in the scratch library. Not yet seen on a device.

- [x] R6-T4 Refactor travel document view model presentation state
  - Covers: R1, R3, R4, R5, R6, R7
  - Files: NeuroAccessMaui/UI/Pages/Kyc/KycTravelDocumentViewModel.cs
  - Validate: Source review of state derivation, guards, haptics, placement help timer, and native alert dedupe
  - Done when: The view model exposes the new presentation API, removes old-layout properties, and keeps flow behavior intact
  - Completed: Session guards, reservation, readout, save, telemetry, and PACE retry are unchanged. The cancelled flag is set before the error flag so a cancel never briefly shows the failure scene. The app project was not built.

- [x] R2-T5 Rebuild travel document page layout
  - Covers: R1, R2, R3, R6, R7
  - Files: NeuroAccessMaui/UI/Pages/Kyc/KycTravelDocumentPage.xaml; NeuroAccessMaui/UI/Pages/Kyc/KycTravelDocumentPage.xaml.cs
  - Validate: Source review of bindings against view-model properties and styles
  - Done when: The page uses the stepper, hero visual, single message block, and state-driven footer with an animated message transition
  - Completed: Every compiled binding was matched to a view-model member. The app project was not built.

- [x] R8-T6 Add localized copy
  - Covers: R8
  - Files: NeuroAccessMaui/Resources/Languages/AppResources{,.sv,.es,.pt}.resx; NeuroAccessMaui/Resources/Languages/AppResources.Designer.cs
  - Validate: XML well-formedness and key parity check across the four languages
  - Done when: All new keys exist in en/sv/es/pt, and XAML-used keys have designer accessors
  - Completed: Ten keys were added per language. All four files parse with no duplicate keys. Every key referenced by the page and view model exists, and every XAML `Localize` key has a designer accessor.

## Outstanding device checks

- Android and iOS; passport and ID card; light and dark themes.
- Reduced motion, large fonts, and sv/es/pt text lengths.
- Cancel, timeout, connection loss mid-read, PACE retry on iOS, and reopening with a saved MRZ or saved readout.
- Haptic feel on Android and success timing behind the iOS sheet (`SuccessRevealDelay`, 900 ms).
