# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Made field in request and payment page have proper background colors
- Handle neuroaccess URIs
- Add dynamic picture size on View Identity
- Fix margins on QR code so it is always visible in identity
- Various minor UI improvements
- Fixed scanning Onboarding QR codes outside of app

## [2.6.1] - 2025/06/30

### Added

- WIP Animation system.
- CustomShell host with NavigationService and PopupService, fully decoupled from MAUI AppShell.
- PolicyRunner/ObservableTaskBuilder integration driving startup/service resilience.
- Windows desktop support with platform-specific services, manifests, and presenter integration.
- Dynamic KYC flows and the reimagined Onboarding journey replacing ApplyID/registration.
- Added Swipe gesture on ID page to expand additional info
- KYC XML schemas, sample processes, and parser support for dynamic flow definitions.
- Dedicated  domain, including autosave, validation, and summary materialization.
- New  UI with summary sections, peer review hooks, and progress indicators.
- Identity summary formatter to shape output for the review screen.
- Async MVVM policy framework (, , etc.) used across loaders.
- Added coordinate parameter to Smart Contracts
- Added text to explain validation of Password/PIN
- New views when creating contract
- New localization strings for contract wizard steps, navigation, review, settings, visibility, and contract label in multiple languages
- Localization for Payment acceptence page
- Localized signing roles and warning messages across all supported languages
- ThemeLoaded property to IThemeService and implementation in ThemeService
- Conditional loading indicators in MainPage.xaml and GetStartedView.xaml
- IsLoading property to GetStartedViewModel
- New wallet page
- New theme resources (Light.xaml, Dark.xaml)
- NeuroAccessBranding version 2.0 (Version 1.0 is still compatible)

### Changed

- Upgraded to .NET 9
- Modified service lifecycle, making the app more resiliant when closing/sleeping/shutting off the app
- Applications page/view-model now load drafts through  pipelines and expose the active .
- Cache services, fetch abstractions, and schema registration were adjusted to support the new flow and XML validation.
- Branding, buttons, pickers, and other shared styles updated to align with the KYC experience visuals.
- Temporarily disabled e2e encryption in chat
- Updated  to display validation feedback in input controls
- Refactored  for naming consistency and improved readability
- Centralized button label styling by introducing  and inheriting from it in
- Minor changes to documentation
- Change settings menu so back button is always visible
- Make app default theme to phone settings
- Registration workflow to direct users based on registration state (e.g., password/biometrics steps)
- ProfilePhoto logic in ViewIdentityViewModel to prioritize photos named ProfilePhoto
- AppColors are now always looked up and not cached

### Fixed

- EDaler symbol loading properly in chat
- Updated  to be invalidated when validation state changes
- Update Portuguese translations
- Fixed Profile configuration not loading properly
- Ensured authentication method set to Password when Later is selected in biometrics step
- --
- A bug which could cause a crash when opening a contract from the Wallet page
- Dangerous setting of properties in ViewContract which are not marshalled to Main Thread
- Bottom sheet in ViewId page is now more performant on most devices
- Improved visibility on text when scanning QR codes

### Removed

- All files related to Registration (Replaced with Onboarding)
- AppShell
- Mopups dependency (Replace with in-house popup solution)
- Legacy  implementation superseded by the new  infrastructure.
- Unused contract overview views and associated XAML files (, )
- Usage of AppThemeBinding, except for images
- --

### Refactored

- ThemeService for maintainability and robustness

### Deprecated

- UiService (Replaced with NavigationService, PopupService)

## [2.6.1] - 2025/06/30

### Added

- Added Open Chat button when viewing an identity
- Added popup explaing that app might need to be restarted after changing language
- Added Popup if cache is cleared successfully
- Added Bottom bar with navigation options
- Added Apps page
- Added a toggle in settings for beta features
- Added button on main page to open Settings
- Added Info popups when clicking a disabled or beta app
- Added a way to open contracts refrenced in a contract reference parameter

### Changed

- Make Share QR popup automatically close after a set interval
- Moved wallet to bottom bar
- Moved flyout menu contents to new app page
- Fixed various layout issues
- Fixed some wrong colors
- Updated Waher nugets to latest version
- On windows machines only .net8.0-Android is targeted
- Improved date handling
- Fixes style inheritance and triggers.
- Fixed chat input background color.

### Fixed

- Fixed contacts not being added properly
- Fixed various bugs and errors regarding contract creation, signing and validation.
- Removed underlines from picker controls on Android devices

### Removed

- Removed flyout menu

## [2.5.1] - 2025-06-16

### Added

- PubSub Support in XmppService
- Fetching of dynamic branding material, colors & images
- InternetContentService, to easily cache files fetched from the web
- XmppGetter, A InternetContentGetter for xmpp items (Currently only PubSub)
- ThemeService to handle loading of branding and themes

### Changed

- Refactored AttachmentCacheService
- Updated .NET sdk to 8.0.411

### Fixed

- Updated all localization resources

## [2.4.1] - 2025-04-16

