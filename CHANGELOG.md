# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- ThemeLoaded property to IThemeService and implementation in ThemeService
- Conditional loading indicators in MainPage.xaml and GetStartedView.xaml
- IsLoading property to GetStartedViewModel
- New wallet page
- New theme resources (Light.xaml, Dark.xaml)
- NeuroAccessBranding version 2.0 (Version 1.0 is still compatible)

### Changed

- Change settings menu so back button is always visible
- Make app default theme to phone settings
- Registration workflow to direct users based on registration state (e.g., password/biometrics steps)
- ProfilePhoto logic in ViewIdentityViewModel to prioritize photos named ProfilePhoto
- AppColors are now always looked up and not cached

### Fixed

- Update Portuguese translations
- Fixed Profile configuration not loading properly
- Ensured authentication method set to Password when Later is selected in biometrics step
- --
- A bug which could cause a crash when opening a contract from the Wallet page
- Dangerous setting of properties in ViewContract which are not marshalled to Main Thread
- Bottom sheet in ViewId page is now more performant on most devices
- Improved visibility on text when scanning QR codes

### Removed

- Usage of AppThemeBinding, except for images
- --

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

